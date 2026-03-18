using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NEW_QCtech
{
    /// <summary>
    /// Page_Login.xaml 的互動邏輯
    /// </summary>
    public partial class Page_Login : Page
    {
        public Page_Login()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 淡入動畫
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            // 滑入動畫
            DoubleAnimation slideIn = new DoubleAnimation
            {
                From = 200,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            RootGrid.BeginAnimation(OpacityProperty, fadeIn);
            PageTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Window1. _windosw1.LoginClick();
            this.NavigationService.Navigate(new Warning());
        }
    }
}
