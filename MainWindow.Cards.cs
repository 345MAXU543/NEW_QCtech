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
using System.Linq;
using System.Windows.Controls.Primitives;

using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace NEW_QCtech
{
    /// <summary>
    /// MainWindow：卡片拖曳、畫面載入初始化、設定視窗入口。
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
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
    }
}
    #endregion