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
    /// Warning.xaml 的互動邏輯
    /// </summary>
    public partial class Warning : Page
    {
        public Warning()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Page_Start());
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;

            // 獲取當前的垂直滾動位置
            double scv = scrollViewer.VerticalOffset;

            double TotalHeight = scrollViewer.ScrollableHeight; // 獲取總的可滾動高度
            if (scv == TotalHeight)
            {
                btn_next.Visibility = Visibility.Visible;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            btn_next.Visibility = Visibility.Collapsed;
        }
    }
}
