using ScottPlot;
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
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace NEW_QCtech
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       

        public MainWindow()
        {
            InitializeComponent();
            MW_ContentControl.Content = new UserControl_ControlPanel();
        }
        public class TestRow
        {
            public string Column1 { get; set; }
            public string Column2 { get; set; }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dg.ItemsSource = new[]
            {
                new TestRow { Column1 = "Row 1 Col 1", Column2 = "Row 1 Col 2" },
                new TestRow { Column1 = "Row 2 Col 1", Column2 = "Row 2 Col 2" },
                new TestRow { Column1 = "Row 3 Col 1", Column2 = "Row 3 Col 2" }
            };
            var sig = WpfPlot1.Plot.Add.Signal(Generate.SquareWaveFromSines());
            sig.LineWidth = 3;
            sig.Color = new("#2b9433");
            sig.AlwaysUseLowDensityMode = true;

            WpfPlot1.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
            WpfPlot1.Plot.Axes.Color(ScottPlot.Colors.White);


            // set grid line colors
            WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(5);
            WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(5);

            // enable minor grid lines by defining a positive width
            WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
            WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;

            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();

        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Minimize the window
            this.WindowState = WindowState.Minimized;
        }

        private void RestoreDownButton_Click(object sender, RoutedEventArgs e)
        {
            // Restore down the window
            if(this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;


        }

        private void label_Alert_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //拖動窗口

        }

        private void label_Alert_MouseMove(object sender, MouseEventArgs e)
        {
            //拖動窗口
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

       
         public void Change_MainWindow_PeripheryGrid_Color(MachineStatus status)
        {
            switch (status)
            {
                case MachineStatus.MOTOR_IDLE:
                   MainWindow_PeripheryGrid.Background = new SolidColorBrush(Colors.LightGreen);
                    break;
                case MachineStatus.MOTOR_MOVING:
                    MainWindow_PeripheryGrid.Background = new SolidColorBrush(Colors.OrangeRed);
                    break;
            }
        }
        public enum MachineStatus
        {
           MOTOR_IDLE,
           MOTOR_MOVING
        }

        private void borderRemove_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            border.Opacity = 0.7;
        }

        private void borderRemove_MouseLeave(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            border.Opacity = 1.0;
        }


    }
}