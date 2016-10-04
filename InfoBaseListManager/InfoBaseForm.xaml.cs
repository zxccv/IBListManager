using System.Windows;
using InfoBaseListDataClasses;

namespace InfoBaseListManager
{
    /// <summary>
    /// Логика взаимодействия для InfoBaseForm.xaml
    /// </summary>
    public partial class InfoBaseForm
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
