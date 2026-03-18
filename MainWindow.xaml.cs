using NEW_QCtech.dataGrid.Models;
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
using NEW_QCtech.dataGrid.Models;
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



        // 關閉程式
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        public static void PlotSemilogX(ScottPlot.WPF.WpfPlot wpfPlot, double[] freqHz, double[] dB)
        {
            // log X 軸：freq 必須 > 0
            var pairs = freqHz.Zip(dB, (f, y) => (f, y))
                .Where(p => double.IsFinite(p.f) && double.IsFinite(p.y) && p.f > 0)
                .ToArray();

            double[] xLog10 = pairs.Select(p => Math.Log10(p.f)).ToArray();
            double[] y = pairs.Select(p => p.y).ToArray();

            var plt = wpfPlot.Plot;
            plt.Clear();

            plt.Add.Scatter(xLog10, y);

            // X 軸刻度顯示成 10^n 對應的 Hz（1,10,100,1000,10000）
            static string HzLabel(double log10Value)
            {
                double hz = Math.Pow(10, log10Value);
                // 你想要顯示 1, 10, 100, 1k, 10k 也可以在這裡客製
                if (hz >= 1000 && hz < 10000) return $"{hz / 1000:0}k";
                if (hz >= 10000) return $"{hz / 1000:0}k";
                return $"{hz:0}";
            }

            var xTickGen = new NumericAutomatic()
            {
                IntegerTicksOnly = true,
                LabelFormatter = value =>
                {
                    int exp = (int)Math.Round(value);

                    string s = exp.ToString();
                    s = s.Replace("-", "⁻")
                         .Replace("0", "⁰")
                         .Replace("1", "¹")
                         .Replace("2", "²")
                         .Replace("3", "³")
                         .Replace("4", "⁴")
                         .Replace("5", "⁵")
                         .Replace("6", "⁶")
                         .Replace("7", "⁷")
                         .Replace("8", "⁸")
                         .Replace("9", "⁹");

                    return $"10{s}";
                },
                MinorTickGenerator = new LogDecadeMinorTickGenerator(),
            };

            plt.Axes.Bottom.TickGenerator = xTickGen;

            // 標題/標籤（照你圖的風格）

            plt.Axes.Bottom.Label.Text = "頻率 Hz";
            plt.Axes.Left.Label.Text = "音壓位準 dB";

            // 指定中文字型：
            plt.Axes.Bottom.Label.FontName = "Microsoft JhengHei UI"; // 微軟正黑體
            plt.Axes.Left.Label.FontName = "Microsoft JhengHei UI";

            // 可選：字體大小
            plt.Axes.Bottom.Label.FontSize = 16;
            plt.Axes.Left.Label.FontSize = 16;

            // plt.Axes.AutoScale();
            wpfPlot.Refresh();
        }

        // 最小化視窗
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

        // Hover 效果
        private void borderRemove_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Border).Opacity = 0.7;
        }

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

        private void PlotSetting_MouseDown(object sender, MouseButtonEventArgs e)
        {

            PlotSetting_Frame.Navigate(page_PlotSetting);

            PlotSetting_Frame.Visibility = Visibility.Visible;
            btn_ClosePlot.Visibility = Visibility.Hidden;
            PlotSetting.Visibility = Visibility.Hidden;
        }


        private void btn_GlobalSetting_Click(object sender, RoutedEventArgs e)
        {
            GlobalSetting globalSetting = new GlobalSetting(null);
            globalSetting.ShowDialog();
        }

        // 以下為卡片拖曳相關事件(即時顯示)
        #region 卡片拖曳
        private Point _dragStart;
        private UIElement? _draggedCard;
        private void CardsPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(CardsPanel);
            _draggedCard = FindCardContainer(e.OriginalSource as DependencyObject);

            // Debug：看你有沒有抓到卡
            // if (_draggedCard == null) MessageBox.Show("沒抓到卡");
        }

        private void CardsPanel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_draggedCard == null) return;

            var pos = e.GetPosition(CardsPanel);
            var dx = Math.Abs(pos.X - _dragStart.X);
            var dy = Math.Abs(pos.Y - _dragStart.Y);

            if (dx < SystemParameters.MinimumHorizontalDragDistance &&
                dy < SystemParameters.MinimumVerticalDragDistance)
                return;

            // 用自訂格式最穩（不要直接丟 UIElement 也可以，但這個更不容易被誤判）
            var data = new DataObject("CARD", _draggedCard);
            DragDrop.DoDragDrop(CardsPanel, data, DragDropEffects.Move);
        }

        private void CardsPanel_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent("CARD") ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void CardsPanel_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("CARD")) return;

            var sourceCard = e.Data.GetData("CARD") as UIElement;
            if (sourceCard == null) return;

            var targetCard = FindCardContainer(e.OriginalSource as DependencyObject);

            int oldIndex = CardsPanel.Children.IndexOf(sourceCard);
            if (oldIndex < 0) return;

            // 丟到空白處：放最後
            if (targetCard == null)
            {
                CardsPanel.Children.Remove(sourceCard);
                CardsPanel.Children.Add(sourceCard);
                _draggedCard = null;
                return;
            }

            int targetIndex = CardsPanel.Children.IndexOf(targetCard);
            if (targetIndex < 0 || targetIndex == oldIndex)
            {
                _draggedCard = null;
                return;
            }

            // 進階：看滑鼠落在 target 左半或右半，決定插前或插後（拖曳體驗會好很多）
            bool insertAfter = IsMouseOnRightHalfOfTarget(e, targetCard);

            int insertIndex = targetIndex + (insertAfter ? 1 : 0);

            // 先移除，再插入（注意移除造成 index 位移）
            CardsPanel.Children.Remove(sourceCard);

            if (insertIndex > oldIndex) insertIndex--; // oldIndex 被移除後，後面的 index 會往前縮

            // 防呆
            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > CardsPanel.Children.Count) insertIndex = CardsPanel.Children.Count;

            CardsPanel.Children.Insert(insertIndex, sourceCard);

            _draggedCard = null;
        }

        private UIElement? FindCardContainer(DependencyObject? d)
        {
            while (d != null)
            {
                if (d is Border b && Equals(b.Tag, "Card"))
                    return b;

                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }

        private bool IsMouseOnRightHalfOfTarget(DragEventArgs e, UIElement target)
        {
            var p = e.GetPosition(target);
            if (target is FrameworkElement fe && fe.ActualWidth > 0)
                return p.X > fe.ActualWidth / 2.0;

            return false;
        }

        #endregion


        private void CardsPanel_Setting(object sender, RoutedEventArgs e)
        {
            GlobalSetting globalSetting = new GlobalSetting("CardsPanel_Setting");
            globalSetting.ShowDialog();
        }


        /// <summary>
        /// 視窗載入完成：
        /// 1) 產生 DataGrid 測試資料 + 記住每個 index 顏色
        /// 2) 初始化三組 base 資料
        /// 3) 初始化兩張 plot 的外觀
        /// 4) 畫第一次
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dg.LoadingRow -= dg_LoadingRow;
            dg.LoadingRow += dg_LoadingRow;

            // 1. 先把 plot 基礎資料準備好
            plotData1 = Generate.SquareWaveFromSines();
            plotData2 = Generate.Sin(10_000);
            plotData3 = new double[] { 5, 10, 7, 13 };

            // 2. 先決定左右圖模式/資料集
            leftDataSet = 1;
            rightDataSet = 1;

            // 3. 建假資料 + 套 DataGrid
            GenerateSampleGridData();
           InitDefaultGridLayoutIfNeeded();
            ApplyGridLayoutToMainGrid();

            // 4. 把 DataGrid 狀態同步回 plot 狀態
            SyncPlotStateFromGrid();

            // 5. 再初始化 plot 外觀
            InitPlots();

            // 6. 最後再畫
            RedrawAllPlots();
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot2.Plot.Axes.AutoScale();

            btn_leftPlot1.Background = new SolidColorBrush(Colors.Gray);
            btn_RightPlot2.Background = new SolidColorBrush(Colors.Gray);
        }

        private void InitDefaultGridLayoutIfNeeded()
        {
            if (_gridLayoutConfig == null)
                _gridLayoutConfig = new DataGridLayoutConfig();

            if (_gridLayoutConfig.SelectedColumns == null)
                _gridLayoutConfig.SelectedColumns = new List<ColumnConfig>();

            if (_gridLayoutConfig.Grouping == null)
                _gridLayoutConfig.Grouping = new GroupConfig();

            if (_gridLayoutConfig.SelectedColumns.Count > 0)
                return;

            _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig
            {
                FieldId = "Main",
                Header = "Main",
                UIType = ColumnFieldUIType.RadioButton
            });

            _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig
            {
                FieldId = "Sub",
                Header = "Sub",
                UIType = ColumnFieldUIType.CheckBox
            });

            _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig
            {
                FieldId = "ColorBrush",
                Header = "color",
                UIType = ColumnFieldUIType.Color
            });

            _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig
            {
                FieldId = "Index",
                Header = "Index",
                UIType = ColumnFieldUIType.Text
            });

            _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig
            {
                FieldId = "Name",
                Header = "Name",
                UIType = ColumnFieldUIType.Text
            });

            _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig
            {
                FieldId = "Force",
                Header = "Force",
                UIType = ColumnFieldUIType.Text
            });

            _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig
            {
                FieldId = "Disp",
                Header = "Disp",
                UIType = ColumnFieldUIType.Text
            });
        }

        private void GenerateSampleGridData()
        {
            dt_dataGrid = new DataTable();

            dt_dataGrid.Columns.Add("Main", typeof(bool));
            dt_dataGrid.Columns.Add("Sub", typeof(bool));
            dt_dataGrid.Columns.Add("ColorBrush", typeof(Brush));
            dt_dataGrid.Columns.Add("Index", typeof(int));
            dt_dataGrid.Columns.Add("Name", typeof(string));
            dt_dataGrid.Columns.Add("Force", typeof(double));
            dt_dataGrid.Columns.Add("Disp", typeof(double));

            Random rnd = new Random();

            for (int i = 0; i < 20; i++)
            {
                bool isMain = (i == 0);
                bool isSub = (i % 2 == 0);

                Brush color =
                    i < 10
                    ? new SolidColorBrush(Colors.Orange)
                    : new SolidColorBrush(Colors.LightGreen);

                double force = 10 + rnd.NextDouble() * 10;
                double disp = 50 + rnd.NextDouble() * 5;

                dt_dataGrid.Rows.Add(
                    isMain,
                    isSub,
                    color,
                    i,
                    "Test " + i,
                    force,
                    disp
                );
            }
        }

        #region Main DataGrid (DataTable 版)

        private void InitMainGridTable()
        {
            dt_dataGrid.Clear();
            dt_dataGrid.Columns.Clear();

            dt_dataGrid.Columns.Add("Main", typeof(bool));
            dt_dataGrid.Columns.Add("Sub", typeof(bool));
            dt_dataGrid.Columns.Add("ColorBrush", typeof(Brush));
            dt_dataGrid.Columns.Add("Index", typeof(int));
            dt_dataGrid.Columns.Add("Name", typeof(string));
            dt_dataGrid.Columns.Add("Force", typeof(double));
            dt_dataGrid.Columns.Add("Disp", typeof(double));

            Random rnd = new Random(1);

            for (int i = 0; i < 20; i++)
            {
                bool isMain = (i == 0);
                bool isSub = (i % 2 == 0);
                Brush brush = new SolidColorBrush(i % 2 == 0 ? Colors.Orange : Colors.LightGreen);

                if (i >= 10)
                    brush = new SolidColorBrush(i % 2 == 0 ? Colors.Cyan : Colors.MediumPurple);

                dt_dataGrid.Rows.Add(
                    isMain,
                    isSub,
                    brush,
                    i,
                    "Test " + i,
                    i < 10 ? 10 + rnd.NextDouble() * 5 : 20 + rnd.NextDouble() * 7,
                    i < 10 ? 50 + rnd.NextDouble() * 3 : 55 + rnd.NextDouble() * 4
                );
            }
        }

        //private void InitDefaultGridLayoutIfNeeded()
        //{
        //    if (_gridLayoutConfig == null)
        //        _gridLayoutConfig = new DataGridLayoutConfig();

        //    if (_gridLayoutConfig.SelectedColumns == null)
        //        _gridLayoutConfig.SelectedColumns = new List<ColumnConfig>();

        //    if (_gridLayoutConfig.SelectedColumns.Count > 0)
        //        return;

        //    _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig { FieldId = "Main", Header = "Main", UIType = ColumnFieldUIType.RadioButton });
        //    _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig { FieldId = "Sub", Header = "Sub", UIType = ColumnFieldUIType.CheckBox });
        //    _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig { FieldId = "ColorBrush", Header = "color", UIType = ColumnFieldUIType.Color });
        //    _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig { FieldId = "Index", Header = "Index", UIType = ColumnFieldUIType.Text });
        //    _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig { FieldId = "Name", Header = "Name", UIType = ColumnFieldUIType.Text });
        //    _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig { FieldId = "Force", Header = "Force", UIType = ColumnFieldUIType.Text });
        //    _gridLayoutConfig.SelectedColumns.Add(new ColumnConfig { FieldId = "Disp", Header = "Disp", UIType = ColumnFieldUIType.Text });
        //}

        //private void ApplyGridLayoutToMainWindow()
        //{
        //    if (dt_dataGrid == null)
        //        return;

        //    if (_gridLayoutConfig == null)
        //        InitDefaultGridLayoutIfNeeded();

        //    BuildMainGridColumns();

        //    dg.ItemsSource = null;
        //    dg.ItemsSource = dt_dataGrid.DefaultView;
        //    dg.Items.Refresh();
        //}

        private void BuildMainGridColumns()
        {
            dg.Columns.Clear();

            if (_gridLayoutConfig == null || _gridLayoutConfig.SelectedColumns == null)
                return;

            for (int i = 0; i < _gridLayoutConfig.SelectedColumns.Count; i++)
            {
                ColumnConfig cfg = _gridLayoutConfig.SelectedColumns[i];
                DataGridColumn col = CreateMainGridColumn(cfg);

                if (col == null)
                    continue;

                col.DisplayIndex = i;
                dg.Columns.Add(col);
            }
        }

        private DataGridColumn CreateMainGridColumn(ColumnConfig cfg)
        {
            if (cfg == null) return null;

            string fieldId = cfg.FieldId ?? "";
            string header = string.IsNullOrWhiteSpace(cfg.Header) ? fieldId : cfg.Header;

            if (fieldId == "Main")
                return CreateMainRadioColumn(header);

            if (fieldId == "Sub")
                return CreateSubCheckColumn(header);

            if (fieldId == "ColorBrush")
                return CreateColorButtonColumn(header);

            if (fieldId == "Index")
                return CreateTextColumn(header, "Index", 80);

            if (fieldId == "Name")
                return CreateTextColumn(header, "Name", new DataGridLength(1, DataGridLengthUnitType.Star));

            if (fieldId == "Force")
                return CreateTextColumn(header, "Force", 120, "{0:0.###}");

            if (fieldId == "Disp")
                return CreateTextColumn(header, "Disp", 120, "{0:0.###}");

            // 其他文字欄位預設
            if (dt_dataGrid.Columns.Contains(fieldId))
                return CreateTextColumn(header, fieldId, 120);

            return null;
        }

        private DataGridTemplateColumn CreateMainRadioColumn(string header)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();
            col.Header = header;
            col.Width = 60;
            col.CellTemplate = BuildMainRadioTemplate();
            return col;
        }

        private DataGridTemplateColumn CreateSubCheckColumn(string header)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();
            col.Header = header;
            col.Width = 60;
            col.CellTemplate = BuildSubCheckTemplate();
            return col;
        }

        private DataGridTemplateColumn CreateColorButtonColumn(string header)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();
            col.Header = header;
            col.Width = 60;
            col.CellTemplate = BuildColorButtonTemplate();
            return col;
        }

        private DataGridTextColumn CreateTextColumn(string header, string fieldName, double width, string stringFormat = null)
        {
            DataGridTextColumn col = new DataGridTextColumn();
            col.Header = header;
            col.Width = width;

            Binding binding = new Binding("[" + fieldName + "]");
            if (!string.IsNullOrWhiteSpace(stringFormat))
                binding.StringFormat = stringFormat;

            col.Binding = binding;
            return col;
        }

        private DataGridTextColumn CreateTextColumn(string header, string fieldName, DataGridLength width, string stringFormat = null)
        {
            DataGridTextColumn col = new DataGridTextColumn();
            col.Header = header;
            col.Width = width;

            Binding binding = new Binding("[" + fieldName + "]");
            if (!string.IsNullOrWhiteSpace(stringFormat))
                binding.StringFormat = stringFormat;

            col.Binding = binding;
            return col;
        }

        private DataTemplate BuildMainRadioTemplate()
        {
            FrameworkElementFactory radio = new FrameworkElementFactory(typeof(RadioButton));
            radio.SetValue(RadioButton.GroupNameProperty, "MainGroup");
            radio.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            radio.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);

            Binding binding = new Binding("[Main]");
            binding.Mode = BindingMode.OneWay;
            radio.SetBinding(ToggleButton.IsCheckedProperty, binding);

            radio.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(dg_Radio_Checked));

            DataTemplate template = new DataTemplate();
            template.VisualTree = radio;
            return template;
        }

        private DataTemplate BuildSubCheckTemplate()
        {
            FrameworkElementFactory chk = new FrameworkElementFactory(typeof(CheckBox));
            chk.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            chk.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            chk.SetValue(FrameworkElement.StyleProperty, dg.Resources["SafeCheckBoxStyle"]);

            Binding binding = new Binding("[Sub]");
            binding.Mode = BindingMode.OneWay;
            chk.SetBinding(ToggleButton.IsCheckedProperty, binding);

            chk.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(dg_Check_Changed));
            chk.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(dg_Check_Changed));

            DataTemplate template = new DataTemplate();
            template.VisualTree = chk;
            return template;
        }

        private DataTemplate BuildColorButtonTemplate()
        {
            FrameworkElementFactory btn = new FrameworkElementFactory(typeof(Button));
            btn.SetValue(Button.WidthProperty, 60.0);
            btn.SetValue(HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);

            Binding binding = new Binding("[ColorBrush]");
            binding.Mode = BindingMode.OneWay;
            btn.SetBinding(Button.BackgroundProperty, binding);

            btn.AddHandler(Button.ClickEvent, new RoutedEventHandler(dg_colorGhange));

            DataTemplate template = new DataTemplate();
            template.VisualTree = btn;
            return template;
        }

        #endregion
        #region scottPlot相關
        private enum PlotMode { Single = 1, Multi = 2, Other = 3 }

        private PlotMode leftMode = PlotMode.Single;   // WpfPlot1 的模式
        private PlotMode rightMode = PlotMode.Multi;   // WpfPlot2 的模式（你想預設多線就這樣）
        //
        // 使用方式：
        // 1) 左邊 tab 控 WpfPlot1 的資料集（1/2/3）
        // 2) 右邊 tab 控 WpfPlot2 的資料集（1/2/3）
        // 3) Plot1 用 Radio 單選 index 畫一條
        // 4) Plot2 用 CheckBox 複選 index 畫多條
        // 5) 顏色由 DataGrid 的 ColorPicker 決定
        //

        // ===== 三組 base 資料（你之後換成真資料也只改這裡）=====
        private double[] plotData1, plotData2, plotData3;

        // ===== 左右兩張圖各自的 tab 狀態（資料集編號 1/2/3）=====
        private int leftDataSet = 1;   // WpfPlot1 用
        private int rightDataSet = 1;  // WpfPlot2 用

        // ===== Plot1 單選 index（Radio）=====
        private int currentRadioIndex = 1;

        // ===== Plot2 複選 index（CheckBox）=====
        private readonly HashSet<int> checkedIndexSet = new();

        // ===== index 對應顏色（避免每次去 dg.Items 找）=====
        private readonly Dictionary<int, Color> indexColor = new();

        // ===== 快取：避免每次重畫都重新算 10000 點（資料集/idx 不變就重用）=====
        private readonly Dictionary<(int dataSet, int idx), double[]> lineCache = new();


        /// <summary>
        /// 初始化兩張圖的外觀（背景透明、白色座標軸、淡 grid）
        /// </summary>
        private void InitPlots()
        {
            // Plot1 外觀
            WpfPlot1.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
            WpfPlot1.Plot.Axes.Color(ScottPlot.Colors.White);
            WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(30);
            WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(30);
            WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
            WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;

            // Plot2 外觀
            WpfPlot2.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
            WpfPlot2.Plot.Axes.Color(ScottPlot.Colors.White);
            WpfPlot2.Plot.Grid.XAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(30);
            WpfPlot2.Plot.Grid.YAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(30);
            WpfPlot2.Plot.Grid.XAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            WpfPlot2.Plot.Grid.YAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            WpfPlot2.Plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
            WpfPlot2.Plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;
        }

        /// <summary>
        /// 取得某個資料集 (1/2/3) 的 base 資料
        /// </summary>
        private double[] GetBaseData(int dataSet)
        {
            if (dataSet == 1 && plotData1 != null) return plotData1;
            if (dataSet == 2 && plotData2 != null) return plotData2;
            if (dataSet == 3 && plotData3 != null) return plotData3;

            return Array.Empty<double>();
        }

        /// <summary>
        /// 由 base 資料 + idx 產生「某條線」
        /// 你目前沒有真資料結構（每個 idx 一條真資料），所以先用簡單可讀的變形方式：
        /// - scale：每個 idx 稍微放大/縮小
        /// - offset：每個 idx 稍微上移/下移
        /// 這樣 tab 切資料會真的改形狀，idx 也會真的不一樣，而且不會每次重畫亂跳。
        /// </summary>
        private double[] GetLineData(int dataSet, int idx)
        {
            if (lineCache.TryGetValue((dataSet, idx), out var cached))
                return cached;

            double[] baseData = GetBaseData(dataSet);

            if (baseData == null || baseData.Length == 0)
                return Array.Empty<double>();

            double scale = 1.0 + idx * 0.03;
            double offset = (idx - 1) * 0.2;

            double[] y = new double[baseData.Length];
            for (int i = 0; i < baseData.Length; i++)
                y[i] = baseData[i] * scale + offset;

            lineCache[(dataSet, idx)] = y;
            return y;
        }

        /// <summary>
        /// 當資料集改變時，清掉 cache（不然會拿到舊資料的變形）
        /// </summary>
        private void ClearLineCache()
        {
            lineCache.Clear();
        }

        /// <summary>
        /// 統一重畫入口（UI事件只要呼叫這個）
        /// </summary>
        private void RedrawAllPlots()
        {
            RedrawPlot1();
            RedrawPlot2();
        }

        /// <summary>
        /// WpfPlot1：單選（radio）永遠只畫一條
        /// </summary>
        private void RedrawPlot1()
        {
            WpfPlot1.Plot.Clear();

            if (leftMode == PlotMode.Single)
            {
                int idx = currentRadioIndex;
                var sig = WpfPlot1.Plot.Add.Signal(GetLineData(leftDataSet, idx));
                sig.LineWidth = 3;

                if (indexColor.TryGetValue(idx, out var c))
                    sig.Color = new ScottPlot.Color(c.R, c.G, c.B, c.A);

                sig.AlwaysUseLowDensityMode = true;
            }
            else if (leftMode == PlotMode.Multi)
            {
                foreach (int idx in checkedIndexSet)
                {
                    var sig = WpfPlot1.Plot.Add.Signal(GetLineData(leftDataSet, idx));
                    sig.LineWidth = 3;

                    if (indexColor.TryGetValue(idx, out var c))
                        sig.Color = new ScottPlot.Color(c.R, c.G, c.B, c.A);

                    sig.AlwaysUseLowDensityMode = true;
                }
            }
            else
            {
                WpfPlot1.Plot.Add.Bars(plotData3);
            }

            WpfPlot1.Refresh();
        }

        /// <summary>
        /// WpfPlot2：複選（checkbox）畫多條
        /// </summary>
        private void RedrawPlot2()
        {
            WpfPlot2.Plot.Clear();

            if (rightMode == PlotMode.Single)
            {
                int idx = currentRadioIndex;
                var sig = WpfPlot2.Plot.Add.Signal(GetLineData(rightDataSet, idx));
                sig.LineWidth = 3;

                if (indexColor.TryGetValue(idx, out var c))
                    sig.Color = new ScottPlot.Color(c.R, c.G, c.B, c.A);

                sig.AlwaysUseLowDensityMode = true;
            }
            else if (rightMode == PlotMode.Multi)
            {
                foreach (int idx in checkedIndexSet)
                {
                    var sig = WpfPlot2.Plot.Add.Signal(GetLineData(rightDataSet, idx));
                    sig.LineWidth = 3;

                    if (indexColor.TryGetValue(idx, out var c))
                        sig.Color = new ScottPlot.Color(c.R, c.G, c.B, c.A);

                    sig.AlwaysUseLowDensityMode = true;
                }
            }
            else
            {
                WpfPlot2.Plot.Add.Bars(plotData3);
            }

            WpfPlot2.Refresh();
        }

        // -------------------- DataGrid 事件：只改狀態 -> 重畫 --------------------

        private void dg_Radio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isBuildingGrid) return;
            if (_isHandlingMainRadio) return;

            RadioButton rb = sender as RadioButton;
            if (rb == null) return;

            DataRowView rowView = rb.DataContext as DataRowView;
            if (rowView == null) return;

            _isHandlingMainRadio = true;

            try
            {
                foreach (DataRow row in dt_dataGrid.Rows)
                    row["Main"] = false;

                rowView["Main"] = true;

                currentRadioIndex = Convert.ToInt32(rowView["Index"]);
            }
            finally
            {
                _isHandlingMainRadio = false;
            }
        }

        private void dg_Check_Changed(object sender, RoutedEventArgs e)
        {
            if (_isBuildingGrid) return;
            if (_isHandlingSubCheck) return;

            CheckBox cb = sender as CheckBox;
            if (cb == null) return;

            DataRowView rowView = cb.DataContext as DataRowView;
            if (rowView == null) return;

            _isHandlingSubCheck = true;

            try
            {
                bool isChecked = cb.IsChecked == true;
                rowView["Sub"] = isChecked;

                int idx = Convert.ToInt32(rowView["Index"]);

                if (isChecked)
                    checkedIndexSet.Add(idx);
                else
                    checkedIndexSet.Remove(idx);
            }
            finally
            {
                _isHandlingSubCheck = false;
            }
        }
        private void dg_colorGhange(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            DataRowView rowView = btn.DataContext as DataRowView;
            if (rowView == null) return;

            Color old = Colors.Transparent;

            Brush b = rowView["ColorBrush"] as Brush;
            SolidColorBrush sbOld = b as SolidColorBrush;
            if (sbOld != null)
                old = sbOld.Color;

            colorPicker picker = new colorPicker(old);
            if (picker.ShowDialog() == true)
            {
                Color selectedColor = picker.SelectedColor;
                SolidColorBrush newBrush = new SolidColorBrush(selectedColor);

                rowView["ColorBrush"] = newBrush;

                int idx = Convert.ToInt32(rowView["Index"]);
                indexColor[idx] = selectedColor;

                dg.Items.Refresh();
                RedrawAllPlots();
            }
        }


        // -------------------- Tab 事件：左邊控 Plot1、右邊控 Plot2 --------------------
        //
        // XAML 裡 left / right 的 6 個按鈕都綁 Click="plotTabChange"
        //
        private void plotTabChange(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            // ===== 左邊三個按鈕 =====
            if (btn.Name == "btn_leftPlot1")
            {
                leftMode = PlotMode.Single;
            }
            else if (btn.Name == "btn_leftPlot2")
            {
                leftMode = PlotMode.Multi;
            }
            else if (btn.Name == "btn_leftPlot3")
            {
                leftMode = PlotMode.Other;
            }

            // ===== 右邊三個按鈕 =====
            if (btn.Name == "btn_RightPlot1")
            {
                rightMode = PlotMode.Single;
            }
            else if (btn.Name == "btn_RightPlot2")
            {
                rightMode = PlotMode.Multi;
            }
            else if (btn.Name == "btn_RightPlot3")
            {
                rightMode = PlotMode.Other;
            }

            if (btn.Name.Contains("left"))
            {
                btn_leftPlot1.Background = new SolidColorBrush(Colors.Transparent);
                btn_leftPlot2.Background = new SolidColorBrush(Colors.Transparent);
                btn_leftPlot3.Background = new SolidColorBrush(Colors.Transparent);
                RedrawPlot1();
                WpfPlot1.Plot.Axes.AutoScale();
            }
            if (btn.Name.Contains("Right"))
            {
                btn_RightPlot1.Background = new SolidColorBrush(Colors.Transparent);
                btn_RightPlot2.Background = new SolidColorBrush(Colors.Transparent);
                btn_RightPlot3.Background = new SolidColorBrush(Colors.Transparent);
                RedrawPlot2();
                WpfPlot2.Plot.Axes.AutoScale();
                WpfPlot2.Plot.Axes.AutoScale();
            }
            btn.Background = new SolidColorBrush(Colors.Gray);
           
        }
        

        #endregion


        public event PropertyChangedEventHandler? PropertyChanged;


        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            InitDefaultGridLayoutIfNeeded();

            DataGridSetting_Window2 win = new DataGridSetting_Window2();
            win.Owner = this;

            win.LoadFromConfig(_gridLayoutConfig, dt_dataGrid);

            bool? result = win.ShowDialog();
            if (result != true)
                return;

            _gridLayoutConfig = win.ExportConfig();

            ApplyGridLayoutToMainGrid();
        }

        private void ApplyGridLayoutToMainWindow()
        {
            if (dt_dataGrid == null)
                return;

            dg.ItemsSource = null;
            dg.ItemsSource = dt_dataGrid.DefaultView;

            // 之後你如果已經有 DataGridLayoutApplier，就在這裡接上
            // DataGridLayoutApplier.Apply(dg, dt_dataGrid, _gridLayoutConfig);
        }

        // ====== 你原本的事件：留著讓你接既有邏輯 ======

        private void SyncPlotStateFromGrid()
        {
            indexColor.Clear();
            checkedIndexSet.Clear();

            foreach (DataRow row in dt_dataGrid.Rows)
            {
                int idx = Convert.ToInt32(row["Index"]);

                SolidColorBrush sb = row["ColorBrush"] as SolidColorBrush;
                if (sb != null)
                    indexColor[idx] = sb.Color;

                if (row["Sub"] != DBNull.Value && Convert.ToBoolean(row["Sub"]))
                    checkedIndexSet.Add(idx);

                if (row["Main"] != DBNull.Value && Convert.ToBoolean(row["Main"]))
                    currentRadioIndex = idx;
            }

            if (currentRadioIndex == 0 && dt_dataGrid.Rows.Count > 0)
                currentRadioIndex = Convert.ToInt32(dt_dataGrid.Rows[0]["Index"]);

            if (checkedIndexSet.Count == 0 && dt_dataGrid.Rows.Count > 0)
                checkedIndexSet.Add(Convert.ToInt32(dt_dataGrid.Rows[0]["Index"]));
        }

        //public void ApplyGridLayoutToMainGrid()
        //{
        //    if (dt_dataGrid == null)
        //        return;

        //    if (_gridLayoutConfig == null)
        //        return;

        //    _isBuildingGrid = true;

        //    try
        //    {
        //        DataTable displayTable = BuildDisplayTableForMainGrid(dt_dataGrid, _gridLayoutConfig);

        //        dg.ItemsSource = null;
        //        dg.Columns.Clear();

        //        for (int i = 0; i < _gridLayoutConfig.SelectedColumns.Count; i++)
        //        {
        //            ColumnConfig cfg = _gridLayoutConfig.SelectedColumns[i];
        //            DataGridColumn col = CreateMainGridColumnFromConfig(cfg);

        //            if (col == null)
        //                continue;

        //            dg.Columns.Add(col);
        //        }

        //        dg.ItemsSource = displayTable.DefaultView;

        //        ApplyMainGridGrouping(displayTable);
        //        dg.Items.Refresh();
        //    }
        //    finally
        //    {
        //        _isBuildingGrid = false;
        //    }
        //}
        public void ApplyGridLayoutToMainGrid()
        {
            if (dt_dataGrid == null)
                return;

            InitDefaultGridLayoutIfNeeded();

            _isBuildingGrid = true;

            try
            {
                _displayGridTable = BuildDisplayTableForMainGrid(dt_dataGrid, _gridLayoutConfig);

                dg.ItemsSource = null;
                dg.Columns.Clear();

                foreach (ColumnConfig cfg in _gridLayoutConfig.SelectedColumns)
                {
                    DataGridColumn col = CreateMainGridColumnFromConfig(cfg);
                    if (col == null)
                        continue;

                    dg.Columns.Add(col);
                }

                dg.ItemsSource = _displayGridTable.DefaultView;
                ApplyMainGridGrouping();
                dg.Items.Refresh();
            }
            finally
            {
                _isBuildingGrid = false;
            }
        }
        private DataTable BuildDisplayTableForMainGrid(DataTable source, DataGridLayoutConfig layout)
        {
            DataTable dt = source.Copy();

            if (!dt.Columns.Contains("__GroupDisplay"))
                dt.Columns.Add("__GroupDisplay", typeof(string));

            if (layout == null || layout.Grouping == null || !layout.Grouping.Enabled)
            {
                foreach (DataRow row in dt.Rows)
                    row["__GroupDisplay"] = "";
                return dt;
            }

            if (layout.Grouping.Mode == GroupMode.BySameValue)
                FillGroupDisplay_BySameValue(dt, layout);
            else if (layout.Grouping.Mode == GroupMode.ByFixedCount)
                FillGroupDisplay_ByFixedCount(dt, layout);
            else if (layout.Grouping.Mode == GroupMode.ByRange)
                FillGroupDisplay_ByRange(dt, layout);
            else
            {
                foreach (DataRow row in dt.Rows)
                    row["__GroupDisplay"] = "";
            }

            return dt;
        }

        private void ApplyMainGridGrouping()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dg.ItemsSource);
            if (view == null)
                return;

            using (view.DeferRefresh())
            {
                view.GroupDescriptions.Clear();
                view.SortDescriptions.Clear();

                if (_gridLayoutConfig != null &&
                    _gridLayoutConfig.Grouping != null &&
                    _gridLayoutConfig.Grouping.Enabled)
                {
                    view.SortDescriptions.Add(
                        new SortDescription("__GroupDisplay", ListSortDirection.Ascending));

                    view.GroupDescriptions.Add(
                        new PropertyGroupDescription("__GroupDisplay"));
                }
            }
        }

        private void FillGroupDisplay_BySameValue(DataTable dt, DataGridLayoutConfig layout)
        {
            string field = layout.Grouping.GroupFieldId;
            if (string.IsNullOrWhiteSpace(field) || !dt.Columns.Contains(field))
                return;

            var groups = dt.AsEnumerable()
                .GroupBy(r => Convert.ToString(r[field]))
                .ToList();

            foreach (var g in groups)
            {
                string groupName = string.IsNullOrWhiteSpace(g.Key) ? "Others" : g.Key;
                string headerText = BuildMainGridGroupHeaderText(groupName, g.ToList(), layout);

                foreach (DataRow row in g)
                    row["__GroupDisplay"] = headerText;
            }
        }

        private void FillGroupDisplay_ByFixedCount(DataTable dt, DataGridLayoutConfig layout)
        {
            int size = layout.Grouping.FixedCount;
            if (size <= 0) size = 10;

            int total = dt.Rows.Count;
            int groupNo = 1;

            for (int start = 0; start < total; start += size)
            {
                int end = Math.Min(start + size - 1, total - 1);

                List<DataRow> rows = new List<DataRow>();
                for (int i = start; i <= end; i++)
                    rows.Add(dt.Rows[i]);

                string groupName = "Group " + groupNo + " (" + (start + 1) + "~" + (end + 1) + ")";
                string headerText = BuildMainGridGroupHeaderText(groupName, rows, layout);

                foreach (DataRow row in rows)
                    row["__GroupDisplay"] = headerText;

                groupNo++;
            }
        }

        private void FillGroupDisplay_ByRange(DataTable dt, DataGridLayoutConfig layout)
        {
            string field = layout.Grouping.GroupFieldId;
            if (string.IsNullOrWhiteSpace(field) || !dt.Columns.Contains(field))
                return;

            foreach (DataRow row in dt.Rows)
                row["__GroupDisplay"] = "Others";

            foreach (GroupRangeDef rg in layout.Grouping.Ranges)
            {
                List<DataRow> rows = new List<DataRow>();

                foreach (DataRow row in dt.Rows)
                {
                    double v;
                    if (!double.TryParse(row[field].ToString(), out v))
                        continue;

                    if (v >= rg.Min && v <= rg.Max)
                        rows.Add(row);
                }

                if (rows.Count == 0)
                    continue;

                string headerText = BuildMainGridGroupHeaderText(rg.Name, rows, layout);

                foreach (DataRow row in rows)
                    row["__GroupDisplay"] = headerText;
            }
        }

        private string BuildMainGridGroupHeaderText(string groupName, List<DataRow> rows, DataGridLayoutConfig layout)
        {
            string text = groupName;

            if (layout.SelectedMetrics == null || layout.SelectedMetrics.Count == 0)
            {
                text += "   |   Count = " + rows.Count;
                return text;
            }

            foreach (GroupMetricDef metric in layout.SelectedMetrics)
            {
                if (metric == null) continue;

                if (metric.Id == "Count")
                {
                    text += "   |   Count = " + rows.Count;
                }
                else if (metric.Id == "index_range")
                {
                    int minIdx = GetMinIndexFromRows(rows);
                    int maxIdx = GetMaxIndexFromRows(rows);
                    text += "   |   Range = " + minIdx + "~" + maxIdx;
                }
                else if (metric.Id == "Avg")
                {
                    double avg;
                    if (TryGetAverageFromRows(rows, metric.TargetFieldId, out avg))
                        text += "   |   Avg(" + metric.TargetFieldId + ") = " + avg.ToString("0.###");
                }
                else if (metric.Id == "Max")
                {
                    double max;
                    if (TryGetMaxFromRows(rows, metric.TargetFieldId, out max))
                        text += "   |   Max(" + metric.TargetFieldId + ") = " + max.ToString("0.###");
                }
            }

            return text;
        }

        private bool TryGetAverageFromRows(List<DataRow> rows, string fieldId, out double avg)
        {
            avg = 0;
            if (string.IsNullOrWhiteSpace(fieldId)) return false;

            double sum = 0;
            int count = 0;

            foreach (DataRow row in rows)
            {
                if (!row.Table.Columns.Contains(fieldId)) continue;

                double v;
                if (double.TryParse(row[fieldId].ToString(), out v))
                {
                    sum += v;
                    count++;
                }
            }

            if (count == 0) return false;

            avg = sum / count;
            return true;
        }

        private bool TryGetMaxFromRows(List<DataRow> rows, string fieldId, out double max)
        {
            max = 0;
            if (string.IsNullOrWhiteSpace(fieldId)) return false;

            bool hasValue = false;

            foreach (DataRow row in rows)
            {
                if (!row.Table.Columns.Contains(fieldId)) continue;

                double v;
                if (double.TryParse(row[fieldId].ToString(), out v))
                {
                    if (!hasValue)
                    {
                        max = v;
                        hasValue = true;
                    }
                    else if (v > max)
                    {
                        max = v;
                    }
                }
            }

            return hasValue;
        }

        private int GetMinIndexFromRows(List<DataRow> rows)
        {
            int min = int.MaxValue;

            foreach (DataRow row in rows)
            {
                if (!row.Table.Columns.Contains("Index")) continue;

                int v;
                if (int.TryParse(row["Index"].ToString(), out v))
                {
                    if (v < min)
                        min = v;
                }
            }

            return min == int.MaxValue ? 0 : min;
        }

        private int GetMaxIndexFromRows(List<DataRow> rows)
        {
            int max = int.MinValue;

            foreach (DataRow row in rows)
            {
                if (!row.Table.Columns.Contains("Index")) continue;

                int v;
                if (int.TryParse(row["Index"].ToString(), out v))
                {
                    if (v > max)
                        max = v;
                }
            }

            return max == int.MinValue ? 0 : max;
        }

        private DataTable BuildDisplayTableFromLayout(DataTable source, DataGridLayoutConfig layout)
        {
            if (layout.Grouping == null || !layout.Grouping.Enabled)
                return BuildFlatDisplayTable(source);

            switch (layout.Grouping.Mode)
            {
                case GroupMode.BySameValue:
                    return BuildGrouped_BySameValue(source, layout);

                case GroupMode.ByFixedCount:
                    return BuildGrouped_ByFixedCount(source, layout);

                case GroupMode.ByRange:
                    return BuildGrouped_ByRange(source, layout);
            }

            return BuildFlatDisplayTable(source);
        }

        private DataTable BuildGrouped_ByFixedCount(DataTable source, DataGridLayoutConfig layout)
        {
            DataTable dt = CreateDisplaySchema(source);

            int size = layout.Grouping.FixedCount;
            int total = source.Rows.Count;

            int groupNo = 1;

            for (int i = 0; i < total; i += size)
            {
                int end = Math.Min(i + size, total);

                List<DataRow> rows = new List<DataRow>();

                for (int j = i; j < end; j++)
                    rows.Add(source.Rows[j]);

                DataRow header = dt.NewRow();
                header["__RowType"] = "Header";
                header["__GroupText"] = BuildGroupHeader("Group " + groupNo, rows, layout);
                dt.Rows.Add(header);

                foreach (DataRow r in rows)
                {
                    DataRow data = dt.NewRow();
                    data["__RowType"] = "Data";

                    foreach (DataColumn col in source.Columns)
                        data[col.ColumnName] = r[col];

                    dt.Rows.Add(data);
                }

                groupNo++;
            }

            return dt;
        }

        private DataTable BuildGrouped_ByRange(DataTable source, DataGridLayoutConfig layout)
        {
            DataTable dt = CreateDisplaySchema(source);

            string field = layout.Grouping.GroupFieldId;

            foreach (GroupRangeDef rg in layout.Grouping.Ranges)
            {
                List<DataRow> rows = new List<DataRow>();

                foreach (DataRow row in source.Rows)
                {
                    double v;

                    if (!double.TryParse(row[field].ToString(), out v))
                        continue;

                    if (v >= rg.Min && v <= rg.Max)
                        rows.Add(row);
                }

                if (rows.Count == 0)
                    continue;

                DataRow header = dt.NewRow();
                header["__RowType"] = "Header";
                header["__GroupText"] = BuildGroupHeader(rg.Name, rows, layout);
                dt.Rows.Add(header);

                foreach (DataRow r in rows)
                {
                    DataRow data = dt.NewRow();
                    data["__RowType"] = "Data";

                    foreach (DataColumn col in source.Columns)
                        data[col.ColumnName] = r[col];

                    dt.Rows.Add(data);
                }
            }

            return dt;
        }

        private string BuildGroupHeader(string groupName, List<DataRow> rows, DataGridLayoutConfig layout)
        {
            string text = groupName;

            foreach (GroupMetricDef metric in layout.SelectedMetrics)
            {
                if (metric.Id == "Count")
                    text += " | Count=" + rows.Count;

                if (metric.Id == "Avg")
                {
                    double sum = 0;
                    int c = 0;

                    foreach (DataRow r in rows)
                    {
                        double v;
                        if (double.TryParse(r[metric.TargetFieldId].ToString(), out v))
                        {
                            sum += v;
                            c++;
                        }
                    }

                    if (c > 0)
                        text += $" | Avg({metric.TargetFieldId})={sum / c:0.###}";
                }

                if (metric.Id == "Max")
                {
                    double max = double.MinValue;

                    foreach (DataRow r in rows)
                    {
                        double v;
                        if (double.TryParse(r[metric.TargetFieldId].ToString(), out v))
                            if (v > max) max = v;
                    }

                    text += $" | Max({metric.TargetFieldId})={max:0.###}";
                }
            }

            return text;
        }



        private DataTable BuildGrouped_BySameValue(DataTable source, DataGridLayoutConfig layout)
        {
            DataTable dt = CreateDisplaySchema(source);

            string field = layout.Grouping.GroupFieldId;

            var groups = source.AsEnumerable()
                .GroupBy(r => r[field]?.ToString())
                .ToList();

            foreach (var g in groups)
            {
                DataRow header = dt.NewRow();
                header["__RowType"] = "Header";
                header["__GroupText"] = BuildGroupHeader(g.Key, g.ToList(), layout);
                dt.Rows.Add(header);

                foreach (DataRow row in g)
                {
                    DataRow data = dt.NewRow();
                    data["__RowType"] = "Data";

                    foreach (DataColumn col in source.Columns)
                        data[col.ColumnName] = row[col];

                    dt.Rows.Add(data);
                }
            }

            return dt;
        }
        private DataTable CreateDisplaySchema(DataTable source)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("__RowType", typeof(string));
            dt.Columns.Add("__GroupText", typeof(string));

            foreach (DataColumn col in source.Columns)
                dt.Columns.Add(col.ColumnName, col.DataType);

            return dt;
        }
      

        private DataTable BuildDisplayTableByConfig(DataTable source, DataGridLayoutConfig config)
        {
            if (source == null)
                return null;

            if (config == null || config.Grouping == null || !config.Grouping.Enabled)
                return BuildFlatDisplayTable(source);

            if (config.Grouping.Mode == GroupMode.BySameValue)
                return BuildGroupedDisplayTable_BySameValue(source, config);

            if (config.Grouping.Mode == GroupMode.ByFixedCount)
                return BuildGroupedDisplayTable_ByFixedCount(source, config);

            if (config.Grouping.Mode == GroupMode.ByRange)
                return BuildGroupedDisplayTable_ByRange(source, config);

            return BuildFlatDisplayTable(source);
        }

        private DataTable BuildFlatDisplayTable(DataTable source)
        {
            DataTable dt = CreateDisplaySchema(source);

            foreach (DataRow row in source.Rows)
            {
                DataRow newRow = dt.NewRow();

                newRow["__RowType"] = "Data";
                newRow["__GroupText"] = "";

                foreach (DataColumn col in source.Columns)
                    newRow[col.ColumnName] = row[col];

                dt.Rows.Add(newRow);
            }

            return dt;
        }

        private DataTable BuildGroupedDisplayTable_BySameValue(DataTable source, DataGridLayoutConfig config)
        {
            DataTable dt = CreateDisplayTableSchema(source);

            string fieldId = config.Grouping.GroupFieldId;
            if (string.IsNullOrWhiteSpace(fieldId) || !source.Columns.Contains(fieldId))
                return BuildFlatDisplayTable(source);

            var groups = source.AsEnumerable()
                .GroupBy(r => Convert.ToString(r[fieldId]))
                .ToList();

            foreach (var g in groups)
            {
                DataRow headerRow = dt.NewRow();
                headerRow["__RowType"] = "Header";
                headerRow["__GroupText"] = BuildGroupHeaderText(g.Key, g.ToList(), config);
                dt.Rows.Add(headerRow);

                foreach (DataRow srcRow in g)
                {
                    DataRow dataRow = dt.NewRow();
                    dataRow["__RowType"] = "Data";
                    dataRow["__GroupText"] = "";

                    CopySourceRowToDisplayRow(srcRow, dataRow, source);
                    dt.Rows.Add(dataRow);
                }
            }

            return dt;
        }

        private DataTable BuildGroupedDisplayTable_ByFixedCount(DataTable source, DataGridLayoutConfig config)
        {
            DataTable dt = CreateDisplayTableSchema(source);

            int fixedCount = config.Grouping.FixedCount;
            if (fixedCount <= 0)
                fixedCount = 10;

            int total = source.Rows.Count;
            int groupNo = 1;

            for (int start = 0; start < total; start += fixedCount)
            {
                int end = Math.Min(start + fixedCount - 1, total - 1);

                List<DataRow> rows = new List<DataRow>();
                for (int i = start; i <= end; i++)
                    rows.Add(source.Rows[i]);

                string groupName = "Group " + groupNo + " (" + (start + 1) + "~" + (end + 1) + ")";

                DataRow headerRow = dt.NewRow();
                headerRow["__RowType"] = "Header";
                headerRow["__GroupText"] = BuildGroupHeaderText(groupName, rows, config);
                dt.Rows.Add(headerRow);

                foreach (DataRow srcRow in rows)
                {
                    DataRow dataRow = dt.NewRow();
                    dataRow["__RowType"] = "Data";
                    dataRow["__GroupText"] = "";

                    CopySourceRowToDisplayRow(srcRow, dataRow, source);
                    dt.Rows.Add(dataRow);
                }

                groupNo++;
            }

            return dt;
        }

        private DataTable BuildGroupedDisplayTable_ByRange(DataTable source, DataGridLayoutConfig config)
        {
            DataTable dt = CreateDisplayTableSchema(source);

            string fieldId = config.Grouping.GroupFieldId;
            if (string.IsNullOrWhiteSpace(fieldId) || !source.Columns.Contains(fieldId))
                return BuildFlatDisplayTable(source);

            if (config.Grouping.Ranges == null || config.Grouping.Ranges.Count == 0)
                return BuildFlatDisplayTable(source);

            foreach (GroupRangeDef rg in config.Grouping.Ranges)
            {
                List<DataRow> rows = new List<DataRow>();

                foreach (DataRow row in source.Rows)
                {
                    double v;
                    if (!TryGetRowDouble(row, fieldId, out v))
                        continue;

                    if (v >= rg.Min && v <= rg.Max)
                        rows.Add(row);
                }

                if (rows.Count == 0)
                    continue;

                DataRow headerRow = dt.NewRow();
                headerRow["__RowType"] = "Header";
                headerRow["__GroupText"] = BuildGroupHeaderText(rg.Name, rows, config);
                dt.Rows.Add(headerRow);

                foreach (DataRow srcRow in rows)
                {
                    DataRow dataRow = dt.NewRow();
                    dataRow["__RowType"] = "Data";
                    dataRow["__GroupText"] = "";

                    CopySourceRowToDisplayRow(srcRow, dataRow, source);
                    dt.Rows.Add(dataRow);
                }
            }

            return dt;
        }

        private DataTable CreateDisplayTableSchema(DataTable source)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("__RowType", typeof(string));
            dt.Columns.Add("__GroupText", typeof(string));

            foreach (DataColumn col in source.Columns)
            {
                dt.Columns.Add(col.ColumnName, col.DataType);
            }

            return dt;
        }

        private void CopySourceRowToDisplayRow(DataRow srcRow, DataRow destRow, DataTable source)
        {
            foreach (DataColumn col in source.Columns)
            {
                destRow[col.ColumnName] = srcRow[col.ColumnName];
            }
        }

        private string BuildGroupHeaderText(string groupName, List<DataRow> rows, DataGridLayoutConfig config)
        {
            string text = groupName;

            if (config.SelectedMetrics == null || config.SelectedMetrics.Count == 0)
            {
                text += "   |   Count = " + rows.Count;
                return text;
            }

            foreach (GroupMetricDef metric in config.SelectedMetrics)
            {
                string metricText = BuildMetricText(metric, rows);
                if (!string.IsNullOrWhiteSpace(metricText))
                    text += "   |   " + metricText;
            }

            return text;
        }

        private string BuildMetricText(GroupMetricDef metric, List<DataRow> rows)
        {
            if (metric == null)
                return "";

            if (metric.Id == "Count")
                return "Count = " + rows.Count;

            if (metric.Id == "index_range")
            {
                int minIdx = GetMinIndex(rows);
                int maxIdx = GetMaxIndex(rows);
                return "Range = " + minIdx + "~" + maxIdx;
            }

            string fieldId = metric.TargetFieldId;
            if (string.IsNullOrWhiteSpace(fieldId))
                return metric.Name;

            if (metric.Id == "Avg")
            {
                double avg;
                if (!TryGetAverage(rows, fieldId, out avg))
                    return "Avg(" + fieldId + ") = N/A";

                return "Avg(" + fieldId + ") = " + avg.ToString("0.###");
            }

            if (metric.Id == "Max")
            {
                double max;
                if (!TryGetMax(rows, fieldId, out max))
                    return "Max(" + fieldId + ") = N/A";

                return "Max(" + fieldId + ") = " + max.ToString("0.###");
            }

            return metric.Name;
        }

        private bool TryGetAverage(List<DataRow> rows, string fieldId, out double avg)
        {
            avg = 0;
            double sum = 0;
            int count = 0;

            foreach (DataRow row in rows)
            {
                double v;
                if (TryGetRowDouble(row, fieldId, out v))
                {
                    sum += v;
                    count++;
                }
            }

            if (count == 0)
                return false;

            avg = sum / count;
            return true;
        }

        private bool TryGetMax(List<DataRow> rows, string fieldId, out double max)
        {
            max = 0;
            bool hasValue = false;

            foreach (DataRow row in rows)
            {
                double v;
                if (TryGetRowDouble(row, fieldId, out v))
                {
                    if (!hasValue)
                    {
                        max = v;
                        hasValue = true;
                    }
                    else if (v > max)
                    {
                        max = v;
                    }
                }
            }

            return hasValue;
        }

        private bool TryGetRowDouble(DataRow row, string fieldId, out double value)
        {
            value = 0;

            if (row == null)
                return false;

            if (!row.Table.Columns.Contains(fieldId))
                return false;

            if (row[fieldId] == DBNull.Value)
                return false;

            return double.TryParse(row[fieldId].ToString(), out value);
        }

        private int GetMinIndex(List<DataRow> rows)
        {
            int minValue = int.MaxValue;

            foreach (DataRow row in rows)
            {
                if (row.Table.Columns.Contains("Index"))
                {
                    int v;
                    if (int.TryParse(row["Index"].ToString(), out v))
                    {
                        if (v < minValue)
                            minValue = v;
                    }
                }
            }

            return minValue == int.MaxValue ? 0 : minValue;
        }

        private int GetMaxIndex(List<DataRow> rows)
        {
            int maxValue = int.MinValue;

            foreach (DataRow row in rows)
            {
                if (row.Table.Columns.Contains("Index"))
                {
                    int v;
                    if (int.TryParse(row["Index"].ToString(), out v))
                    {
                        if (v > maxValue)
                            maxValue = v;
                    }
                }
            }

            return maxValue == int.MinValue ? 0 : maxValue;
        }

        private DataGridTextColumn CreateGroupHeaderTextColumn()
        {
            DataGridTextColumn col = new DataGridTextColumn();

            col.Header = "Group";
            col.Width = new DataGridLength(250);
            col.Binding = new Binding("[__GroupText]");

            return col;
        }

        private void dg_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataRowView rv = e.Row.Item as DataRowView;
            if (rv == null)
                return;

            e.Row.Background = Brushes.Transparent;
            e.Row.Foreground = Brushes.White;
            e.Row.Height = 40;
        }

        private DataGridColumn CreateMainGridColumnFromConfig(ColumnConfig cfg)
        {
            if (cfg == null)
                return null;

            string fieldId = cfg.FieldId ?? "";
            string header = string.IsNullOrWhiteSpace(cfg.Header) ? fieldId : cfg.Header;

            if (fieldId == "Main")
                return CreateMainRadioColumn(header);

            if (fieldId == "Sub")
                return CreateSubCheckColumn(header);

            if (fieldId == "ColorBrush")
                return CreateColorButtonColumn(header);

            if (fieldId == "Index")
                return CreateTextColumn(header, "Index", 80);

            if (fieldId == "Name")
                return CreateTextColumn(header, "Name", 200);

            if (fieldId == "Force")
                return CreateTextColumn(header, "Force", 120);

            if (fieldId == "Disp")
                return CreateTextColumn(header, "Disp", 120);

            if (dt_dataGrid.Columns.Contains(fieldId))
                return CreateTextColumn(header, fieldId, 120);

            return null;
        }
    }

}
