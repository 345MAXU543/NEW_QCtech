using System;
using System.Collections.Generic;
using System.Text;

namespace NEW_QCtech.dataGrid.Models
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

        // 預覽支援
        ByRange = 1,
        ByFixedCount = 2,
        BySameValue = 3,

        // 目前不預覽
        CustomFormula = 10,
        MultiCondition = 11,
        Nested = 12
    }
}
