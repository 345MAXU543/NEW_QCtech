using NEW_QCtech.dataGrid.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NEW_QCtech.dataGrid;

namespace NEW_QCtech
{
    public partial class DataGridSetting_Window2 : Window
    {
        private class ColumnFieldDef
        {
            public string Id = "";
            public string Name = "";
            public string PathInTree = "";
            public ColumnFieldUIType UIType = ColumnFieldUIType.Text;
        }

        private class GroupModeItem
        {
            public string Text = "";
            public GroupMode Mode = GroupMode.None;

            public override string ToString()
            {
                return Text;
            }
        }

        private List<ColumnFieldDef> _allFields = new List<ColumnFieldDef>();
        private List<GroupMetricDef> _allMetrics = new List<GroupMetricDef>();

        private ColumnFieldDef _selectedField = null;
        private GroupMetricDef _selectedMetric = null;

        private List<ColumnConfig> _selectedColumns = new List<ColumnConfig>();
        private List<GroupMetricDef> _selectedMetrics = new List<GroupMetricDef>();

        private DataTable _previewTable = new DataTable();
        private GroupConfig _grouping = new GroupConfig();

        private TextBlock _txtGroupHint = null;
        private ComboBox _cbGroupField = null;
        private TextBox _tbFixedCount = null;
        private TextBox _tbR1Min = null;
        private TextBox _tbR1Max = null;
        private TextBox _tbR2Min = null;
        private TextBox _tbR2Max = null;
        private TextBox _tbR3Min = null;
        private TextBox _tbR3Max = null;

        private ListBox _lbMetricPreview = null;
        private ComboBox _cbMetricTargetField = null;
        private TextBlock _txtMetricInfo = null;

        private DataTable _sourceTable = null;

        private bool _configLoadedFromOutside = false;

        public DataGridLayoutConfig ResultConfig = null;

        public DataGridSetting_Window2()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TreeViewLoadData();

            dg_Preview.AutoGenerateColumns = true;
            dg_Preview.AutoGeneratingColumn += dgPreview_AutoGeneratingColumn;
            dg_Preview.ColumnReordered += dg_Preview_ColumnReordered;

            chk_isShow.Checked += chk_isShow_CheckedChanged;
            chk_isShow.Unchecked += chk_isShow_CheckedChanged;

            InitBottomRightMetricArea();
            InitGroupModeCombo();
            BuildGroupOptionUi();

            if (_configLoadedFromOutside)
            {
                ApplyLoadedConfigToUi();
                RefreshGroupFieldCombo();
                RefreshMetricTargetFieldCombo();
            }

            RebuildPreviewGrid();
            RefreshMetricPreviewList();
        }

        /// <summary>
        /// 舊版呼叫方式保留
        /// </summary>
        public void LoadFromConfig(DataGridLayoutConfig cfg)
        {
            LoadFromConfig(cfg, null);
        }

        /// <summary>
        /// 主視窗呼叫：把目前設定 + 原始 DataTable 傳進設定器
        /// </summary>
        public void LoadFromConfig(DataGridLayoutConfig cfg, DataTable sourceTable)
        {
            int i;

            _sourceTable = sourceTable;
            _configLoadedFromOutside = true;

            _selectedColumns.Clear();
            _selectedMetrics.Clear();
            _grouping = new GroupConfig();

            if (cfg == null)
            {
                ResultConfig = null;

                if (IsLoaded)
                {
                    TreeViewLoadData();
                    BuildGroupOptionUi();
                    RebuildPreviewGrid();
                    RefreshMetricPreviewList();
                }

                return;
            }

            if (cfg.SelectedColumns != null)
            {
                for (i = 0; i < cfg.SelectedColumns.Count; i++)
                {
                    _selectedColumns.Add(CloneColumnConfig(cfg.SelectedColumns[i]));
                }
            }

            if (cfg.SelectedMetrics != null)
            {
                for (i = 0; i < cfg.SelectedMetrics.Count; i++)
                {
                    _selectedMetrics.Add(CloneGroupMetric(cfg.SelectedMetrics[i]));
                }
            }

            _grouping = CloneGroupConfig(cfg.Grouping);
            ResultConfig = BuildResultConfig();

            if (IsLoaded)
            {
                TreeViewLoadData();
                ApplyLoadedConfigToUi();
                RefreshGroupFieldCombo();
                RefreshMetricTargetFieldCombo();
                RebuildPreviewGrid();
                RefreshMetricPreviewList();
            }
        }

        /// <summary>
        /// 主視窗 ShowDialog() 後取回設定
        /// </summary>
        public DataGridLayoutConfig ExportConfig()
        {
            ResultConfig = BuildResultConfig();
            return ResultConfig;
        }

        private void ApplyLoadedConfigToUi()
        {
            int i;
            GroupModeItem item;

            chk_isShow.IsChecked = _grouping != null && _grouping.Enabled;

            if (_grouping == null)
                _grouping = new GroupConfig();

            for (i = 0; i < cb_GroupMode.Items.Count; i++)
            {
                item = cb_GroupMode.Items[i] as GroupModeItem;
                if (item != null && item.Mode == _grouping.Mode)
                {
                    cb_GroupMode.SelectedIndex = i;
                    break;
                }
            }

            BuildGroupOptionUi();
            ApplyGroupingValueToUiControls();
        }

        private void ApplyGroupingValueToUiControls()
        {
            int i;

            if (_cbGroupField != null && _grouping.GroupFieldId != "")
            {
                for (i = 0; i < _cbGroupField.Items.Count; i++)
                {
                    if (_cbGroupField.Items[i].ToString() == _grouping.GroupFieldId)
                    {
                        _cbGroupField.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (_tbFixedCount != null)
            {
                _tbFixedCount.Text = _grouping.FixedCount.ToString();
            }

            if (_grouping.Ranges != null && _grouping.Ranges.Count > 0)
            {
                if (_tbR1Min != null && _tbR1Max != null && _grouping.Ranges.Count >= 1)
                {
                    _tbR1Min.Text = _grouping.Ranges[0].Min.ToString("0.###");
                    _tbR1Max.Text = _grouping.Ranges[0].Max.ToString("0.###");
                }

                if (_tbR2Min != null && _tbR2Max != null && _grouping.Ranges.Count >= 2)
                {
                    _tbR2Min.Text = _grouping.Ranges[1].Min.ToString("0.###");
                    _tbR2Max.Text = _grouping.Ranges[1].Max.ToString("0.###");
                }

                if (_tbR3Min != null && _tbR3Max != null && _grouping.Ranges.Count >= 3)
                {
                    _tbR3Min.Text = _grouping.Ranges[2].Min.ToString("0.###");
                    _tbR3Max.Text = _grouping.Ranges[2].Max.ToString("0.###");
                }
            }
        }

        #region Tree

        private void TreeViewLoadData()
        {
            BuildTreeViewData();
            PutDataToTreeViewUI();
        }

        private void BuildTreeViewData()
        {
            _allFields.Clear();
            _allMetrics.Clear();

            // 先從集中欄位中心讀
            foreach (GridColumnDefinition def in ColumnRegistry.GetAll())
            {
                AddField(def.FieldId, def.Header, def.PathInTree, def.UIType);
            }

            // 如果主視窗有傳實際 DataTable 進來，補上 registry 沒有的欄位
            if (_sourceTable != null && _sourceTable.Columns.Count > 0)
            {
                foreach (DataColumn col in _sourceTable.Columns)
                {
                    bool exists = _allFields.Any(x =>
                        string.Equals(x.Id, col.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        AddField(col.ColumnName, col.ColumnName, "資料表", GuessUiTypeByColumn(col));
                    }
                }
            }

            AddMetric("Count", "Count");
            AddMetric("Avg", "Average");
            AddMetric("Max", "Maximum");
            AddMetric("index_range", "筆數區間");
        }

        private ColumnFieldUIType GuessUiTypeByColumn(DataColumn col)
        {
            if (col == null) return ColumnFieldUIType.Text;

            if (col.ColumnName == "Main")
                return ColumnFieldUIType.RadioButton;

            if (col.ColumnName == "Sub")
                return ColumnFieldUIType.CheckBox;

            if (col.ColumnName == "ColorBrush")
                return ColumnFieldUIType.Color;

            if (col.DataType == typeof(bool))
                return ColumnFieldUIType.CheckBox;

            return ColumnFieldUIType.Text;
        }

        private void AddField(string id, string name, string pathInTree, ColumnFieldUIType uiType)
        {
            ColumnFieldDef f = new ColumnFieldDef();
            f.Id = id;
            f.Name = name;
            f.PathInTree = pathInTree;
            f.UIType = uiType;
            _allFields.Add(f);
        }

        private void AddMetric(string id, string name)
        {
            GroupMetricDef m = new GroupMetricDef();
            m.Id = id;
            m.Name = name;
            _allMetrics.Add(m);
        }

        private void PutDataToTreeViewUI()
        {
            TreeViewItem rootColumns;
            TreeViewItem rootMetrics;
            TreeViewItem folder;
            int i;

            treeView.Items.Clear();

            rootColumns = NewFolderItem("Columns");
            rootMetrics = NewFolderItem("Metrics");

            treeView.Items.Add(rootColumns);
            treeView.Items.Add(rootMetrics);

            for (i = 0; i < _allFields.Count; i++)
            {
                folder = EnsureFolder(rootColumns, _allFields[i].PathInTree);
                folder.Items.Add(NewLeafItem(_allFields[i].Name, _allFields[i]));
            }

            for (i = 0; i < _allMetrics.Count; i++)
            {
                rootMetrics.Items.Add(NewLeafItem(_allMetrics[i].Name, _allMetrics[i]));
            }

            rootColumns.IsExpanded = true;
            rootMetrics.IsExpanded = true;
        }

        private TreeViewItem NewFolderItem(string title)
        {
            TreeViewItem t = new TreeViewItem();
            t.Header = title;
            t.Tag = null;
            t.Foreground = Brushes.White;
            return t;
        }

        private TreeViewItem NewLeafItem(string title, object tag)
        {
            TreeViewItem t = new TreeViewItem();
            t.Header = title;
            t.Tag = tag;
            t.Foreground = Brushes.White;
            return t;
        }

        private TreeViewItem EnsureFolder(TreeViewItem root, string groupPath)
        {
            string[] parts;
            TreeViewItem current;
            TreeViewItem found;
            TreeViewItem child;
            string folderName;
            int p;
            int i;

            if (string.IsNullOrWhiteSpace(groupPath))
                return root;

            parts = groupPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            current = root;

            for (p = 0; p < parts.Length; p++)
            {
                folderName = parts[p];
                found = null;

                for (i = 0; i < current.Items.Count; i++)
                {
                    child = current.Items[i] as TreeViewItem;
                    if (child == null) continue;

                    if (child.Tag == null && (child.Header as string) == folderName)
                    {
                        found = child;
                        break;
                    }
                }

                if (found == null)
                {
                    found = NewFolderItem(folderName);
                    current.Items.Add(found);
                }

                current = found;
            }

            return current;
        }

        #endregion

        #region Tree events

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem item;
            ColumnFieldDef f;
            GroupMetricDef m;

            _selectedField = null;
            _selectedMetric = null;

            item = treeView.SelectedItem as TreeViewItem;
            if (item == null) return;

            f = item.Tag as ColumnFieldDef;
            if (f != null)
            {
                _selectedField = f;
                return;
            }

            m = item.Tag as GroupMetricDef;
            if (m != null)
            {
                _selectedMetric = m;
                return;
            }
        }

        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item;
            ColumnFieldDef f;
            GroupMetricDef m;

            item = GetTreeViewItemFromOriginalSource(e.OriginalSource as DependencyObject);
            if (item == null) return;
            if (item.Tag == null) return;

            f = item.Tag as ColumnFieldDef;
            if (f != null)
            {
                AddSelectedColumnByField(f);
                RefreshGroupFieldCombo();
                RefreshMetricTargetFieldCombo();
                RebuildPreviewGrid();
                e.Handled = true;
                return;
            }

            m = item.Tag as GroupMetricDef;
            if (m != null)
            {
                AddGroupMetric(m);
                RefreshMetricPreviewList();
                RebuildPreviewGrid();
                e.Handled = true;
                return;
            }
        }

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedField != null)
            {
                AddSelectedColumnByField(_selectedField);
                RefreshGroupFieldCombo();
                RefreshMetricTargetFieldCombo();
                RebuildPreviewGrid();
                return;
            }

            if (_selectedMetric != null)
            {
                AddGroupMetric(_selectedMetric);
                RefreshMetricPreviewList();
                RebuildPreviewGrid();
                return;
            }
        }

        private TreeViewItem GetTreeViewItemFromOriginalSource(DependencyObject d)
        {
            while (d != null)
            {
                TreeViewItem tvi = d as TreeViewItem;
                if (tvi != null) return tvi;

                d = VisualTreeHelper.GetParent(d);
            }

            return null;
        }

        #endregion

        #region Add selected

        private void AddSelectedColumnByField(ColumnFieldDef f)
        {
            int i;
            ColumnConfig c;
            GridColumnDefinition reg;

            if (f == null) return;

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                if (string.Equals(_selectedColumns[i].FieldId, f.Id, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            reg = ColumnRegistry.Get(f.Id);

            c = new ColumnConfig();
            c.FieldId = f.Id;
            c.Header = reg != null ? reg.Header : f.Name;
            c.Format = reg != null ? reg.Format : "";
            c.OkNgRule = "";
            c.UIType = reg != null ? reg.UIType : f.UIType;

            _selectedColumns.Add(c);
        }

        private void AddGroupMetric(GroupMetricDef m)
        {
            int i;
            GroupMetricDef nm;

            if (m == null) return;

            for (i = 0; i < _selectedMetrics.Count; i++)
            {
                if (_selectedMetrics[i].Id == m.Id)
                    return;
            }

            nm = CloneGroupMetric(m);
            nm.TargetFieldId = GetDefaultMetricTargetFieldId();

            _selectedMetrics.Add(nm);

            if (m.Id == "index_range")
            {
                SelectGroupMode(GroupMode.ByFixedCount);
            }
        }

        #endregion

        #region Group UI

        private void InitGroupModeCombo()
        {
            cb_GroupMode.Items.Clear();

            AddGroupModeItem("不分群", GroupMode.None);
            AddGroupModeItem("數值區間(3組)", GroupMode.ByRange);
            AddGroupModeItem("每N筆一群", GroupMode.ByFixedCount);
            AddGroupModeItem("相同值一群", GroupMode.BySameValue);
            AddGroupModeItem("自訂公式(不預覽)", GroupMode.CustomFormula);
            AddGroupModeItem("多條件(不預覽)", GroupMode.MultiCondition);
            AddGroupModeItem("巢狀群組(不預覽)", GroupMode.Nested);

            cb_GroupMode.SelectedIndex = 0;
            cb_GroupMode.SelectionChanged += cb_GroupMode_SelectionChanged;
        }

        private void AddGroupModeItem(string text, GroupMode mode)
        {
            GroupModeItem item = new GroupModeItem();
            item.Text = text;
            item.Mode = mode;
            cb_GroupMode.Items.Add(item);
        }

        private void SelectGroupMode(GroupMode mode)
        {
            int i;
            GroupModeItem item;

            for (i = 0; i < cb_GroupMode.Items.Count; i++)
            {
                item = cb_GroupMode.Items[i] as GroupModeItem;
                if (item != null && item.Mode == mode)
                {
                    cb_GroupMode.SelectedIndex = i;
                    break;
                }
            }
        }

        private void cb_GroupMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BuildGroupOptionUi();
            ReadGroupingFromUi();
            RebuildPreviewGrid();
        }

        private void chk_isShow_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ReadGroupingFromUi();
            RebuildPreviewGrid();
        }

        private void BuildGroupOptionUi()
        {
            GroupMode mode;

            mode = GetSelectedGroupMode();

            _cbGroupField = null;
            _tbFixedCount = null;
            _tbR1Min = null;
            _tbR1Max = null;
            _tbR2Min = null;
            _tbR2Max = null;
            _tbR3Min = null;
            _tbR3Max = null;

            wp_GroupOptionHost.Children.Clear();

            _txtGroupHint = new TextBlock();
            _txtGroupHint.Foreground = Brushes.Orange;
            _txtGroupHint.FontSize = 16;
            _txtGroupHint.Margin = new Thickness(0, 0, 0, 8);
            _txtGroupHint.TextWrapping = TextWrapping.Wrap;
            _txtGroupHint.Width = 280;

            if (mode == GroupMode.ByRange)
            {
                BuildGroupOptionUi_ByRange();
            }
            else if (mode == GroupMode.ByFixedCount)
            {
                BuildGroupOptionUi_ByFixedCount();
            }
            else if (mode == GroupMode.BySameValue)
            {
                BuildGroupOptionUi_BySameValue();
            }
            else if (mode == GroupMode.None)
            {
                _txtGroupHint.Text = "目前未啟用群組預覽。";
                wp_GroupOptionHost.Children.Add(_txtGroupHint);
            }
            else
            {
                _txtGroupHint.Text = "此分群模式目前在預覽區不提供預覽，實際套用時才會生效。";
                wp_GroupOptionHost.Children.Add(_txtGroupHint);
            }
        }

        private void BuildGroupOptionUi_ByRange()
        {
            wp_GroupOptionHost.Children.Add(NewWhiteLabel("欄位:"));
            _cbGroupField = NewFieldComboBox();
            wp_GroupOptionHost.Children.Add(_cbGroupField);

            wp_GroupOptionHost.Children.Add(NewWhiteLabel("G1:"));
            _tbR1Min = NewSmallTextBox("0");
            _tbR1Max = NewSmallTextBox("10");
            wp_GroupOptionHost.Children.Add(_tbR1Min);
            wp_GroupOptionHost.Children.Add(NewWhiteLabel("~"));
            wp_GroupOptionHost.Children.Add(_tbR1Max);

            wp_GroupOptionHost.Children.Add(NewWhiteLabel("G2:"));
            _tbR2Min = NewSmallTextBox("11");
            _tbR2Max = NewSmallTextBox("20");
            wp_GroupOptionHost.Children.Add(_tbR2Min);
            wp_GroupOptionHost.Children.Add(NewWhiteLabel("~"));
            wp_GroupOptionHost.Children.Add(_tbR2Max);

            wp_GroupOptionHost.Children.Add(NewWhiteLabel("G3:"));
            _tbR3Min = NewSmallTextBox("21");
            _tbR3Max = NewSmallTextBox("30");
            wp_GroupOptionHost.Children.Add(_tbR3Min);
            wp_GroupOptionHost.Children.Add(NewWhiteLabel("~"));
            wp_GroupOptionHost.Children.Add(_tbR3Max);

            wp_GroupOptionHost.Children.Add(_txtGroupHint);
            _txtGroupHint.Text = "數值落在區間內就會分到對應群組。";

            HookGroupControlEvents();
        }

        private void BuildGroupOptionUi_ByFixedCount()
        {
            wp_GroupOptionHost.Children.Add(NewWhiteLabel("每"));
            _tbFixedCount = NewSmallTextBox("10");
            wp_GroupOptionHost.Children.Add(_tbFixedCount);
            wp_GroupOptionHost.Children.Add(NewWhiteLabel("筆一群"));

            _txtGroupHint.Text = "目前預覽會依筆數自動顯示 Group 1 / Group 2 / Group 3...";
            wp_GroupOptionHost.Children.Add(_txtGroupHint);

            HookGroupControlEvents();
        }

        private void BuildGroupOptionUi_BySameValue()
        {
            wp_GroupOptionHost.Children.Add(NewWhiteLabel("欄位:"));
            _cbGroupField = NewFieldComboBox();
            wp_GroupOptionHost.Children.Add(_cbGroupField);

            _txtGroupHint.Text = "相同值的資料會被分到同一群。";
            wp_GroupOptionHost.Children.Add(_txtGroupHint);

            HookGroupControlEvents();
        }

        private void HookGroupControlEvents()
        {
            if (_cbGroupField != null)
                _cbGroupField.SelectionChanged += GroupOptionControl_Changed;

            if (_tbFixedCount != null)
                _tbFixedCount.TextChanged += GroupOptionControl_Changed;

            if (_tbR1Min != null) _tbR1Min.TextChanged += GroupOptionControl_Changed;
            if (_tbR1Max != null) _tbR1Max.TextChanged += GroupOptionControl_Changed;
            if (_tbR2Min != null) _tbR2Min.TextChanged += GroupOptionControl_Changed;
            if (_tbR2Max != null) _tbR2Max.TextChanged += GroupOptionControl_Changed;
            if (_tbR3Min != null) _tbR3Min.TextChanged += GroupOptionControl_Changed;
            if (_tbR3Max != null) _tbR3Max.TextChanged += GroupOptionControl_Changed;
        }

        private void GroupOptionControl_Changed(object sender, EventArgs e)
        {
            ReadGroupingFromUi();
            RebuildPreviewGrid();
        }

        private Label NewWhiteLabel(string text)
        {
            Label lb = new Label();
            lb.Content = text;
            lb.Foreground = Brushes.White;
            lb.FontSize = 18;
            lb.VerticalAlignment = VerticalAlignment.Center;
            lb.Padding = new Thickness(2);
            return lb;
        }

        private TextBox NewSmallTextBox(string text)
        {
            TextBox tb = new TextBox();
            tb.Text = text;
            tb.Width = 52;
            tb.Height = 28;
            tb.FontSize = 16;
            tb.Margin = new Thickness(2);
            return tb;
        }

        private ComboBox NewFieldComboBox()
        {
            ComboBox cb = new ComboBox();
            int i;

            cb.Width = 120;
            cb.Height = 30;
            cb.FontSize = 16;
            cb.Margin = new Thickness(2);
            cb.Items.Clear();

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                if (_selectedColumns[i].UIType == ColumnFieldUIType.Text)
                {
                    cb.Items.Add(_selectedColumns[i].FieldId);
                }
            }

            if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;

            return cb;
        }

        private void RefreshGroupFieldCombo()
        {
            string old;
            int i;

            if (_cbGroupField == null) return;

            old = "";
            if (_cbGroupField.SelectedItem != null)
                old = _cbGroupField.SelectedItem.ToString();

            _cbGroupField.Items.Clear();

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                if (_selectedColumns[i].UIType == ColumnFieldUIType.Text)
                {
                    _cbGroupField.Items.Add(_selectedColumns[i].FieldId);
                }
            }

            if (old != "")
            {
                for (i = 0; i < _cbGroupField.Items.Count; i++)
                {
                    if (_cbGroupField.Items[i].ToString() == old)
                    {
                        _cbGroupField.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (_cbGroupField.Items.Count > 0)
                _cbGroupField.SelectedIndex = 0;
        }

        private GroupMode GetSelectedGroupMode()
        {
            GroupModeItem item;

            if (cb_GroupMode.SelectedItem == null) return GroupMode.None;

            item = cb_GroupMode.SelectedItem as GroupModeItem;
            if (item == null) return GroupMode.None;

            return item.Mode;
        }

        private void ReadGroupingFromUi()
        {
            int n;

            if (_grouping == null)
                _grouping = new GroupConfig();

            _grouping.Enabled = chk_isShow.IsChecked == true;
            _grouping.Mode = GetSelectedGroupMode();

            _grouping.GroupFieldId = "";
            _grouping.Ranges.Clear();
            _grouping.FixedCount = 10;

            if (_cbGroupField != null && _cbGroupField.SelectedItem != null)
                _grouping.GroupFieldId = _cbGroupField.SelectedItem.ToString();

            if (_tbFixedCount != null)
            {
                n = 10;
                if (int.TryParse(_tbFixedCount.Text.Trim(), out n))
                {
                    if (n <= 0) n = 10;
                    _grouping.FixedCount = n;
                }
            }

            if (_tbR1Min != null && _tbR1Max != null)
                AddRangeFromTextBox(_tbR1Min, _tbR1Max, "Group 1");

            if (_tbR2Min != null && _tbR2Max != null)
                AddRangeFromTextBox(_tbR2Min, _tbR2Max, "Group 2");

            if (_tbR3Min != null && _tbR3Max != null)
                AddRangeFromTextBox(_tbR3Min, _tbR3Max, "Group 3");
        }

        private void AddRangeFromTextBox(TextBox tbMin, TextBox tbMax, string groupName)
        {
            double min;
            double max;
            GroupRangeDef rg;

            min = 0;
            max = 0;

            if (!double.TryParse(tbMin.Text.Trim(), out min))
                return;

            if (!double.TryParse(tbMax.Text.Trim(), out max))
                return;

            rg = new GroupRangeDef();
            rg.Name = groupName + " (" + min.ToString("0.###") + "~" + max.ToString("0.###") + ")";
            rg.Min = min;
            rg.Max = max;

            _grouping.Ranges.Add(rg);
        }

        #endregion

        #region Metric area

        private void InitBottomRightMetricArea()
        {
            Grid root;
            StackPanel sp;
            TextBlock title;
            Label lb;

            root = grid_MetricHost;
            root.Children.Clear();

            sp = new StackPanel();

            title = new TextBlock();
            title.Text = "已加入 Metrics";
            title.Foreground = Brushes.White;
            title.FontSize = 24;
            title.FontWeight = FontWeights.Bold;
            title.Margin = new Thickness(0, 0, 0, 8);
            sp.Children.Add(title);

            _lbMetricPreview = new ListBox();
            _lbMetricPreview.Height = 220;
            _lbMetricPreview.FontSize = 18;
            _lbMetricPreview.Background = Brushes.Transparent;
            _lbMetricPreview.Foreground = Brushes.White;
            _lbMetricPreview.BorderThickness = new Thickness(0);
            _lbMetricPreview.SelectionChanged += lbMetricPreview_SelectionChanged;
            sp.Children.Add(_lbMetricPreview);

            lb = new Label();
            lb.Content = "Metric 作用欄位:";
            lb.Foreground = Brushes.White;
            lb.FontSize = 18;
            lb.Padding = new Thickness(2);
            sp.Children.Add(lb);

            _cbMetricTargetField = new ComboBox();
            _cbMetricTargetField.Height = 32;
            _cbMetricTargetField.FontSize = 16;
            _cbMetricTargetField.Background = Brushes.White;
            _cbMetricTargetField.Foreground = Brushes.Black;
            _cbMetricTargetField.SelectionChanged += cbMetricTargetField_SelectionChanged;
            sp.Children.Add(_cbMetricTargetField);

            _txtMetricInfo = new TextBlock();
            _txtMetricInfo.Foreground = Brushes.Orange;
            _txtMetricInfo.FontSize = 16;
            _txtMetricInfo.Margin = new Thickness(0, 8, 0, 0);
            _txtMetricInfo.TextWrapping = TextWrapping.Wrap;
            sp.Children.Add(_txtMetricInfo);

            root.Children.Add(sp);
        }

        private void RefreshMetricPreviewList()
        {
            int i;
            string text;

            if (_lbMetricPreview == null) return;

            _lbMetricPreview.Items.Clear();

            for (i = 0; i < _selectedMetrics.Count; i++)
            {
                text = _selectedMetrics[i].Name;

                if (_selectedMetrics[i].TargetFieldId != "")
                    text += "   ->   " + _selectedMetrics[i].TargetFieldId;

                _lbMetricPreview.Items.Add(text);
            }

            RefreshMetricTargetFieldCombo();

            if (_txtMetricInfo != null)
            {
                if (_selectedMetrics.Count == 0)
                    _txtMetricInfo.Text = "目前尚未加入 Metric。";
                else
                    _txtMetricInfo.Text = "已加入的 Metric 會直接模擬到群組標頭。";
            }
        }

        private void RefreshMetricTargetFieldCombo()
        {
            int i;
            GroupMetricDef m;

            if (_cbMetricTargetField == null) return;

            _cbMetricTargetField.Items.Clear();

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                if (CanFieldBeNumeric(_selectedColumns[i].FieldId))
                    _cbMetricTargetField.Items.Add(_selectedColumns[i].FieldId);
            }

            if (_lbMetricPreview == null) return;
            if (_lbMetricPreview.SelectedIndex < 0) return;
            if (_lbMetricPreview.SelectedIndex >= _selectedMetrics.Count) return;

            m = _selectedMetrics[_lbMetricPreview.SelectedIndex];

            if (m.TargetFieldId != "")
            {
                for (i = 0; i < _cbMetricTargetField.Items.Count; i++)
                {
                    if (_cbMetricTargetField.Items[i].ToString() == m.TargetFieldId)
                    {
                        _cbMetricTargetField.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                if (_cbMetricTargetField.Items.Count > 0)
                    _cbMetricTargetField.SelectedIndex = 0;
            }
        }

        private void lbMetricPreview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshMetricTargetFieldCombo();
        }

        private void cbMetricTargetField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GroupMetricDef m;

            if (_lbMetricPreview == null) return;
            if (_lbMetricPreview.SelectedIndex < 0) return;
            if (_lbMetricPreview.SelectedIndex >= _selectedMetrics.Count) return;
            if (_cbMetricTargetField.SelectedItem == null) return;

            m = _selectedMetrics[_lbMetricPreview.SelectedIndex];
            m.TargetFieldId = _cbMetricTargetField.SelectedItem.ToString();

            RefreshMetricPreviewList();
            _lbMetricPreview.SelectedIndex = Math.Min(_selectedMetrics.Count - 1, _lbMetricPreview.SelectedIndex);
            RebuildPreviewGrid();
        }

        #endregion

        #region Preview

        private void RebuildPreviewGrid()
        {
            ICollectionView view;

            ReadGroupingFromUi();

            txtPreviewTopHint.Text = "";

            if (_grouping.Enabled && !IsPreviewGroupModeSupported(_grouping.Mode))
            {
                txtPreviewTopHint.Text = "此分群模式目前在預覽區不提供預覽，實際套用時才會生效。";
            }

            dg_Preview.ItemsSource = null;
            dg_Preview.Columns.Clear();

            BuildPreviewTable();

            dg_Preview.ItemsSource = _previewTable.DefaultView;

            ApplyHeadersToGrid(dg_Preview);

            view = CollectionViewSource.GetDefaultView(dg_Preview.ItemsSource);
            if (view != null)
            {
                using (view.DeferRefresh())
                {
                    view.GroupDescriptions.Clear();
                    view.SortDescriptions.Clear();

                    if (ShouldUsePreviewGrouping())
                    {
                        view.SortDescriptions.Add(new SortDescription("__GroupDisplay", ListSortDirection.Ascending));
                        view.GroupDescriptions.Add(new PropertyGroupDescription("__GroupDisplay"));
                    }
                }
            }
        }

        private void BuildPreviewTable()
        {
            int i;
            int r;
            ColumnConfig c;
            DataRow row;
            bool doGroup;

            _previewTable.Clear();
            _previewTable.Columns.Clear();

            doGroup = ShouldUsePreviewGrouping();

            if (doGroup)
            {
                _previewTable.Columns.Add("__GroupKey", typeof(string));
                _previewTable.Columns.Add("__GroupDisplay", typeof(string));
                _previewTable.Columns.Add("__RowNo", typeof(int));
            }

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                c = _selectedColumns[i];

                if (c.UIType == ColumnFieldUIType.CheckBox)
                    _previewTable.Columns.Add(c.FieldId, typeof(bool));
                else
                    _previewTable.Columns.Add(c.FieldId, typeof(string));
            }

            for (r = 0; r < 30; r++)
            {
                row = _previewTable.NewRow();

                if (doGroup)
                {
                    row["__GroupKey"] = GetPreviewGroupName(r);
                    row["__RowNo"] = r + 1;
                    row["__GroupDisplay"] = "";
                }

                for (i = 0; i < _selectedColumns.Count; i++)
                {
                    c = _selectedColumns[i];
                    row[c.FieldId] = MakeSampleValue(c, r);
                }

                _previewTable.Rows.Add(row);
            }

            if (doGroup)
            {
                FillPreviewGroupDisplayTexts();
            }
        }

        private void FillPreviewGroupDisplayTexts()
        {
            Dictionary<string, List<DataRow>> groups = new Dictionary<string, List<DataRow>>();
            string key;
            int i;
            string displayText;

            for (i = 0; i < _previewTable.Rows.Count; i++)
            {
                key = _previewTable.Rows[i]["__GroupKey"].ToString();

                if (!groups.ContainsKey(key))
                    groups[key] = new List<DataRow>();

                groups[key].Add(_previewTable.Rows[i]);
            }

            foreach (KeyValuePair<string, List<DataRow>> kv in groups)
            {
                displayText = BuildGroupDisplayText(kv.Key, kv.Value);

                for (i = 0; i < kv.Value.Count; i++)
                {
                    kv.Value[i]["__GroupDisplay"] = displayText;
                }
            }
        }

        private string BuildGroupDisplayText(string groupKey, List<DataRow> rows)
        {
            string text;
            int i;
            string metricText;

            text = groupKey;

            if (_selectedMetrics.Count == 0)
            {
                text += "   |   Count = " + rows.Count.ToString();
                return text;
            }

            for (i = 0; i < _selectedMetrics.Count; i++)
            {
                metricText = BuildMetricText(_selectedMetrics[i], rows);

                if (metricText != "")
                    text += "   |   " + metricText;
            }

            return text;
        }

        private string BuildMetricText(GroupMetricDef metric, List<DataRow> rows)
        {
            string fieldId;
            string header;
            double avgValue;
            double maxValue;
            int minNo;
            int maxNo;

            if (metric == null) return "";

            if (metric.Id == "Count")
                return "Count = " + rows.Count.ToString();

            if (metric.Id == "index_range")
            {
                minNo = GetMinRowNo(rows);
                maxNo = GetMaxRowNo(rows);
                return "Range = " + minNo.ToString() + "~" + maxNo.ToString();
            }

            fieldId = metric.TargetFieldId;
            if (fieldId == "")
                fieldId = GetDefaultMetricTargetFieldId();

            header = GetColumnHeaderByFieldId(fieldId);

            if (metric.Id == "Avg")
            {
                if (!TryGetAverage(rows, fieldId, out avgValue))
                    return "Avg(" + header + ") = N/A";

                return "Avg(" + header + ") = " + avgValue.ToString("0.###");
            }

            if (metric.Id == "Max")
            {
                if (!TryGetMax(rows, fieldId, out maxValue))
                    return "Max(" + header + ") = N/A";

                return "Max(" + header + ") = " + maxValue.ToString("0.###");
            }

            return metric.Name;
        }

        private bool TryGetAverage(List<DataRow> rows, string fieldId, out double avgValue)
        {
            int i;
            int count;
            double sum;
            double v;

            avgValue = 0;
            sum = 0;
            count = 0;

            if (fieldId == "") return false;

            for (i = 0; i < rows.Count; i++)
            {
                if (TryGetRowDouble(rows[i], fieldId, out v))
                {
                    sum += v;
                    count++;
                }
            }

            if (count <= 0) return false;

            avgValue = sum / count;
            return true;
        }

        private bool TryGetMax(List<DataRow> rows, string fieldId, out double maxValue)
        {
            int i;
            bool hasValue;
            double v;

            maxValue = 0;
            hasValue = false;

            if (fieldId == "") return false;

            for (i = 0; i < rows.Count; i++)
            {
                if (TryGetRowDouble(rows[i], fieldId, out v))
                {
                    if (!hasValue)
                    {
                        maxValue = v;
                        hasValue = true;
                    }
                    else if (v > maxValue)
                    {
                        maxValue = v;
                    }
                }
            }

            return hasValue;
        }

        private bool TryGetRowDouble(DataRow row, string fieldId, out double value)
        {
            string raw;

            value = 0;

            if (row == null) return false;
            if (!_previewTable.Columns.Contains(fieldId)) return false;

            raw = row[fieldId].ToString();
            return double.TryParse(raw, out value);
        }

        private int GetMinRowNo(List<DataRow> rows)
        {
            int i;
            int minValue;
            int v;

            if (rows.Count == 0) return 0;

            minValue = Convert.ToInt32(rows[0]["__RowNo"]);

            for (i = 1; i < rows.Count; i++)
            {
                v = Convert.ToInt32(rows[i]["__RowNo"]);
                if (v < minValue)
                    minValue = v;
            }

            return minValue;
        }

        private int GetMaxRowNo(List<DataRow> rows)
        {
            int i;
            int maxValue;
            int v;

            if (rows.Count == 0) return 0;

            maxValue = Convert.ToInt32(rows[0]["__RowNo"]);

            for (i = 1; i < rows.Count; i++)
            {
                v = Convert.ToInt32(rows[i]["__RowNo"]);
                if (v > maxValue)
                    maxValue = v;
            }

            return maxValue;
        }

        private void ApplyHeadersToGrid(DataGrid dg)
        {
            int i;
            int k;
            string fieldId;

            for (i = 0; i < dg.Columns.Count; i++)
            {
                fieldId = dg.Columns[i].SortMemberPath;

                if (fieldId == "__GroupKey") continue;
                if (fieldId == "__GroupDisplay") continue;
                if (fieldId == "__RowNo") continue;

                for (k = 0; k < _selectedColumns.Count; k++)
                {
                    if (_selectedColumns[k].FieldId == fieldId)
                    {
                        dg.Columns[i].Header = _selectedColumns[k].Header;
                        break;
                    }
                }
            }
        }

        private void dgPreview_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            int i;
            string fieldId;

            fieldId = e.PropertyName;

            if (fieldId == "__GroupKey")
            {
                e.Cancel = true;
                return;
            }

            if (fieldId == "__GroupDisplay")
            {
                e.Cancel = true;
                return;
            }

            if (fieldId == "__RowNo")
            {
                e.Cancel = true;
                return;
            }

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                if (_selectedColumns[i].FieldId == fieldId)
                {
                    e.Column.Header = _selectedColumns[i].Header;
                    break;
                }
            }
        }

        private bool ShouldUsePreviewGrouping()
        {
            if (!_grouping.Enabled)
                return false;

            if (!IsPreviewGroupModeSupported(_grouping.Mode))
                return false;

            if (_grouping.Mode == GroupMode.ByFixedCount)
                return true;

            if (_grouping.GroupFieldId == "")
                return false;

            return true;
        }

        private bool IsPreviewGroupModeSupported(GroupMode mode)
        {
            if (mode == GroupMode.ByRange) return true;
            if (mode == GroupMode.ByFixedCount) return true;
            if (mode == GroupMode.BySameValue) return true;
            return false;
        }

        private string GetPreviewGroupName(int rowIndex)
        {
            if (_grouping.Mode == GroupMode.ByFixedCount)
                return GetPreviewGroupName_ByFixedCount(rowIndex);

            if (_grouping.Mode == GroupMode.BySameValue)
                return GetPreviewGroupName_BySameValue(rowIndex);

            if (_grouping.Mode == GroupMode.ByRange)
                return GetPreviewGroupName_ByRange(rowIndex);

            return "Ungrouped";
        }

        private string GetPreviewGroupName_ByFixedCount(int rowIndex)
        {
            int count;
            int g;
            int start;
            int end;

            count = _grouping.FixedCount;
            if (count <= 0) count = 10;

            g = (rowIndex / count) + 1;
            start = ((g - 1) * count) + 1;
            end = g * count;

            if (end > 30)
                end = 30;

            return "Group " + g.ToString() + " (" + start.ToString() + "~" + end.ToString() + ")";
        }

        private string GetPreviewGroupName_BySameValue(int rowIndex)
        {
            string raw;

            raw = GetFieldValueForGrouping(rowIndex);
            if (raw == "") return "Others";

            return raw;
        }

        private string GetPreviewGroupName_ByRange(int rowIndex)
        {
            string raw;
            double v;
            int i;
            GroupRangeDef rg;

            raw = GetFieldValueForGrouping(rowIndex);

            if (!double.TryParse(raw, out v))
                return "Others";

            for (i = 0; i < _grouping.Ranges.Count; i++)
            {
                rg = _grouping.Ranges[i];
                if (v >= rg.Min && v <= rg.Max)
                    return rg.Name;
            }

            return "Others";
        }

        private string GetFieldValueForGrouping(int rowIndex)
        {
            ColumnConfig fake;
            object v;

            if (_grouping.GroupFieldId == "")
                return "";

            fake = new ColumnConfig();
            fake.FieldId = _grouping.GroupFieldId;
            fake.UIType = ColumnFieldUIType.Text;

            v = MakeSampleValue(fake, rowIndex);
            if (v == null) return "";

            return v.ToString();
        }

        private object MakeSampleValue(ColumnConfig c, int rowIndex)
        {
            if (c.UIType == ColumnFieldUIType.CheckBox)
                return rowIndex % 3 == 0;

            if (c.UIType == ColumnFieldUIType.RadioButton)
                return rowIndex == 0 ? "●" : "○";

            if (c.UIType == ColumnFieldUIType.Button)
                return "[Button]";

            if (c.UIType == ColumnFieldUIType.OkNgLabel)
                return rowIndex % 3 == 0 ? "[OK]" : "[NG]";

            if (c.UIType == ColumnFieldUIType.Color)
            {
                if (rowIndex % 3 == 0) return "Red";
                if (rowIndex % 3 == 1) return "Green";
                return "Blue";
            }

            // 若來自主表，這裡做一個通用展示
            if (_sourceTable != null && _sourceTable.Columns.Contains(c.FieldId))
            {
                Type t = _sourceTable.Columns[c.FieldId].DataType;

                if (t == typeof(int) || t == typeof(long) || t == typeof(short))
                    return (rowIndex + 1).ToString();

                if (t == typeof(double) || t == typeof(float) || t == typeof(decimal))
                    return (10.0 + rowIndex * 1.23).ToString("0.###");

                if (t == typeof(bool))
                    return rowIndex % 2 == 0;

                return c.FieldId + "_" + rowIndex.ToString();
            }

            // 舊示範欄位
            if (string.Equals(c.FieldId, "idx", StringComparison.OrdinalIgnoreCase) || string.Equals(c.FieldId, "Index", StringComparison.OrdinalIgnoreCase)) return rowIndex.ToString();
            if (c.FieldId == "name") return "Test " + rowIndex;
            if (c.FieldId == "time") return (rowIndex * 0.1).ToString("0.000");
            if (string.Equals(c.FieldId, "force", StringComparison.OrdinalIgnoreCase) || string.Equals(c.FieldId, "Force", StringComparison.OrdinalIgnoreCase)) return (10.0 + rowIndex * 1.23).ToString("0.00");
            if (string.Equals(c.FieldId, "disp", StringComparison.OrdinalIgnoreCase) || string.Equals(c.FieldId, "Disp", StringComparison.OrdinalIgnoreCase)) return (50.0 + rowIndex * 0.12).ToString("0.00");
            if (c.FieldId == "vel") return (0.10 + rowIndex * 0.01).ToString("0.000");

            return "";
        }

        #endregion

        #region Buttons

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            ResultConfig = BuildResultConfig();
            DialogResult = true;
            Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private DataGridLayoutConfig BuildResultConfig()
        {
            DataGridLayoutConfig cfg;
            int i;

            ReadGroupingFromUi();

            cfg = new DataGridLayoutConfig();

            if (cfg.SelectedColumns == null)
                cfg.SelectedColumns = new List<ColumnConfig>();

            if (cfg.SelectedMetrics == null)
                cfg.SelectedMetrics = new List<GroupMetricDef>();

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                cfg.SelectedColumns.Add(CloneColumnConfig(_selectedColumns[i]));
            }

            cfg.Grouping = CloneGroupConfig(_grouping);

            for (i = 0; i < _selectedMetrics.Count; i++)
            {
                cfg.SelectedMetrics.Add(CloneGroupMetric(_selectedMetrics[i]));
            }

            return cfg;
        }

        #endregion

        #region Clone / helpers

        private ColumnConfig CloneColumnConfig(ColumnConfig src)
        {
            ColumnConfig c = new ColumnConfig();
            if (src == null) return c;

            c.FieldId = src.FieldId;
            c.Header = src.Header;
            c.Format = src.Format;
            c.OkNgRule = src.OkNgRule;
            c.UIType = src.UIType;

            return c;
        }

        private GroupMetricDef CloneGroupMetric(GroupMetricDef src)
        {
            GroupMetricDef m = new GroupMetricDef();
            if (src == null) return m;

            m.Id = src.Id;
            m.Name = src.Name;
            m.TargetFieldId = src.TargetFieldId;

            return m;
        }

        private GroupConfig CloneGroupConfig(GroupConfig src)
        {
            GroupConfig g = new GroupConfig();
            GroupRangeDef r;
            GroupRangeDef nr;
            int i;

            if (src == null) return g;

            g.Enabled = src.Enabled;
            g.Mode = src.Mode;
            g.GroupFieldId = src.GroupFieldId;
            g.FixedCount = src.FixedCount;

            if (src.Ranges != null)
            {
                for (i = 0; i < src.Ranges.Count; i++)
                {
                    r = src.Ranges[i];
                    nr = new GroupRangeDef();
                    nr.Name = r.Name;
                    nr.Min = r.Min;
                    nr.Max = r.Max;
                    g.Ranges.Add(nr);
                }
            }

            return g;
        }

        private string GetDefaultMetricTargetFieldId()
        {
            int i;

            if (_grouping != null)
            {
                if (CanFieldBeNumeric(_grouping.GroupFieldId))
                    return _grouping.GroupFieldId;
            }

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                if (CanFieldBeNumeric(_selectedColumns[i].FieldId))
                    return _selectedColumns[i].FieldId;
            }

            return "";
        }

        private bool CanFieldBeNumeric(string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
                return false;

            if (ColumnRegistry.IsNumericField(fieldId))
                return true;

            // 如果主表存在，就直接用實際欄位型別判斷
            if (_sourceTable != null && _sourceTable.Columns.Contains(fieldId))
            {
                Type t = _sourceTable.Columns[fieldId].DataType;

                if (t == typeof(byte) ||
                    t == typeof(short) ||
                    t == typeof(int) ||
                    t == typeof(long) ||
                    t == typeof(float) ||
                    t == typeof(double) ||
                    t == typeof(decimal))
                    return true;
            }

            // 舊示範欄位保留相容
            if (string.Equals(fieldId, "idx", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(fieldId, "time", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(fieldId, "force", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(fieldId, "disp", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(fieldId, "vel", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private string GetColumnHeaderByFieldId(string fieldId)
        {
            int i;
            GridColumnDefinition reg;

            for (i = 0; i < _selectedColumns.Count; i++)
            {
                if (string.Equals(_selectedColumns[i].FieldId, fieldId, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(_selectedColumns[i].Header))
                        return _selectedColumns[i].Header;

                    break;
                }
            }

            reg = ColumnRegistry.Get(fieldId);
            if (reg != null && !string.IsNullOrWhiteSpace(reg.Header))
                return reg.Header;

            return fieldId;
        }

        #endregion

        #region Column reorder

        private void dg_Preview_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            ApplyDisplayOrderToSelectedColumns();
        }

        private void ApplyDisplayOrderToSelectedColumns()
        {
            List<DataGridColumn> cols;
            List<ColumnConfig> newList;
            string fieldId;
            int i;
            int k;

            cols = dg_Preview.Columns.OrderBy(c => c.DisplayIndex).ToList();
            newList = new List<ColumnConfig>();

            for (i = 0; i < cols.Count; i++)
            {
                fieldId = cols[i].SortMemberPath;

                if (fieldId == "__GroupKey") continue;
                if (fieldId == "__GroupDisplay") continue;
                if (fieldId == "__RowNo") continue;

                for (k = 0; k < _selectedColumns.Count; k++)
                {
                    if (_selectedColumns[k].FieldId == fieldId)
                    {
                        newList.Add(_selectedColumns[k]);
                        break;
                    }
                }
            }

            if (newList.Count == _selectedColumns.Count)
                _selectedColumns = newList;
        }

        #endregion
    }
}
