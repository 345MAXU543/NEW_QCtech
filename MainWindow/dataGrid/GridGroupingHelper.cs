using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls;

namespace NEW_QCtech.MainWindow.dataGrid
{
    internal static class GridGroupingHelper
    {
        public static string BuildGroupDisplayText(
            string groupKey,
            List<DataRow> rows,
            IList<GroupMetricDef> metrics,
            Func<GroupMetricDef, string> resolveTargetFieldId,
            Func<string, string> getHeader)
        {
            string text = groupKey ?? "";

            if (rows == null)
                rows = new List<DataRow>();

            if (metrics == null || metrics.Count == 0)
                return text + "   |   Count = " + rows.Count;

            foreach (GroupMetricDef metric in metrics)
            {
                string metricText = BuildMetricText(metric, rows, resolveTargetFieldId, getHeader);
                if (!string.IsNullOrWhiteSpace(metricText))
                    text += "   |   " + metricText;
            }

            return text;
        }

        public static string BuildMetricText(
            GroupMetricDef metric,
            List<DataRow> rows,
            Func<GroupMetricDef, string> resolveTargetFieldId,
            Func<string, string> getHeader)
        {
            if (metric == null)
                return "";

            if (rows == null)
                rows = new List<DataRow>();

            if (metric.Id == "Count")
                return "Count = " + rows.Count;

            if (metric.Id == "index_range")
                return "Range = " + GetMinRowNo(rows) + "~" + GetMaxRowNo(rows);

            string fieldId = resolveTargetFieldId != null ? (resolveTargetFieldId(metric) ?? "") : (metric.TargetFieldId ?? "");
            string header = getHeader != null ? (getHeader(fieldId) ?? fieldId) : fieldId;

            if (metric.Id == "Avg")
            {
                if (!TryGetAverage(rows, fieldId, out double avgValue))
                    return "Avg(" + header + ") = N/A";

                return "Avg(" + header + ") = " + avgValue.ToString("0.###");
            }

            if (metric.Id == "Max")
            {
                if (!TryGetMax(rows, fieldId, out double maxValue))
                    return "Max(" + header + ") = N/A";

                return "Max(" + header + ") = " + maxValue.ToString("0.###");
            }

            return metric.Name ?? "";
        }

        public static bool TryGetAverage(List<DataRow> rows, string fieldId, out double avgValue)
        {
            avgValue = 0;
            if (string.IsNullOrWhiteSpace(fieldId) || rows == null || rows.Count == 0)
                return false;

            double sum = 0;
            int count = 0;

            foreach (DataRow row in rows)
            {
                if (TryGetRowDouble(row, fieldId, out double value))
                {
                    sum += value;
                    count++;
                }
            }

            if (count <= 0)
                return false;

            avgValue = sum / count;
            return true;
        }

        public static bool TryGetMax(List<DataRow> rows, string fieldId, out double maxValue)
        {
            maxValue = 0;
            if (string.IsNullOrWhiteSpace(fieldId) || rows == null || rows.Count == 0)
                return false;

            bool hasValue = false;

            foreach (DataRow row in rows)
            {
                if (TryGetRowDouble(row, fieldId, out double value))
                {
                    if (!hasValue || value > maxValue)
                    {
                        maxValue = value;
                        hasValue = true;
                    }
                }
            }

            return hasValue;
        }

        public static bool TryGetRowDouble(DataRow row, string fieldId, out double value)
        {
            value = 0;

            if (row == null || string.IsNullOrWhiteSpace(fieldId))
                return false;

            if (row.Table == null || !row.Table.Columns.Contains(fieldId))
                return false;

            object raw = row[fieldId];
            if (raw == null || raw == DBNull.Value)
                return false;

            return double.TryParse(raw.ToString(), out value);
        }

        public static int GetMinRowNo(List<DataRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return 0;

            return rows
                .Where(r => r.Table != null && r.Table.Columns.Contains("__RowNo"))
                .Select(r => Convert.ToInt32(r["__RowNo"]))
                .DefaultIfEmpty(0)
                .Min();
        }

        public static int GetMaxRowNo(List<DataRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return 0;

            return rows
                .Where(r => r.Table != null && r.Table.Columns.Contains("__RowNo"))
                .Select(r => Convert.ToInt32(r["__RowNo"]))
                .DefaultIfEmpty(0)
                .Max();
        }

        public static void ApplyHeadersToGrid(DataGrid dg, IList<ColumnConfig> selectedColumns)
        {
            if (dg == null || selectedColumns == null)
                return;

            foreach (DataGridColumn column in dg.Columns)
            {
                string fieldId = column.SortMemberPath;
                if (fieldId == "__GroupKey" || fieldId == "__GroupDisplay" || fieldId == "__RowNo")
                    continue;

                ColumnConfig cfg = selectedColumns.FirstOrDefault(x => string.Equals(x.FieldId, fieldId, StringComparison.OrdinalIgnoreCase));
                if (cfg != null && !string.IsNullOrWhiteSpace(cfg.Header))
                    column.Header = cfg.Header;
            }
        }
    }
}
