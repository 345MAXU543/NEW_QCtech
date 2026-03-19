using NEW_QCtech;
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
using System.Linq;
using System.Windows.Controls.Primitives;

using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace NEW_QCtech
{
    /// <summary>
    /// MainWindow：Plot 狀態、Plot 重畫、與 DataGrid 對 Plot 的互動。
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
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


        #region scottPlot相關
        private enum PlotMode { Single = 1, Multi = 2, Other = 3 }

        private PlotMode leftMode = PlotMode.Single;   // WpfPlot1 的模式
        private PlotMode rightMode = PlotMode.Multi;   // WpfPlot2 的模式（預設多線）
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
            RedrawAllPlots();
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
            RedrawAllPlots();
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
    }
}
