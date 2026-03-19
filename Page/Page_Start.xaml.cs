using System;
using System.Collections.Generic;
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

namespace NEW_QCtech
{
    /// <summary>
    /// Page_Start.xaml 的互動邏輯
    /// </summary>
    public partial class Page_Start : Page
    {
        public Page_Start()
        {
            InitializeComponent();
        }

        private void AAA_Click(object sender, RoutedEventArgs e)
        {
            Window1._windosw1.WarningClick();
        }
    }
}
