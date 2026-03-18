using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NEW_QCtech
{
    public partial class DataGridSetting_Window : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        // ===== B 區：Catalog Roots (TreeView) =====
        public ObservableCollection<TreeNode> CatalogRoots { get; private set; } = new ObservableCollection<TreeNode>();

        // ===== A 區：Selected Columns + Preview Rows =====
        public ObservableCollection<ColumnConfig> SelectedColumns { get; private set; } = new ObservableCollection<ColumnConfig>();
        public ObservableCollection<PreviewRow> PreviewRows { get; private set; } = new ObservableCollection<PreviewRow>();
        public ICollectionView PreviewView { get; private set; } = null!;

        private ColumnConfig? _selectedColumn;
        public ColumnConfig? SelectedColumn
        {
            get => _selectedColumn;
            set
            {
                _selectedColumn = value;
                Raise(nameof(SelectedColumn));
                Raise(nameof(HasSelectedColumn));
                Raise(nameof(ColumnEditorHint));
            }
        }

        public bool HasSelectedColumn => SelectedColumn != null;

        public string ColumnEditorHint =>
            (SelectedColumn == null) ? "請在 A 區 DataGrid 點選一個欄位。" : "你可以在這裡修改欄位屬性（Header/Format/OKNG）。";

        // ===== C 下：Grouping =====
        public GroupingConfig Grouping { get; private set; } = new GroupingConfig();
        public ObservableCollection<FieldDef> GroupKeyCandidates { get; private set; } = new ObservableCollection<FieldDef>();

        private FieldDef? _selectedFieldInTree;
        private MetricDef? _selectedMetricInTree;

        private readonly List<FieldDef> _allFields = new();
        private readonly List<MetricDef> _allMetricDefs = new();

        public DataGridSetting_Window()
        {
            InitializeComponent();
            DataContext = this;

            BuildDemoCatalog();
            BuildTree();

            // demo 預設欄位
            AddColumnByFieldId("idx");
            AddColumnByFieldId("name");
            AddColumnByFieldId("force");
            AddColumnByFieldId("main");
            AddColumnByFieldId("sub");
            AddColumnByFieldId("color");
            AddColumnByFieldId("disp");
            AddColumnByFieldId("vel");

            SelectedColumn = SelectedColumns.Count > 0 ? SelectedColumns[0] : null;

            // 建 View（DataGrid 綁它）
            PreviewView = CollectionViewSource.GetDefaultView(PreviewRows);
            Raise(nameof(PreviewView));

            // ✅ 只留一個入口：任何配置改動 → RefreshPreview()
            Grouping.PropertyChanged += Grouping_PropertyChanged;

            SelectedColumns.CollectionChanged += SelectedColumns_CollectionChanged;


            RefreshPreview();
        }
        private void Grouping_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GroupingConfig.Enabled) ||
                e.PropertyName == nameof(GroupingConfig.GroupKey1FieldId))
            {
                RefreshPreview();
            }
        }
        private void SelectedColumns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshPreview();
        }

        // -------------------- ✅ 乾淨維護：一個入口刷新 --------------------
        private void RefreshPreview()
        {
            RebuildPreviewColumns();
            RebuildPreviewRows();
            ApplyPreviewGrouping();
            PreviewView?.Refresh();
        }

        // -------------------- PreviewRow --------------------
        public class PreviewRow : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            private readonly Dictionary<string, object?> _data = new();

            public object? this[string key]
            {
                get => _data.TryGetValue(key, out var v) ? v : "";
                set
                {
                    _data[key] = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[" + key + "]"));
                }
            }

            private string _groupKey = "";
            public string GroupKey
            {
                get => _groupKey;
                set
                {
                    _groupKey = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupKey)));
                }
            }
        }

        // -------------------- Models --------------------
        public class FieldDef
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string GroupPath { get; set; } = "";
            public bool IsUsed { get; set; } = false;
        }

        public class ColumnConfig : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            private void RaiseLocal(string n)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(n));
                }
            }

            private string _fieldId = "";
            public string FieldId { get => _fieldId; set { _fieldId = value; RaiseLocal(nameof(FieldId)); } }

            private string _header = "";
            public string Header { get => _header; set { _header = value; RaiseLocal(nameof(Header)); } }

            private string _format = "";
            public string Format { get => _format; set { _format = value; RaiseLocal(nameof(Format)); } }

            private string _okNgRule = "";
            public string OkNgRule { get => _okNgRule; set { _okNgRule = value; RaiseLocal(nameof(OkNgRule)); } }

            private string _okNgKey = "";
            public string OkNgKey { get => _okNgKey; set { _okNgKey = value; RaiseLocal(nameof(OkNgKey)); } }

            private string _okNgNum = "";
            public string OkNgNum { get => _okNgNum; set { _okNgNum = value; RaiseLocal(nameof(OkNgNum)); } }
        }

        public class MetricDef
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
        }

        public class MetricConfig
        {
            public string MetricId { get; set; } = "";
            public string SourceFieldId { get; set; } = "";
            public string Format { get; set; } = "0.###";

            public string Display
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(SourceFieldId))
                        return MetricId;

                    return MetricId + "(" + SourceFieldId + ")";
                }
            }
        }

        public class GroupingConfig : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            private void RaiseLocal(string n)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(n));
                }
            }

            private bool _enabled = false;
            public bool Enabled { get => _enabled; set { _enabled = value; RaiseLocal(nameof(Enabled)); } }

            private string _groupKey1FieldId = "";
            public string GroupKey1FieldId { get => _groupKey1FieldId; set { _groupKey1FieldId = value; RaiseLocal(nameof(GroupKey1FieldId)); } }

            public ObservableCollection<MetricConfig> SelectedMetrics { get; private set; } = new ObservableCollection<MetricConfig>();

            private MetricConfig? _selectedMetric;
            public MetricConfig? SelectedMetric { get => _selectedMetric; set { _selectedMetric = value; RaiseLocal(nameof(SelectedMetric)); } }
        }

        public class TreeNode
        {
            public string Title { get; set; } = "";
            public ObservableCollection<TreeNode> Children { get; private set; } = new ObservableCollection<TreeNode>();
            public FieldDef? Field { get; set; }
            public MetricDef? Metric { get; set; }
        }

        // -------------------- ✅ 點 ColumnHeader 就選 ColumnConfig --------------------
        private void dgPreview_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // 往上找 DataGridColumnHeader（點到 header 才處理）
            while (dep != null && dep is not DataGridColumnHeader)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridColumnHeader ch)
            {
                if (ch.Column?.Header is ColumnConfig cc)
                    SelectedColumn = cc;
            }
        }

        // -------------------- Preview Columns（用 SelectedColumns 生成） --------------------
        private void RebuildPreviewColumns()
        {
            dgPreview.Columns.Clear();

            foreach (var col in SelectedColumns)
            {
                var dgCol = new DataGridTextColumn
                {
                    Header = col, // Header 放 ColumnConfig（維護最舒服）
                    Width = 80
                };

                var b = new Binding("[" + col.FieldId + "]")
                {
                    Mode = BindingMode.OneWay
                };

                if (!string.IsNullOrWhiteSpace(col.Format))
                    b.StringFormat = col.Format;

                dgCol.Binding = b;
                dgPreview.Columns.Add(dgCol);
            }
        }

        // -------------------- Preview Rows --------------------
        private void RebuildPreviewRows()
        {
            PreviewRows.Clear();

            for (int r = 0; r < 30; r++)
            {
                var row = new PreviewRow();

                foreach (var col in SelectedColumns)
                    row[col.FieldId] = MakeSampleValue(col.FieldId, r);

                // GroupKey
                if (Grouping.Enabled && !string.IsNullOrWhiteSpace(Grouping.GroupKey1FieldId))
                {
                    var v = row[Grouping.GroupKey1FieldId];
                    row.GroupKey = v?.ToString() ?? "";
                }
                else row.GroupKey = "";

                PreviewRows.Add(row);
            }
        }

        private void ApplyPreviewGrouping()
        {
            if (PreviewView == null) return;

            PreviewView.GroupDescriptions.Clear();

            if (Grouping.Enabled && !string.IsNullOrWhiteSpace(Grouping.GroupKey1FieldId))
                PreviewView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PreviewRow.GroupKey)));
        }

        private object MakeSampleValue(string fieldId, int rowIndex)
        {
            if (fieldId == "idx") return rowIndex.ToString();
            if (fieldId == "name") return "Test " + rowIndex;

            if (fieldId == "force") return 10.0 + rowIndex * 1.23;
            if (fieldId == "disp") return 50.0 + rowIndex * 0.12;
            if (fieldId == "vel") return 0.10 + rowIndex * 0.01;

            if (fieldId == "main") return (rowIndex == 0) ? "●" : "";
            if (fieldId == "sub") return (rowIndex % 2 == 0) ? "☑" : "☐";
            if (fieldId == "color") return (rowIndex % 2 == 0) ? "Orange" : "Green";

            return "…";
        }

        // -------------------- Demo Catalog --------------------
        private void BuildDemoCatalog()
        {
            _allFields.Clear();
            _allMetricDefs.Clear();
            GroupKeyCandidates.Clear();

            AddField("idx", "Index", "Basic");
            AddField("name", "Name", "Basic");

            AddField("main", "Main", "Select");
            AddField("sub", "Sub", "Select");

            AddField("color", "Color", "Style");

            AddField("force", "Force", "Signal");
            AddField("disp", "Displacement", "Signal");
            AddField("vel", "Velocity", "Signal");

            AddField("disp", "Displacement", "Sal");
            AddField("disp", "Displacement", "Sal");
            foreach (var f in _allFields)
                GroupKeyCandidates.Add(f);

            AddMetricDef("Count", "Count");
            AddMetricDef("Sum", "Sum");
            AddMetricDef("Avg", "Avg");
            AddMetricDef("Min", "Min");
            AddMetricDef("Max", "Max");

            Grouping.GroupKey1FieldId = "idx";
        }

        private void AddField(string id, string name, string groupPath)
            => _allFields.Add(new FieldDef { Id = id, Name = name, GroupPath = groupPath, IsUsed = false });

        private void AddMetricDef(string id, string name)
            => _allMetricDefs.Add(new MetricDef { Id = id, Name = name });

        private void BuildTree()
        {
            CatalogRoots.Clear();

            var columnsRoot = new TreeNode { Title = "Columns" };
            CatalogRoots.Add(columnsRoot);

            var metricsRoot = new TreeNode { Title = "Metrics" };
            CatalogRoots.Add(metricsRoot);

            foreach (var f in _allFields)
                AddFieldToTree(columnsRoot, f);

            foreach (var m in _allMetricDefs)
                metricsRoot.Children.Add(new TreeNode { Title = m.Name, Metric = m });
        }

        private void AddFieldToTree(TreeNode root, FieldDef field)
        {
            string[] parts = field.GroupPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            TreeNode current = root;

            foreach (var groupName in parts)
            {
                var found = current.Children.FirstOrDefault(n =>
                    n.Field == null && n.Metric == null && n.Title == groupName);

                if (found == null)
                {
                    found = new TreeNode { Title = groupName };
                    current.Children.Add(found);
                }
                current = found;
            }

            current.Children.Add(new TreeNode { Title = field.Name, Field = field });
        }

        // -------------------- Tree Selection --------------------
        private void tvCatalog_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedFieldInTree = null;
            _selectedMetricInTree = null;

            if (tvCatalog.SelectedItem is not TreeNode node) return;

            if (node.Field != null) _selectedFieldInTree = node.Field;
            if (node.Metric != null) _selectedMetricInTree = node.Metric;
        }

        // -------------------- Buttons --------------------
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFieldInTree != null)
            {
                AddColumnByFieldId(_selectedFieldInTree.Id);
                return;
            }

            if (_selectedMetricInTree != null)
            {
                var mc = new MetricConfig
                {
                    MetricId = _selectedMetricInTree.Id,
                    SourceFieldId = "force",
                    Format = "0.###"
                };

                Grouping.SelectedMetrics.Add(mc);
                Grouping.SelectedMetric = mc;
                PreviewView?.Refresh();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedColumn == null) return;

            string fid = SelectedColumn.FieldId;
            int idx = SelectedColumns.IndexOf(SelectedColumn);

            SelectedColumns.Remove(SelectedColumn);
            SetFieldUsed(fid, false);

            SelectedColumn = (SelectedColumns.Count == 0) ? null
                : SelectedColumns[Math.Max(0, Math.Min(idx, SelectedColumns.Count - 1))];
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedColumn == null) return;
            int idx = SelectedColumns.IndexOf(SelectedColumn);
            if (idx <= 0) return;
            SelectedColumns.Move(idx, idx - 1);
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedColumn == null) return;
            int idx = SelectedColumns.IndexOf(SelectedColumn);
            if (idx < 0 || idx >= SelectedColumns.Count - 1) return;
            SelectedColumns.Move(idx, idx + 1);
        }

        private void btnMetricUp_Click(object sender, RoutedEventArgs e)
        {
            if (Grouping.SelectedMetric == null) return;
            int idx = Grouping.SelectedMetrics.IndexOf(Grouping.SelectedMetric);
            if (idx <= 0) return;
            Grouping.SelectedMetrics.Move(idx, idx - 1);
            PreviewView?.Refresh();
        }

        private void btnMetricDown_Click(object sender, RoutedEventArgs e)
        {
            if (Grouping.SelectedMetric == null) return;
            int idx = Grouping.SelectedMetrics.IndexOf(Grouping.SelectedMetric);
            if (idx < 0 || idx >= Grouping.SelectedMetrics.Count - 1) return;
            Grouping.SelectedMetrics.Move(idx, idx + 1);
            PreviewView?.Refresh();
        }

        private void btnMetricRemove_Click(object sender, RoutedEventArgs e)
        {
            if (Grouping.SelectedMetric == null) return;

            int idx = Grouping.SelectedMetrics.IndexOf(Grouping.SelectedMetric);
            Grouping.SelectedMetrics.Remove(Grouping.SelectedMetric);
            Grouping.SelectedMetric = null;

            if (Grouping.SelectedMetrics.Count > 0)
            {
                if (idx >= Grouping.SelectedMetrics.Count) idx = Grouping.SelectedMetrics.Count - 1;
                Grouping.SelectedMetric = Grouping.SelectedMetrics[idx];
            }

            PreviewView?.Refresh();
        }

        // -------------------- Helpers --------------------
        private void AddColumnByFieldId(string fieldId)
        {
            var f = FindField(fieldId);
            if (f == null || f.IsUsed) return;

            var col = new ColumnConfig
            {
                FieldId = f.Id,
                Header = f.Name,
                Format = "",
                OkNgRule = "",
            };

            SelectedColumns.Add(col);
            SelectedColumn = col;

            f.IsUsed = true;
        }
        private FieldDef FindField(string id)
        {
            for (int i = 0; i < _allFields.Count; i++)
            {
                if (_allFields[i].Id == id)
                {
                    return _allFields[i];
                }
            }
            return null;
        }
        private void SetFieldUsed(string fieldId, bool used)
        {
            var f = FindField(fieldId);
            if (f != null) f.IsUsed = used;
        }
    }

    // ==================== ✅ GroupHeader Converter（集中統計邏輯） ====================
    public class GroupHeaderTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = CollectionViewGroup
            // values[1] = GroupingConfig (Window.Grouping)
            if (values == null || values.Length < 2) return "";

            var g = values[0] as CollectionViewGroup;
            var grouping = values[1] as DataGridSetting_Window.GroupingConfig;

            if (g == null) return "";

            string groupName = g.Name?.ToString() ?? "";
            int count = g.ItemCount;

            string text = $"Group: {groupName}   Count: {count}";

            if (grouping == null || !grouping.Enabled) return text;
            if (grouping.SelectedMetrics == null || grouping.SelectedMetrics.Count == 0) return text;

            // g.Items => PreviewRow
            var rows = new List<DataGridSetting_Window.PreviewRow>();
            foreach (var it in g.Items)
                if (it is DataGridSetting_Window.PreviewRow pr) rows.Add(pr);

            foreach (var mc in grouping.SelectedMetrics)
            {
                string metricId = mc.MetricId ?? "";
                string fieldId = mc.SourceFieldId ?? "";

                if (string.Equals(metricId, "Count", StringComparison.OrdinalIgnoreCase))
                {
                    text += $"   |   Count={count}";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fieldId))
                    continue;

                var vals = new List<double>();
                foreach (var r in rows)
                {
                    var raw = r[fieldId];
                    if (TryToDouble(raw, out double dv))
                        vals.Add(dv);
                }

                if (vals.Count == 0) continue;

                if (string.Equals(metricId, "Sum", StringComparison.OrdinalIgnoreCase))
                    text += $"   |   Sum({fieldId})={Format(vals.Sum(), mc.Format)}";
                else if (string.Equals(metricId, "Avg", StringComparison.OrdinalIgnoreCase))
                    text += $"   |   Avg({fieldId})={Format(vals.Average(), mc.Format)}";
                else if (string.Equals(metricId, "Min", StringComparison.OrdinalIgnoreCase))
                    text += $"   |   Min({fieldId})={Format(vals.Min(), mc.Format)}";
                else if (string.Equals(metricId, "Max", StringComparison.OrdinalIgnoreCase))
                    text += $"   |   Max({fieldId})={Format(vals.Max(), mc.Format)}";
            }

            return text;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static bool TryToDouble(object? raw, out double v)
        {
            v = 0;
            if (raw == null) return false;

            if (raw is double d) { v = d; return true; }
            if (raw is float f) { v = f; return true; }
            if (raw is int i) { v = i; return true; }
            if (raw is long l) { v = l; return true; }

            string s = raw.ToString() ?? "";
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ||
                   double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v);
        }

        private static string Format(double v, string fmt)
        {
            if (string.IsNullOrWhiteSpace(fmt)) return v.ToString("0.###");
            try { return v.ToString(fmt); } catch { return v.ToString("0.###"); }
        }
    }
}