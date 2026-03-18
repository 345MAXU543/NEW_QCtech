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
    /// Page_PlotSetting.xaml 的互動邏輯
    /// </summary>
    public partial class Page_PlotSetting : Page
    {
        public Page_PlotSetting()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow._mainWindow.PlotSetting_Frame.Navigate(null);
            MainWindow._mainWindow.PlotSetting_Frame.Refresh();
            MainWindow._mainWindow.PlotSetting_Frame.Visibility = Visibility.Collapsed;
            MainWindow._mainWindow.btn_ClosePlot.Visibility = Visibility.Visible;
            MainWindow._mainWindow.PlotSetting.Visibility = Visibility.Visible;

            //感交差 而且只是DEMO
            if(RadioBtn_01.IsChecked == true)
            {
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot1, 0);
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot2, 2);
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[1].Width =  GridLength.Auto;
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[2].Width = new GridLength(0);
            }
            else if (RadioBtn_02.IsChecked == true)
            {
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot1, 0);
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot2, 2);
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[0].Width = new GridLength(0);
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[1].Width = GridLength.Auto;
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            }
            else if (RadioBtn_03.IsChecked == true)
            {
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[1].Width = GridLength.Auto;
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                //調換WpfPlot1與WpfPlot2在 MainWindow._mainWindow.plotGrid的Column
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot1, 0);
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot2, 2);


            }
            else if (RadioBtn_04.IsChecked == true)
            {
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[1].Width = GridLength.Auto;
                MainWindow._mainWindow.plotGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot1, 2);
                Grid.SetColumn(MainWindow._mainWindow.WpfPlot2, 0);
            }

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PlayEnterAnimation();
        }

        void PlayEnterAnimation()
        {
            // 下滑動畫
            DoubleAnimation slide = new DoubleAnimation
            {
                From = -300,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            SlideTransform.BeginAnimation(TranslateTransform.YProperty, slide);

            // 淡入動畫
            DoubleAnimation fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Grid01.BeginAnimation(OpacityProperty, fade);
        }
    }
}
