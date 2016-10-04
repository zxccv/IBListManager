using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using InfoBaseListDataClasses;
using InfoBaseListUDPServerNamespace;
using Microsoft.VisualBasic;

namespace InfoBaseListManager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ObservableCollection<Computer> _comps;
        private ICollectionView _cvComps;
        private ICollectionView _cvUsers;
        private ICollectionView _cvInfoBases;

        private ICollectionView _cvInfoBaseCollections;
        private ICollectionView _cvStoredInfoBases;

        private InfoBaseListUdpServer _udpServer;
        private BackgroundTasks _backgroundTasks;        
        
        public MainWindow()
        {
            InitializeComponent();

            if(Config.ConfigurationData.Port == 0)
            {
                var frmSettins = new SettingsForm();
                frmSettins.ShowDialog();
            }

            if(Config.ConfigurationData.PoolList == null 
                || Config.ConfigurationData.PoolList.Count == 0)
            {
                var pool = new Pool {Name = "Организация"};
                Config.ConfigurationData.PoolList = new List<Pool> {pool};
                Config.ConfigurationData.CurrentPool = pool;
            }
            
            cbPoolList.ItemsSource = Config.ConfigurationData.PoolList;
            stackConfig.DataContext = Config.ConfigurationData;           
            
        }

        private void SaveInfoBases()
        {
            var selUser = (lbUsers.SelectedItem as User);

            if (selUser == null)
            {
                return;
            }

            selUser.PushInfoBases(_udpServer);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var frmSettins = new SettingsForm();
            frmSettins.ShowDialog();
            cbPoolList.Items.Refresh();
        }

        private void cbPoolList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.ConfigurationData.Save();

            if (_udpServer != null)
                _udpServer.Stop();
            _udpServer = new InfoBaseListUdpServer();
            _udpServer.Start(Config.ConfigurationData.Port, Config.ConfigurationData.CurrentPool.Name);

            _comps = new ObservableCollection<Computer>();

            _cvComps = CollectionViewSource.GetDefaultView(_comps);
            _cvComps.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
            lbComps.ItemsSource = _cvComps;

            _cvInfoBaseCollections = CollectionViewSource.GetDefaultView(Config.ConfigurationData.CurrentPool.InfoBaseCollectionList);
            _cvInfoBaseCollections.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            lbInfoBaseCollections.ItemsSource = _cvInfoBaseCollections;
            
            lbUsers.ItemsSource = null;
            tvInfobases.ItemsSource = null;

            if (_backgroundTasks != null)
                _backgroundTasks.Stop();
            _backgroundTasks = new BackgroundTasks(_udpServer, _comps, _cvComps);
            _backgroundTasks.Start();

            

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _backgroundTasks.Stop();
            _udpServer.Stop();
        }

        private void lbComps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selComp = (lbComps.SelectedItem as Computer);

            if (selComp == null)
                return;

            
            selComp.QueryUsers(_udpServer);

            _cvUsers = CollectionViewSource.GetDefaultView(selComp.Users);
            _cvUsers.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));            
            lbUsers.ItemsSource = _cvUsers;
            
            lbUsers_SelectionChanged(sender, e);
        }

        private void lbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selUser = (lbUsers.SelectedItem as User);

            if (selUser == null)
            {
                tvInfobases.ItemsSource = null;
                return;
            }
                

            selUser.QueryInfoBases(_udpServer);

            _cvInfoBases = CollectionViewSource.GetDefaultView(selUser.InfoBaseTree.ChildInfoBases);
            _cvInfoBases.SortDescriptions.Add(new SortDescription("InfoBaseName", ListSortDirection.Ascending));            
            tvInfobases.ItemsSource = _cvInfoBases;
        }

        private bool EditInfoBase(InfoBase ib)
        {
            var ibCopy = new InfoBase(ib);
            InfoBaseForm ibForm = new InfoBaseForm{ InfoBase = ibCopy};

            var showDialog = ibForm.ShowDialog();
            if(showDialog != null && (bool)showDialog)
            {
                var typeInfoBase = typeof(InfoBase);
                var typeInfoBaseFields = typeInfoBase.GetProperties();

                foreach (var typeInfoBaseField in typeInfoBaseFields)
                {
                    typeInfoBaseField.SetValue(ib, typeInfoBaseField.GetValue(ibCopy,null), null);                    
                }
                                
                return true;
            }

            return false;
        }

        private void tvInfobases_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selInfoBaseTree = (tvInfobases.SelectedItem as InfoBaseTree);

            if (selInfoBaseTree == null)
                return;

            var selInfoBase = selInfoBaseTree.InfoBase;

            EditInfoBase(selInfoBase);
            SaveInfoBases();
            _cvInfoBases.Refresh();
        }
        
        

        

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selUser = (lbUsers.SelectedItem as User);

            if (selUser == null)
            {
                return;
            }

            var ib = new InfoBase
            {
                InfobaseName = "Информационная база",
                Connect = "Srvr=\"\";Ref=\"\";",
                App = "Auto",
                Version = "8.3",
                Folder = "/",
                WA = "1"
            };

            var ibt = new InfoBaseTree(ib, selUser.InfoBaseTree);

            selUser.InfoBaseTree.ChildInfoBases.Add(ibt);
                       
            if(EditInfoBase(ib))
            {
                SaveInfoBases();
                _cvInfoBases.Refresh();
            } else
            {
                selUser.InfoBaseTree.ChildInfoBases.Remove(ibt);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selInfoBaseTree = (tvInfobases.SelectedItem as InfoBaseTree);

            if (selInfoBaseTree == null)
                return;

            var folder = selInfoBaseTree.Folder;

            folder.ChildInfoBases.Remove(selInfoBaseTree);
            SaveInfoBases();
        }

        

#region InfoBaseCollection

        private void EditCollectionName(InfoBaseCollection ibc)
        {
            ibc.Name = Interaction.InputBox("Введите название списка:", "Список " + ibc.Name, ibc.Name);
            _cvInfoBaseCollections.Refresh();
            Config.ConfigurationData.Save();
        }

        private void lbInfoBaseCollections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selInfoBaseCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selInfoBaseCollection == null)
            {
                lbStoredInfoBases.ItemsSource = null;
                return;
            }


            _cvStoredInfoBases = CollectionViewSource.GetDefaultView(selInfoBaseCollection.InfoBaseList);
            _cvStoredInfoBases.SortDescriptions.Add(new SortDescription("InfobaseName", ListSortDirection.Ascending));
            lbStoredInfoBases.ItemsSource = _cvStoredInfoBases;

        }

        private void lbInfoBaseCollections_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selInfoBaseCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selInfoBaseCollection == null)
                return;

            EditCollectionName(selInfoBaseCollection);
        }

        private void btnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            var newCollection = new InfoBaseCollection {Name = "Новый список"};

            Config.ConfigurationData.CurrentPool.InfoBaseCollectionList.Add(newCollection);

            EditCollectionName(newCollection);
        }

        private void btnRemoveCollection_Click(object sender, RoutedEventArgs e)
        {
            var selInfoBaseCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selInfoBaseCollection == null)
                return;

            Config.ConfigurationData.CurrentPool.InfoBaseCollectionList.Remove(selInfoBaseCollection);
            Config.ConfigurationData.Save();
        }

#endregion
        
        #region stored_infobases

        private void btnAddStoredInfoBase_Click(object sender, RoutedEventArgs e)
        {
            var selInfoBaseCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selInfoBaseCollection == null)
                return;

            var ib = new InfoBase
            {
                InfobaseName = "Информационная база",
                Connect = "Srvr=\"\";Ref=\"\";",
                App = "Auto",
                Version = "8.3",
                Folder = "/",
                WA = "1"
            };

            selInfoBaseCollection.InfoBaseList.Add(ib);
            

            if (EditInfoBase(ib))
            {
                Config.ConfigurationData.Save();
                _cvStoredInfoBases.Refresh();
            } else
            {
                selInfoBaseCollection.InfoBaseList.Remove(ib);
            } 
        }

        private void btnRemoveStoredInfoBase_Click(object sender, RoutedEventArgs e)
        {
            var selStoredInfoBase = (lbStoredInfoBases.SelectedItem as InfoBase);

            if (selStoredInfoBase == null)
                return;

            var selInfoBaseCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selInfoBaseCollection == null)
                return;

            selInfoBaseCollection.InfoBaseList.Remove(selStoredInfoBase);
            Config.ConfigurationData.Save();
            _cvStoredInfoBases.Refresh();

        }


        #endregion

        private void lbStoredInfoBases_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selStoredInfoBase = (lbStoredInfoBases.SelectedItem as InfoBase);

            if (selStoredInfoBase == null)
                return;

            if (EditInfoBase(selStoredInfoBase))
            {
                Config.ConfigurationData.Save();
                _cvStoredInfoBases.Refresh();
            }            

        }
        
        private void lbStoredInfoBases_Label_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(lbStoredInfoBases, ((Label)sender).DataContext, DragDropEffects.Move);
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var ibt = ((TextBlock)sender).DataContext as InfoBaseTree;

            if(ibt == null)
                return;

            var ib = ibt.InfoBase;

            DragDrop.DoDragDrop(tvInfobases, ib, DragDropEffects.Move);
        }

        private void lbStoredInfoBases_DragOver(object sender, DragEventArgs e)
        {
            var selCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (e.Data.GetDataPresent(typeof(InfoBase)) && selCollection != null)
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void tvInfobases_DragOver(object sender, DragEventArgs e)
        {
            var selUser = (lbUsers.SelectedItem as User);

            if (e.Data.GetDataPresent(typeof(InfoBase)) && selUser != null)
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void lbStoredInfoBases_Drop(object sender, DragEventArgs e)
        {
            var selCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selCollection == null)
                return;

            var ib = e.Data.GetData(typeof(InfoBase)) as InfoBase;

            if (ib == null)
                return;

            selCollection.InfoBaseList.Add(new InfoBase(ib));

            Config.ConfigurationData.Save();
            _cvStoredInfoBases.Refresh();
        }

        private void tvInfobases_Drop(object sender, DragEventArgs e)
        {
            var selUser = (lbUsers.SelectedItem as User);

            if (selUser == null)
                return;

            var ib = e.Data.GetData(typeof(InfoBase)) as InfoBase;

            if (ib == null)
                return;

            var ibt = new InfoBaseTree(new InfoBase(ib), selUser.InfoBaseTree);

            selUser.InfoBaseTree.ChildInfoBases.Add(ibt);

            SaveInfoBases();
            _cvInfoBases.Refresh(); 
        }

        

        

        



        






    }
}
