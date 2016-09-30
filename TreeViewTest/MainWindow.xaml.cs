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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace TreeViewTest
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public class NestedSet
    {
        public string Name { get; set; }
        public ObservableCollection<NestedSet> SubSet { get; set; }           
    }



    public class Node
    {
        public string Name { get; set; }
        public ObservableCollection<Node> Nodes { get; set; }
    }

    public partial class MainWindow : Window
    {
        ObservableCollection<NestedSet> SubSet;
        ObservableCollection<Node> nodes;
        public MainWindow()
        {
            

                                  
            InitializeComponent();

            
            SubSet = new ObservableCollection<NestedSet>
            {
                new NestedSet
                {
                    Name = "1",
                    SubSet = new ObservableCollection<NestedSet>
                    {
                        new NestedSet {Name = "2"},
                        new NestedSet {Name = "3"}
                    }
                },
                new NestedSet {Name = "4"}
            };

            tv.ItemsSource = SubSet;
                      

            nodes = new ObservableCollection<Node>
            {
                new Node
                {
                    Name ="Европа",
                    Nodes = new ObservableCollection<Node>
                    {
                        new Node {Name="Германия" },
                        new Node {Name="Франция" },
                        new Node
                        {
                            Name ="Великобритания",
                            Nodes = new ObservableCollection<Node>
                            {
                                new Node {Name="Англия" },
                                new Node {Name="Шотландия" },
                                new Node {Name="Уэльс" },
                                new Node {Name="Сев. Ирландия" },
                            }
                        }
                    }
                },
                new Node
                {
                    Name ="Азия",
                    Nodes = new ObservableCollection<Node>
                    {
                        new Node {Name="Китай" },
                        new Node {Name="Япония" },
                        new Node { Name ="Индия" }
                    }
                },
                new Node { Name="Африка" },
                new Node { Name="Америка" },
                new Node { Name="Австралия" }
            };
            treeView1.ItemsSource = nodes;

            
        }
    }
}
