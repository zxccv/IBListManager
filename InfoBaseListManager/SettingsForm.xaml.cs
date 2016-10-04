using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace InfoBaseListManager
{
    /// <summary>
    /// Логика взаимодействия для SettingsForm.xaml
    /// </summary>
    public partial class SettingsForm
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
                
        private void Window_Closing(object sender, CancelEventArgs e)
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
            var textBox = sender as TextBox;
            if (textBox != null) lbPoolList.SelectedItem = textBox.DataContext;
        }
    }
}
