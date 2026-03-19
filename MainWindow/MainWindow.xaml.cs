using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.MultiplotLayouts;
using ScottPlot.TickGenerators;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Data;
using System.Linq;
using System.Windows.Controls.Primitives;

// 避免與 System.Drawing.Color 衝突
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace NEW_QCtech
{
    /// <summary>
    /// MainWindow：主視窗
    /// 這裡實作了：
    /// - 自製 WindowChrome
    /// - 多螢幕 aware 最大化
    /// - DPI aware placement
    /// - ScottPlot 初始化
    /// - 自訂拖曳 TitleBar
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        // 儲存還原前視窗大小
        double MainWindow_originalWidth;
        double MainWindow_originalHeight;

        // true = normal , false = maximized
        bool mainWindowState = true;
        Page_PlotSetting page_PlotSetting;
        static public DataTable dt_dataGrid;
        static public MainWindow _mainWindow;
        private DataTable _displayGridTable = null;


        public DataGridLayoutConfig _gridLayoutConfig = new DataGridLayoutConfig();
        private bool _isApplyingMainRadio = false;

        private bool _isBuildingGrid = false;
        private bool _isHandlingMainRadio = false;
        private bool _isHandlingSubCheck = false;
        public MainWindow()
        {
            InitializeComponent();
            page_PlotSetting = new Page_PlotSetting();
            // 將控制面板 UserControl 塞入主內容區
            MW_ContentControl.Content = new UserControl_ControlPanel();
            _mainWindow = this;
            dt_dataGrid = new DataTable();
        }

        //自製最小化
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        #region WIN32_API

        // 找最接近視窗的螢幕
        const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // Monitor 結構
        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;   // 整個螢幕
            public RECT rcWork;      // 工作區（排除 taskbar）
            public int dwFlags;
        }

        // Win32 RECT
        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        // 自製最大化 / 還原
        private void RestoreDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindowState == true && this.WindowState == WindowState.Normal)
            {
                // 儲存還原尺寸
                MainWindow_originalWidth = this.ActualWidth;
                MainWindow_originalHeight = this.ActualHeight;

                // 強制 flush WPF layout，確保 HWND 已同步到目前螢幕
                Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

                // 取得 HWND
                var hwnd = new WindowInteropHelper(this).Handle;

                // 找目前所在螢幕
                IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                MONITORINFO info = new MONITORINFO();
                info.cbSize = Marshal.SizeOf(info);
                GetMonitorInfo(hMonitor, ref info);

                // 工作區（排除 taskbar）
                RECT rc = info.rcWork;

                // Pixel → WPF DIP
                var source = PresentationSource.FromVisual(this);
                double dpiX = source.CompositionTarget.TransformFromDevice.M11;
                double dpiY = source.CompositionTarget.TransformFromDevice.M22;

                // 套用目前螢幕最大化
                this.Left = rc.Left * dpiX;
                this.Top = rc.Top * dpiY;
                this.Width = (rc.Right - rc.Left) * dpiX;
                this.Height = (rc.Bottom - rc.Top) * dpiY;

                mainWindowState = false;
            }
            else if (this.WindowState == WindowState.Maximized && mainWindowState == false)
            {
                this.WindowState = WindowState.Normal;
                // 還原原始大小
                this.Width = MainWindow_originalWidth;
                this.Height = MainWindow_originalHeight;
                mainWindowState = true;
            }
            else if (mainWindowState == true && this.WindowState == WindowState.Normal)
            {
                this.Width = 1024;
                this.Height = 600;
                mainWindowState = true;
            }
        }

        // TitleBar 拖曳
        private void label_Alert_MouseMove(object sender, MouseEventArgs e)
        {
            // 若在 maximized 狀態拖曳，先還原再拖（Win11 行為）
            if (e.LeftButton == MouseButtonState.Pressed && mainWindowState == false)
            {
                this.Width = MainWindow_originalWidth;
                this.Height = MainWindow_originalHeight;

                Application.Current.MainWindow.Top = PointToScreen(Mouse.GetPosition(this)).Y;
                Application.Current.MainWindow.Left = PointToScreen(Mouse.GetPosition(this)).X - Mouse.GetPosition(this).X;

                this.DragMove();
            }
            else if (e.LeftButton == MouseButtonState.Pressed && mainWindowState == true)
            {
                this.DragMove();
            }
        }

        // 外框顏色依機台狀態變化
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

        // Hover 效果//靠近時出現UI
        private void borderRemove_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Border).Opacity = 0.7;
        }
        // Hover 效果//靠近時出現UI
        private void borderRemove_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Border).Opacity = 1.0;
        }

        // 控制面板伸縮
        private void border_StretchCtrlPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UserControl_ControlPanel._ControlPanel.StretchControlPanelUI();

            if (UserControl_ControlPanel.CtrlUI_status == "Expand")
                Text_StretchCtrlPanel.RenderTransform = new RotateTransform(0);
            else
                Text_StretchCtrlPanel.RenderTransform = new RotateTransform(180);
        }

        // 左側滑出控制面板感應
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point aa = e.GetPosition(this.GS_Left);
            double BsAcW = border_StretchCtrlPanel.ActualWidth * 2;

            if (Math.Abs(aa.X) < Math.Abs(BsAcW))
                border_StretchCtrlPanel.Visibility = Visibility.Visible;
            else
                border_StretchCtrlPanel.Visibility = Visibility.Hidden;
        }

        //呼叫曲線圖設定
        private void PlotSetting_MouseDown(object sender, MouseButtonEventArgs e)
        {

            PlotSetting_Frame.Navigate(page_PlotSetting);

            PlotSetting_Frame.Visibility = Visibility.Visible;
            btn_ClosePlot.Visibility = Visibility.Hidden;
            PlotSetting.Visibility = Visibility.Hidden;
        }

        //右上S按鈕
        private void btn_GlobalSetting_Click(object sender, RoutedEventArgs e)
        {
            GlobalSetting globalSetting = new GlobalSetting(null);
            globalSetting.ShowDialog();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        //關閉軟體
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

}
