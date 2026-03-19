using NEW_QCtech.dataGrid.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using NEW_QCtech.dataGrid;

namespace NEW_QCtech.dataGrid
{
    class DataGridLayoutApplier
    {
        public static void ApplyToDataGrid(DataGrid dg, DataTable sourceTable, DataGridLayoutConfig cfg)
        {
            DataTable displayTable;
            ICollectionView view;

            if (dg == null) return;
            if (sourceTable == null) return;
            if (cfg == null) return;

            dg.ItemsSource = null;
            dg.Columns.Clear();
            dg.AutoGenerateColumns = true;
            dg.AutoGeneratingColumn -= Dg_AutoGeneratingColumn;
            dg.AutoGeneratingColumn += Dg_AutoGeneratingColumn;

            displayTable = BuildDisplayTable(sourceTable, cfg);

            dg.Tag = cfg;
            dg.ItemsSource = displayTable.DefaultView;

            ApplyHeadersToGrid(dg, cfg);

            view = CollectionViewSource.GetDefaultView(dg.ItemsSource);
            if (view == null) return;

            using (view.DeferRefresh())
            {
                view.GroupDescriptions.Clear();
                view.SortDescriptions.Clear();

                if (ShouldUseGrouping(cfg))
                {
                    view.SortDescriptions.Add(new SortDescription("__GroupDisplay", ListSortDirection.Ascending));
                    view.GroupDescriptions.Add(new PropertyGroupDescription("__GroupDisplay"));
                }
            }
        }

        private static void Dg_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "__GroupKey")
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "__GroupDisplay")
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "__RowNo")
            {
                e.Cancel = true;
                return;
            }
        }

        private static DataTable BuildDisplayTable(DataTable sourceTable, DataGridLayoutConfig cfg)
        {
            DataTable dt = new DataTable();
            int i;
            int r;
            string fieldId;
            DataRow srcRow;
            DataRow newRow;
            bool useGrouping;

            useGrouping = ShouldUseGrouping(cfg);

            if (useGrouping)
            {
                dt.Columns.Add("__GroupKey", typeof(string));
                dt.Columns.Add("__GroupDisplay", typeof(string));
                dt.Columns.Add("__RowNo", typeof(int));
            }

            for (i = 0; i < cfg.SelectedColumns.Count; i++)
            {
                fieldId = cfg.SelectedColumns[i].FieldId;

                if (!sourceTable.Columns.Contains(fieldId))
                    continue;

                dt.Columns.Add(fieldId, sourceTable.Columns[fieldId].DataType);
            }

            for (r = 0; r < sourceTable.Rows.Count; r++)
            {
                srcRow = sourceTable.Rows[r];
                newRow = dt.NewRow();

                if (useGrouping)
                {
                    newRow["__GroupKey"] = GetGroupKey(sourceTable, srcRow, r, cfg);
                    newRow["__RowNo"] = r + 1;
                    newRow["__GroupDisplay"] = "";
                }

                for (i = 0; i < cfg.SelectedColumns.Count; i++)
                {
                    fieldId = cfg.SelectedColumns[i].FieldId;

                    if (!sourceTable.Columns.Contains(fieldId))
                        continue;

                    newRow[fieldId] = srcRow[fieldId];
                }

                dt.Rows.Add(newRow);
            }

            if (useGrouping)
            {
                FillGroupDisplayTexts(dt, cfg);
            }

            return dt;
        }

        private static void FillGroupDisplayTexts(DataTable dt, DataGridLayoutConfig cfg)
        {
            Dictionary<string, List<DataRow>> groups = new Dictionary<string, List<DataRow>>();
            int i;
            string key;
            string display;

            for (i = 0; i < dt.Rows.Count; i++)
            {
                key = dt.Rows[i]["__GroupKey"].ToString();

                if (!groups.ContainsKey(key))
                    groups[key] = new List<DataRow>();

                groups[key].Add(dt.Rows[i]);
            }

            foreach (KeyValuePair<string, List<DataRow>> kv in groups)
            {
                display = BuildGroupDisplayText(kv.Key, kv.Value, cfg);

                for (i = 0; i < kv.Value.Count; i++)
                {
                    kv.Value[i]["__GroupDisplay"] = display;
                }
            }
        }

        private static string BuildGroupDisplayText(string groupKey, List<DataRow> rows, DataGridLayoutConfig cfg)
        {
            string text = groupKey;
            int i;
            string metricText;

            if (cfg.SelectedMetrics.Count == 0)
            {
                text += "   |   Count = " + rows.Count.ToString();
                return text;
            }

            for (i = 0; i < cfg.SelectedMetrics.Count; i++)
            {
                metricText = BuildMetricText(cfg.SelectedMetrics[i], rows, cfg);

                if (metricText != "")
                    text += "   |   " + metricText;
            }

            return text;
        }

        private static string BuildMetricText(GroupMetricDef metric, List<DataRow> rows, DataGridLayoutConfig cfg)
        {
            string fieldId;
            string fieldHeader;
            double avgValue;
            double maxValue;
            int minNo;
            int maxNo;

            if (metric == null) return "";

            if (metric.Id == "Count")
            {
                return "Count = " + rows.Count.ToString();
            }

            if (metric.Id == "index_range")
            {
                minNo = GetMinRowNo(rows);
                maxNo = GetMaxRowNo(rows);
                return "Range = " + minNo.ToString() + "~" + maxNo.ToString();
            }

            fieldId = metric.TargetFieldId;
            if (fieldId == "")
                fieldId = GetDefaultMetricTargetFieldId(cfg);

            fieldHeader = GetColumnHeader(cfg, fieldId);

            if (metric.Id == "Avg")
            {
                if (!TryGetAverage(rows, fieldId, out avgValue))
                    return "Avg(" + fieldHeader + ") = N/A";

                return "Avg(" + fieldHeader + ") = " + avgValue.ToString("0.###");
            }

            if (metric.Id == "Max")
            {
                if (!TryGetMax(rows, fieldId, out maxValue))
                    return "Max(" + fieldHeader + ") = N/A";

                return "Max(" + fieldHeader + ") = " + maxValue.ToString("0.###");
            }

            return metric.Name;
        }

        private static string GetDefaultMetricTargetFieldId(DataGridLayoutConfig cfg)
        {
            int i;
            string fieldId;

            if (cfg == null || cfg.SelectedColumns == null)
                return "";

            for (i = 0; i < cfg.SelectedColumns.Count; i++)
            {
                fieldId = cfg.SelectedColumns[i].FieldId;

                if (IsNumericFieldId(fieldId))
                    return fieldId;
            }

            return "";
        }

        private static bool IsNumericFieldId(string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
                return false;

            if (ColumnRegistry.IsNumericField(fieldId))
                return true;

            return false;
        }

        private static string GetColumnHeader(DataGridLayoutConfig cfg, string fieldId)
        {
            int i;
            GridColumnDefinition reg;

            if (string.IsNullOrWhiteSpace(fieldId))
                return "";

            if (cfg != null && cfg.SelectedColumns != null)
            {
                for (i = 0; i < cfg.SelectedColumns.Count; i++)
                {
                    if (string.Equals(cfg.SelectedColumns[i].FieldId, fieldId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(cfg.SelectedColumns[i].Header))
                            return cfg.SelectedColumns[i].Header;

                        break;
                    }
                }
            }

            reg = ColumnRegistry.Get(fieldId);
            if (reg != null && !string.IsNullOrWhiteSpace(reg.Header))
                return reg.Header;

            return fieldId;
        }

        private static bool TryGetAverage(List<DataRow> rows, string fieldId, out double avgValue)
        {
            int i;
            int count;
            double sum;
            double v;

            avgValue = 0;
            sum = 0;
            count = 0;

            if (fieldId == "")
                return false;

            for (i = 0; i < rows.Count; i++)
            {
                if (TryGetRowDouble(rows[i], fieldId, out v))
                {
                    sum += v;
                    count++;
                }
            }

            if (count <= 0)
                return false;

            avgValue = sum / count;
            return true;
        }

        private static bool TryGetMax(List<DataRow> rows, string fieldId, out double maxValue)
        {
            int i;
            bool hasValue;
            double v;

            maxValue = 0;
            hasValue = false;

            if (fieldId == "")
                return false;

            for (i = 0; i < rows.Count; i++)
            {
                if (TryGetRowDouble(rows[i], fieldId, out v))
                {
                    if (!hasValue)
                    {
                        maxValue = v;
                        hasValue = true;
                    }
                    else
                    {
                        if (v > maxValue)
                            maxValue = v;
                    }
                }
            }

            return hasValue;
        }

        private static bool TryGetRowDouble(DataRow row, string fieldId, out double value)
        {
            string raw;

            value = 0;

            if (row == null) return false;
            if (!row.Table.Columns.Contains(fieldId)) return false;

            raw = row[fieldId].ToString();
            return double.TryParse(raw, out value);
        }

        private static int GetMinRowNo(List<DataRow> rows)
        {
            int i;
            int minValue;
            int v;

            if (rows.Count == 0)
                return 0;

            minValue = Convert.ToInt32(rows[0]["__RowNo"]);

            for (i = 1; i < rows.Count; i++)
            {
                v = Convert.ToInt32(rows[i]["__RowNo"]);
                if (v < minValue)
                    minValue = v;
            }

            return minValue;
        }

        private static int GetMaxRowNo(List<DataRow> rows)
        {
            int i;
            int maxValue;
            int v;

            if (rows.Count == 0)
                return 0;

            maxValue = Convert.ToInt32(rows[0]["__RowNo"]);

            for (i = 1; i < rows.Count; i++)
            {
                v = Convert.ToInt32(rows[i]["__RowNo"]);
                if (v > maxValue)
                    maxValue = v;
            }

            return maxValue;
        }

        private static void ApplyHeadersToGrid(DataGrid dg, DataGridLayoutConfig cfg)
        {
            int i;
            int k;
            string fieldId;
            string header;
            GridColumnDefinition reg;

            for (i = 0; i < dg.Columns.Count; i++)
            {
                fieldId = dg.Columns[i].SortMemberPath;

                if (fieldId == "__GroupKey") continue;
                if (fieldId == "__GroupDisplay") continue;
                if (fieldId == "__RowNo") continue;

                header = null;

                for (k = 0; k < cfg.SelectedColumns.Count; k++)
                {
                    if (string.Equals(cfg.SelectedColumns[k].FieldId, fieldId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(cfg.SelectedColumns[k].Header))
                            header = cfg.SelectedColumns[k].Header;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(header))
                {
                    reg = ColumnRegistry.Get(fieldId);
                    if (reg != null && !string.IsNullOrWhiteSpace(reg.Header))
                        header = reg.Header;
                }

                if (!string.IsNullOrWhiteSpace(header))
                    dg.Columns[i].Header = header;
            }
        }

        private static bool ShouldUseGrouping(DataGridLayoutConfig cfg)
        {
            if (cfg == null) return false;
            if (cfg.Grouping == null) return false;
            if (!cfg.Grouping.Enabled) return false;

            if (cfg.Grouping.Mode == GroupMode.ByFixedCount)
                return true;

            if (cfg.Grouping.Mode == GroupMode.BySameValue)
            {
                if (cfg.Grouping.GroupFieldId != "")
                    return true;
            }

            if (cfg.Grouping.Mode == GroupMode.ByRange)
            {
                if (cfg.Grouping.GroupFieldId != "")
                    return true;
            }

            return false;
        }

        private static string GetGroupKey(DataTable sourceTable, DataRow row, int rowIndex, DataGridLayoutConfig cfg)
        {
            if (cfg.Grouping.Mode == GroupMode.ByFixedCount)
                return GetGroupKey_ByFixedCount(rowIndex, cfg.Grouping.FixedCount, sourceTable.Rows.Count);

            if (cfg.Grouping.Mode == GroupMode.BySameValue)
                return GetGroupKey_BySameValue(row, cfg.Grouping.GroupFieldId);

            if (cfg.Grouping.Mode == GroupMode.ByRange)
                return GetGroupKey_ByRange(row, cfg.Grouping);

            return "Ungrouped";
        }

        private static string GetGroupKey_ByFixedCount(int rowIndex, int fixedCount, int totalCount)
        {
            int g;
            int start;
            int end;

            if (fixedCount <= 0) fixedCount = 10;

            g = (rowIndex / fixedCount) + 1;
            start = ((g - 1) * fixedCount) + 1;
            end = g * fixedCount;

            if (end > totalCount)
                end = totalCount;

            return "Group " + g.ToString() + " (" + start.ToString() + "~" + end.ToString() + ")";
        }

        private static string GetGroupKey_BySameValue(DataRow row, string fieldId)
        {
            string s;

            if (row == null) return "Others";
            if (fieldId == "") return "Others";
            if (!row.Table.Columns.Contains(fieldId)) return "Others";

            s = row[fieldId].ToString();
            if (s == "") return "Others";

            return s;
        }

        private static string GetGroupKey_ByRange(DataRow row, GroupConfig grouping)
        {
            string raw;
            double v;
            int i;
            GroupRangeDef rg;

            if (row == null) return "Others";
            if (grouping == null) return "Others";
            if (grouping.GroupFieldId == "") return "Others";
            if (!row.Table.Columns.Contains(grouping.GroupFieldId)) return "Others";

            raw = row[grouping.GroupFieldId].ToString();
            if (!double.TryParse(raw, out v))
                return "Others";

            for (i = 0; i < grouping.Ranges.Count; i++)
            {
                rg = grouping.Ranges[i];
                if (v >= rg.Min && v <= rg.Max)
                    return rg.Name;
            }

            return "Others";
        }
        }
    }
