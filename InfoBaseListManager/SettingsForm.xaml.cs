using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace InfoBaseListManager
{
    /// <summary>
    /// Логика взаимодействия для SettingsForm.xaml
    /// </summary>
    public partial class SettingsForm : Window
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = Config.ConfigurationData;
            lbPoolList.Items.Clear();
            lbPoolList.ItemsSource = Config.ConfigurationData.PoolList;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Config.ConfigurationData.PoolList.Add(new Pool());
            lbPoolList.Items.Refresh();
            lbPoolList.SelectedItem = Config.ConfigurationData.PoolList[Config.ConfigurationData.PoolList.Count - 1];
            
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if(lbPoolList.SelectedItem != null)
            {
                Config.ConfigurationData.PoolList.Remove((Pool)lbPoolList.SelectedItem);
                lbPoolList.Items.Refresh();                
            }

        }
                
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(Config.ConfigurationData.Port == 0)
            {
                MessageBox.Show("Задайте корректный порт");
                e.Cancel = true;
                return;
            }
            Config.ConfigurationData.Save();            
        }

        private void tbPoolName_GotFocus(object sender, RoutedEventArgs e)
        {
            lbPoolList.SelectedItem = (sender as TextBox).DataContext;
        }

        

 
    }
}
