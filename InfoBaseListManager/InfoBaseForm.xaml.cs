using System;
using System.Collections.Generic;
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
        public InfoBase SourceInfoBase { get; set; }
        public IEnumerable<InfoBase> IBListForSearch { get; set; }

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
            if (IBListForSearch != null)
            {
                foreach (var ib in IBListForSearch)
                {
                    if (!ReferenceEquals(ib, SourceInfoBase) && ib.InfobaseName.Equals(InfoBase.InfobaseName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        MessageBox.Show("Информационная база с таким наименованием уже существует", "Неверное имя базы",
                            MessageBoxButton.OK);

                        return;
                    }
                }
            }
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
