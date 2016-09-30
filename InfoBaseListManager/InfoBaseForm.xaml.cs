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
using InfoBaseListDataClasses;

namespace InfoBaseListManager
{
    /// <summary>
    /// Логика взаимодействия для InfoBaseForm.xaml
    /// </summary>
    public partial class InfoBaseForm : Window
    {
        public InfoBase InfoBase { get; set; }

        public InfoBaseForm()
        {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            grid.DataContext = InfoBase;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
