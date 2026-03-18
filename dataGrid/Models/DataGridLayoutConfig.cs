using System;
using System.Collections.Generic;
using System.Text;

namespace NEW_QCtech.dataGrid.Models
{
   public class DataGridLayoutConfig
    {
        public List<ColumnConfig> SelectedColumns = new List<ColumnConfig>();
        public GroupConfig Grouping = new GroupConfig();
        public List<GroupMetricDef> SelectedMetrics = new List<GroupMetricDef>();
    }
}
