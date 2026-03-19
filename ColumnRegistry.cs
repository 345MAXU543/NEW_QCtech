using System;
using System.Collections.Generic;
using System.Linq;
using NEW_QCtech.dataGrid.Models;

namespace NEW_QCtech.dataGrid
{
    public class GridColumnDefinition
    {
        public string FieldId = "";
        public string Header = "";
        public string PathInTree = "";
        public string Format = "";
        public string Unit = "";
        public string GroupName = "";
        public ColumnFieldUIType UIType = ColumnFieldUIType.Text;
        public bool IsNumeric = false;
        public bool DefaultVisible = true;
        public double DefaultWidth = 100;
    }

    public static class ColumnRegistry
    {
        private static readonly List<GridColumnDefinition> _all = new List<GridColumnDefinition>
        {
            new GridColumnDefinition
            {
                FieldId = "Main",
                Header = "Main",
                PathInTree = "Selection/Main",
                UIType = ColumnFieldUIType.RadioButton,
                IsNumeric = false,
                DefaultWidth = 60
            },

            new GridColumnDefinition
            {
                FieldId = "dick",
                Header = "Force_TEST",
                PathInTree = "Selection/Main",
                UIType = ColumnFieldUIType.RadioButton,
                IsNumeric = false,
                DefaultWidth = 60
            },
            new GridColumnDefinition
            {
                FieldId = "Sub",
                Header = "Sub",
                PathInTree = "Selection/Sub",
                UIType = ColumnFieldUIType.CheckBox,
                IsNumeric = false,
                DefaultWidth = 60
            },
            new GridColumnDefinition
            {
                FieldId = "ColorBrush",
                Header = "Color",
                PathInTree = "Display/Color",
                UIType = ColumnFieldUIType.Color,
                IsNumeric = false,
                DefaultWidth = 60
            },
            new GridColumnDefinition
            {
                FieldId = "Index",
                Header = "Index",
                PathInTree = "Basic/Index",
                UIType = ColumnFieldUIType.Text,
                IsNumeric = true,
                Format = "",
                DefaultWidth = 80
            },
            new GridColumnDefinition
            {
                FieldId = "Name",
                Header = "Name",
                PathInTree = "Basic/Name",
                UIType = ColumnFieldUIType.Text,
                IsNumeric = false,
                Format = "",
                DefaultWidth = 30
            },
            new GridColumnDefinition
            {
                FieldId = "Force",
                Header = "?????",
                PathInTree = "Measure/Force",
                UIType = ColumnFieldUIType.Text,
                IsNumeric = true,
                Format = "{0:0.###}",
                Unit = "",
                GroupName = "Measure",
                DefaultWidth = 30
            },
            new GridColumnDefinition
            {
                FieldId = "Disp",
                Header = "Disp",
                PathInTree = "Measure/Disp",
                UIType = ColumnFieldUIType.Text,
                IsNumeric = true,
                Format = "{0:0.###}",
                Unit = "",
                GroupName = "Measure",
                DefaultWidth = 120
            },
        };

        public static List<GridColumnDefinition> GetAll()
        {
            return _all;
        }

        public static GridColumnDefinition Get(string fieldId)
        {
            return _all.FirstOrDefault(x =>
                string.Equals(x.FieldId, fieldId, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsNumericField(string fieldId)
        {
            GridColumnDefinition def = Get(fieldId);
            return def != null && def.IsNumeric;
        }

        public static string GetHeader(string fieldId)
        {
            GridColumnDefinition def = Get(fieldId);
            return def != null ? def.Header : fieldId;
        }
    }
}