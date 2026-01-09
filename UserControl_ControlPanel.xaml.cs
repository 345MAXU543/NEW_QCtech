using System;
using System.Collections.Generic;
using System.Reflection;
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
using WpfAnimatedGif;
using static NEW_QCtech.App;

namespace NEW_QCtech
{
    /// <summary>
    /// UserControl_ControlPanel.xaml 的互動邏輯
    /// </summary>
    public partial class UserControl_ControlPanel : UserControl
    {
        private readonly MainWindow _mainWindow;
        public UserControl_ControlPanel()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow;
        }

        //GIF播放(UP按鈕)
        private void grid_btnUP_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ImageBehavior.SetAnimatedSource(image01, GifCache.Get("UP"));
            image01.Stretch = Stretch.Fill;
            Mouse.Capture(grid_btnUP);

            _mainWindow.Change_MainWindow_PeripheryGrid_Color(MainWindow.MachineStatus.MOTOR_MOVING);
        }

        //GIF停止播放(在GIF上放開滑鼠)
        //private void imageBox_01_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    ImageBehavior.SetAnimatedSource(image01, null);
        //    _mainWindow.Change_MainWindow_PeripheryGrid_Color(MainWindow.MachineStatus.MOTOR_IDLE);
        //}

        private void grid_btnUP_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 停止 GIF
            ImageBehavior.SetAnimatedSource(image01, null);

            // 變回狀態
            _mainWindow.Change_MainWindow_PeripheryGrid_Color(
                MainWindow.MachineStatus.MOTOR_IDLE);

            // ⭐釋放滑鼠
            Mouse.Capture(null);
        }

        private void grid_More_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Width = this.ActualWidth * 4;
            grid_More.Visibility = Visibility.Hidden;
            grid_BackToSmallSize.Visibility = Visibility.Visible;

            grid_01.Visibility = Visibility.Visible;
            grid_02.Visibility = Visibility.Visible;
        }

        private void grid_BackToSmallSize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Width = grid_btnUP.ActualWidth * 1.1;
            grid_More.Visibility = Visibility.Visible;
            grid_BackToSmallSize.Visibility = Visibility.Hidden;

            grid_01.Visibility = Visibility.Collapsed;
            grid_02.Visibility = Visibility.Collapsed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = grid_btnUP.ActualWidth * 1.1;
            grid_More.Visibility = Visibility.Visible;
            grid_BackToSmallSize.Visibility = Visibility.Hidden;

            grid_01.Visibility = Visibility.Collapsed;
            grid_02.Visibility = Visibility.Collapsed;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            grid_main.Width = this.ActualWidth;
        }

        private void slider_ManualVelocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Textbox_ManualVelocity.Text = ((int)slider_ManualVelocity.Value).ToString();
        }

       
    }
}
