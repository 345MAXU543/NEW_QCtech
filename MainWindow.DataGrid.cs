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
using NEW_QCtech.dataGrid;

using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace NEW_QCtech
{
    /// <summary>
    /// MainWindow：Main DataGrid 建構、套版、群組與欄位建立。
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
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
            GridColumnDefinition reg;
            string fieldId;
            string header;
            string format;

            if (cfg == null) return null;

            fieldId = cfg.FieldId ?? "";
            header = GetColumnHeaderFromRegistryOrConfig(cfg);
            format = GetColumnFormatFromRegistryOrConfig(cfg);
            reg = ColumnRegistry.Get(fieldId);

            if (string.Equals(fieldId, "Main", StringComparison.OrdinalIgnoreCase))
                return CreateMainRadioColumn(header);

            if (string.Equals(fieldId, "Sub", StringComparison.OrdinalIgnoreCase))
                return CreateSubCheckColumn(header);

            if (string.Equals(fieldId, "ColorBrush", StringComparison.OrdinalIgnoreCase))
                return CreateColorButtonColumn(header);

            if (string.Equals(fieldId, "Index", StringComparison.OrdinalIgnoreCase))
                return CreateTextColumn(header, "Index", reg != null ? reg.DefaultWidth : 80, format);

            if (string.Equals(fieldId, "Name", StringComparison.OrdinalIgnoreCase))
                return CreateTextColumn(header, "Name", new DataGridLength(1, DataGridLengthUnitType.Star), format);

            if (string.Equals(fieldId, "Force", StringComparison.OrdinalIgnoreCase))
                return CreateTextColumn(header, "Force", reg != null ? reg.DefaultWidth : 120, format);

            if (string.Equals(fieldId, "Disp", StringComparison.OrdinalIgnoreCase))
                return CreateTextColumn(header, "Disp", reg != null ? reg.DefaultWidth : 120, format);

            // 其他文字欄位預設
            if (dt_dataGrid != null && dt_dataGrid.Columns.Contains(fieldId))
                return CreateTextColumn(header, fieldId, reg != null ? reg.DefaultWidth : 120, format);

            return null;
        }

        private DataGridTemplateColumn CreateMainRadioColumn(string header)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();
            col.Header = header;
            col.Width = GetColumnWidthFromRegistryOrDefault("Main", 60);
            col.CellTemplate = BuildMainRadioTemplate();
            return col;
        }

        private DataGridTemplateColumn CreateSubCheckColumn(string header)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();
            col.Header = header;
            col.Width = GetColumnWidthFromRegistryOrDefault("Sub", 60);
            col.CellTemplate = BuildSubCheckTemplate();
            return col;
        }

        private DataGridTemplateColumn CreateColorButtonColumn(string header)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();
            col.Header = header;
            col.Width = GetColumnWidthFromRegistryOrDefault("ColorBrush", 60);
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

        private string GetColumnHeaderFromRegistryOrConfig(ColumnConfig cfg)
        {
            GridColumnDefinition reg;

            if (cfg == null)
                return "";

            if (!string.IsNullOrWhiteSpace(cfg.Header))
                return cfg.Header;

            reg = ColumnRegistry.Get(cfg.FieldId);
            if (reg != null && !string.IsNullOrWhiteSpace(reg.Header))
                return reg.Header;

            return cfg.FieldId ?? "";
        }

        private string GetColumnFormatFromRegistryOrConfig(ColumnConfig cfg)
        {
            GridColumnDefinition reg;

            if (cfg == null)
                return "";

            if (!string.IsNullOrWhiteSpace(cfg.Format))
                return cfg.Format;

            reg = ColumnRegistry.Get(cfg.FieldId);
            if (reg != null && !string.IsNullOrWhiteSpace(reg.Format))
                return reg.Format;

            return "";
        }

        private double GetColumnWidthFromRegistryOrDefault(string fieldId, double fallback)
        {
            GridColumnDefinition reg;

            reg = ColumnRegistry.Get(fieldId);
            if (reg != null && reg.DefaultWidth > 0)
                return reg.DefaultWidth;

            return fallback;
        }
    }
}
