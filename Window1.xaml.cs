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
using System.Windows.Shapes;

namespace NEW_QCtech
{
    /// <summary>
    /// Window1.xaml 的互動邏輯
    /// </summary>
    public partial class Window1 : Window
    {
       static public Window1 _windosw1;
        public Window1()
        {
            InitializeComponent();
        }

        private void label_Alert_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void CardsPanel_Setting(object sender, RoutedEventArgs e)
        {

        }

        private void Frame_MainWorkArea_Loaded(object sender, RoutedEventArgs e)
        {
            _windosw1 = new Window1();
            Frame_MainWorkArea.Navigate(new Page_Login());
        }
        public void LoginClick()
        {
           // Frame_MainWorkArea.Source = null;
           // Frame_MainWorkArea.Navigate(new Warning());
        }

        public void WarningClick()
        {
            this.Close();
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
