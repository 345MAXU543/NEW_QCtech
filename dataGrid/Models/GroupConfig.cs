using System;
using System.Collections.Generic;
using System.Text;

namespace NEW_QCtech.dataGrid.Models
{
   public class GroupConfig
    {
        public bool Enabled = false;
        public GroupMode Mode = GroupMode.None;
        public string GroupFieldId = "";
        public List<GroupRangeDef> Ranges = new List<GroupRangeDef>();
        public int FixedCount = 10;
    }
}
