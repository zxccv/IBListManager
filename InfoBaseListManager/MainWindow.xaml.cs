using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using InfoBaseListUDPServerNamespace;

namespace InfoBaseListManager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Computer> comps;
        private ICollectionView cvComps;
        private ICollectionView cvUsers;
        private ICollectionView cvInfoBases;

        private ICollectionView cvInfoBaseCollections;
        private ICollectionView cvStoredInfoBases;

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

            if(Config.ConfigurationData.PoolList.Count == 0)
            {
                var pool = new Pool();
                pool.Name = "Организация";
                Config.ConfigurationData.PoolList.Add(pool);
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

            comps = new ObservableCollection<Computer>();

            cvComps = CollectionViewSource.GetDefaultView(comps);
            cvComps.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
            lbComps.ItemsSource = cvComps;

            cvInfoBaseCollections = CollectionViewSource.GetDefaultView(Config.ConfigurationData.CurrentPool.InfoBaseCollectionList);
            cvInfoBaseCollections.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            lbInfoBaseCollections.ItemsSource = cvInfoBaseCollections;
            
            lbUsers.ItemsSource = null;
            tvInfobases.ItemsSource = null;

            if (_backgroundTasks != null)
                _backgroundTasks.Stop();
            _backgroundTasks = new BackgroundTasks(_udpServer, comps, cvComps);
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

            cvUsers = CollectionViewSource.GetDefaultView(selComp.Users);
            cvUsers.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));            
            lbUsers.ItemsSource = cvUsers;
            
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

            cvInfoBases = CollectionViewSource.GetDefaultView(selUser.InfoBaseTree.ChildInfoBases);
            cvInfoBases.SortDescriptions.Add(new SortDescription("InfoBaseName", ListSortDirection.Ascending));            
            tvInfobases.ItemsSource = cvInfoBases;
        }

        private bool EditInfoBase(InfoBaseListDataClasses.InfoBase ib)
        {
            var ibCopy = new InfoBaseListDataClasses.InfoBase(ib);
            InfoBaseForm ibForm = new InfoBaseForm{ InfoBase = ibCopy};

            if((bool)ibForm.ShowDialog())
            {
                var typeInfoBase = typeof(InfoBaseListDataClasses.InfoBase);
                var typeInfoBaseFields = typeInfoBase.GetProperties();

                foreach (var typeInfoBaseField in typeInfoBaseFields)
                {
                    typeInfoBaseField.SetValue(ib, typeInfoBaseField.GetValue(ibCopy,null), null);                    
                }
                                
                return true;
            }

            return false;
        }

        private void tvInfobases_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selInfoBaseTree = (tvInfobases.SelectedItem as InfoBaseTree);

            if (selInfoBaseTree == null)
                return;

            var selInfoBase = selInfoBaseTree.InfoBase;

            EditInfoBase(selInfoBase);
            SaveInfoBases();
            cvInfoBases.Refresh();
        }
        
        

        

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selUser = (lbUsers.SelectedItem as User);

            if (selUser == null)
            {
                return;
            }  

            var ib = new InfoBaseListDataClasses.InfoBase();
            ib.InfobaseName = "Информационная база";
            ib.Connect = "Srvr=\"\";Ref=\"\";";
            ib.App = "Auto";
            ib.Version = "8.3";
            ib.Folder = "/";
            ib.WA = "1";

            var ibt = new InfoBaseTree(ib, selUser.InfoBaseTree);

            selUser.InfoBaseTree.ChildInfoBases.Add(ibt);
                       
            if(EditInfoBase(ib))
            {
                SaveInfoBases();
                cvInfoBases.Refresh();
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
            ibc.Name = Microsoft.VisualBasic.Interaction.InputBox("Введите название списка:", "Список " + ibc.Name, ibc.Name);
            cvInfoBaseCollections.Refresh();
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


            cvStoredInfoBases = CollectionViewSource.GetDefaultView(selInfoBaseCollection.InfoBaseList);
            cvStoredInfoBases.SortDescriptions.Add(new SortDescription("InfobaseName", ListSortDirection.Ascending));
            lbStoredInfoBases.ItemsSource = cvStoredInfoBases;

        }

        private void lbInfoBaseCollections_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selInfoBaseCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selInfoBaseCollection == null)
                return;

            EditCollectionName(selInfoBaseCollection);
        }

        private void btnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            var newCollection = new InfoBaseCollection();
            newCollection.Name = "Новый список";

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

            var ib = new InfoBaseListDataClasses.InfoBase();
            ib.InfobaseName = "Информационная база";
            ib.Connect = "Srvr=\"\";Ref=\"\";";
            ib.App = "Auto";
            ib.Version = "8.3";
            ib.Folder = "/";
            ib.WA = "1";

            selInfoBaseCollection.InfoBaseList.Add(ib);
            

            if (EditInfoBase(ib))
            {
                Config.ConfigurationData.Save();
                cvStoredInfoBases.Refresh();
            } else
            {
                selInfoBaseCollection.InfoBaseList.Remove(ib);
            } 
        }

        private void btnRemoveStoredInfoBase_Click(object sender, RoutedEventArgs e)
        {
            var selStoredInfoBase = (lbStoredInfoBases.SelectedItem as InfoBaseListDataClasses.InfoBase);

            if (selStoredInfoBase == null)
                return;

            var selInfoBaseCollection = (lbInfoBaseCollections.SelectedItem as InfoBaseCollection);

            if (selInfoBaseCollection == null)
                return;

            selInfoBaseCollection.InfoBaseList.Remove(selStoredInfoBase);
            Config.ConfigurationData.Save();
            cvStoredInfoBases.Refresh();

        }


        #endregion

        private void lbStoredInfoBases_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selStoredInfoBase = (lbStoredInfoBases.SelectedItem as InfoBaseListDataClasses.InfoBase);

            if (selStoredInfoBase == null)
                return;

            if (EditInfoBase(selStoredInfoBase))
            {
                Config.ConfigurationData.Save();
                cvStoredInfoBases.Refresh();
            }            

        }

        private void Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //if (((System.Windows.Controls.Label)sender).DataContext != lbStoredInfoBases.SelectedItem)
            //    return;
            DragDrop.DoDragDrop(lbStoredInfoBases, ((System.Windows.Controls.Label)sender).DataContext, DragDropEffects.Move);
        }

        






    }
}
