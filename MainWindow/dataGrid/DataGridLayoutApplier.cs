using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;
using System.Windows.Data;

namespace NEW_QCtech
{
    class DataGridLayoutApplier
    {
        public static void ApplyToDataGrid(DataGrid dg, DataTable sourceTable, DataGridLayoutConfig cfg)
        {
            if (dg == null || sourceTable == null || cfg == null)
                return;

            dg.ItemsSource = null;
            dg.Columns.Clear();
            dg.AutoGenerateColumns = true;
            dg.AutoGeneratingColumn -= Dg_AutoGeneratingColumn;
            dg.AutoGeneratingColumn += Dg_AutoGeneratingColumn;

            DataTable displayTable = BuildDisplayTable(sourceTable, cfg);
            dg.Tag = cfg;
            dg.ItemsSource = displayTable.DefaultView;

            GridGroupingHelper.ApplyHeadersToGrid(dg, cfg.SelectedColumns);

            ICollectionView view = CollectionViewSource.GetDefaultView(dg.ItemsSource);
            if (view == null)
                return;

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
            if (e.PropertyName == "__GroupKey" || e.PropertyName == "__GroupDisplay" || e.PropertyName == "__RowNo")
                e.Cancel = true;
        }

        private static DataTable BuildDisplayTable(DataTable sourceTable, DataGridLayoutConfig cfg)
        {
            DataTable dt = new DataTable();
            bool useGrouping = ShouldUseGrouping(cfg);

            if (useGrouping)
            {
                dt.Columns.Add("__GroupKey", typeof(string));
                dt.Columns.Add("__GroupDisplay", typeof(string));
                dt.Columns.Add("__RowNo", typeof(int));
            }

            foreach (ColumnConfig column in cfg.SelectedColumns)
            {
                string fieldId = column.FieldId;
                if (sourceTable.Columns.Contains(fieldId))
                    dt.Columns.Add(fieldId, sourceTable.Columns[fieldId].DataType);
            }

            for (int r = 0; r < sourceTable.Rows.Count; r++)
            {
                DataRow srcRow = sourceTable.Rows[r];
                DataRow newRow = dt.NewRow();

                if (useGrouping)
                {
                    newRow["__GroupKey"] = GetGroupKey(sourceTable, srcRow, r, cfg);
                    newRow["__RowNo"] = r + 1;
                    newRow["__GroupDisplay"] = "";
                }

                foreach (ColumnConfig column in cfg.SelectedColumns)
                {
                    string fieldId = column.FieldId;
                    if (sourceTable.Columns.Contains(fieldId))
                        newRow[fieldId] = srcRow[fieldId];
                }

                dt.Rows.Add(newRow);
            }

            if (useGrouping)
                FillGroupDisplayTexts(dt, cfg);

            return dt;
        }

        private static void FillGroupDisplayTexts(DataTable dt, DataGridLayoutConfig cfg)
        {
            Dictionary<string, List<DataRow>> groups = new Dictionary<string, List<DataRow>>();

            foreach (DataRow row in dt.Rows)
            {
                string key = Convert.ToString(row["__GroupKey"]);
                if (!groups.ContainsKey(key))
                    groups[key] = new List<DataRow>();

                groups[key].Add(row);
            }

            foreach (KeyValuePair<string, List<DataRow>> kv in groups)
            {
                string display = GridGroupingHelper.BuildGroupDisplayText(
                    kv.Key,
                    kv.Value,
                    cfg.SelectedMetrics,
                    metric => ResolveMetricTargetFieldId(metric, cfg),
                    fieldId => GetColumnHeader(cfg, fieldId));

                foreach (DataRow row in kv.Value)
                    row["__GroupDisplay"] = display;
            }
        }

        private static string ResolveMetricTargetFieldId(GroupMetricDef metric, DataGridLayoutConfig cfg)
        {
            if (metric == null)
                return "";

            if (!string.IsNullOrWhiteSpace(metric.TargetFieldId))
                return metric.TargetFieldId;

            return GetDefaultMetricTargetFieldId(cfg);
        }

        private static string GetDefaultMetricTargetFieldId(DataGridLayoutConfig cfg)
        {
            if (cfg == null || cfg.SelectedColumns == null)
                return "";

            foreach (ColumnConfig column in cfg.SelectedColumns)
            {
                if (IsNumericFieldId(column.FieldId))
                    return column.FieldId;
            }

            return "";
        }

        private static bool IsNumericFieldId(string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
                return false;

            return ColumnRegistry.IsNumericField(fieldId);
        }

        private static string GetColumnHeader(DataGridLayoutConfig cfg, string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
                return "";

            if (cfg != null && cfg.SelectedColumns != null)
            {
                foreach (ColumnConfig column in cfg.SelectedColumns)
                {
                    if (string.Equals(column.FieldId, fieldId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(column.Header))
                            return column.Header;

                        break;
                    }
                }
            }

            GridColumnDefinition reg = ColumnRegistry.Get(fieldId);
            if (reg != null && !string.IsNullOrWhiteSpace(reg.Header))
                return reg.Header;

            return fieldId;
        }

        private static bool ShouldUseGrouping(DataGridLayoutConfig cfg)
        {
            return cfg != null && cfg.Grouping != null && cfg.Grouping.Enabled && cfg.Grouping.Mode != GroupMode.None;
        }

        private static string GetGroupKey(DataTable sourceTable, DataRow row, int rowIndex, DataGridLayoutConfig cfg)
        {
            if (cfg == null || cfg.Grouping == null || !cfg.Grouping.Enabled)
                return "";

            GroupConfig g = cfg.Grouping;

            if (g.Mode == GroupMode.BySameValue)
                return GetGroupKey_BySameValue(sourceTable, row, g);

            if (g.Mode == GroupMode.ByFixedCount)
                return GetGroupKey_ByFixedCount(rowIndex, g);

            if (g.Mode == GroupMode.ByRange)
                return GetGroupKey_ByRange(sourceTable, row, g);

            return "";
        }

        private static string GetGroupKey_BySameValue(DataTable sourceTable, DataRow row, GroupConfig g)
        {
            string fieldId = g.GroupFieldId;
            if (string.IsNullOrWhiteSpace(fieldId) || !sourceTable.Columns.Contains(fieldId))
                return "Others";

            string raw = Convert.ToString(row[fieldId]);
            return string.IsNullOrWhiteSpace(raw) ? "Others" : raw;
        }

        private static string GetGroupKey_ByFixedCount(int rowIndex, GroupConfig g)
        {
            int size = g.FixedCount <= 0 ? 10 : g.FixedCount;
            int start = (rowIndex / size) * size + 1;
            int end = start + size - 1;
            return "Group " + ((rowIndex / size) + 1) + " (" + start + "~" + end + ")";
        }

        private static string GetGroupKey_ByRange(DataTable sourceTable, DataRow row, GroupConfig g)
        {
            string fieldId = g.GroupFieldId;
            if (string.IsNullOrWhiteSpace(fieldId) || !sourceTable.Columns.Contains(fieldId))
                return "Others";

            if (!double.TryParse(Convert.ToString(row[fieldId]), out double value))
                return "Others";

            if (g.Ranges != null)
            {
                foreach (GroupRangeDef range in g.Ranges)
                {
                    if (value >= range.Min && value <= range.Max)
                        return range.Name;
                }
            }

            return "Others";
        }
    }
}
