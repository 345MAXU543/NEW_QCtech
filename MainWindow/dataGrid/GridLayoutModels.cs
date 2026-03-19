using System.Collections.Generic;

namespace NEW_QCtech
{
    public enum ColumnFieldUIType
    {
        Text = 0,
        CheckBox = 1,
        RadioButton = 2,
        Button = 3,
        OkNgLabel = 4,
        Color = 5
    }

    public enum GroupMode
    {
        None = 0,
        ByRange = 1,
        ByFixedCount = 2,
        BySameValue = 3,
        CustomFormula = 10,
        MultiCondition = 11,
        Nested = 12
    }

    public class ColumnConfig
    {
        public string FieldId = "";
        public string Header = "";
        public string Format = "";
        public string OkNgRule = "";
        public ColumnFieldUIType UIType = ColumnFieldUIType.Text;
    }

    public class DataGridLayoutConfig
    {
        public List<ColumnConfig> SelectedColumns = new List<ColumnConfig>();
        public GroupConfig Grouping = new GroupConfig();
        public List<GroupMetricDef> SelectedMetrics = new List<GroupMetricDef>();
    }

    public class GroupConfig
    {
        public bool Enabled = false;
        public GroupMode Mode = GroupMode.None;
        public string GroupFieldId = "";
        public List<GroupRangeDef> Ranges = new List<GroupRangeDef>();
        public int FixedCount = 10;
    }

    public class GroupMetricDef
    {
        public string Id = "";
        public string Name = "";
        public string TargetFieldId = "";
    }

    public class GroupRangeDef
    {
        public string Name = "";
        public double Min = 0;
        public double Max = 0;
    }
}
