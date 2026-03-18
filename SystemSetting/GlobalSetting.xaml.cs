using NEW_QCtech.SystemSetting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NEW_QCtech
{
    /// <summary>
    /// GlobalSetting.xaml 的互動邏輯
    /// </summary>
    public partial class GlobalSetting : Window
    {
        public GlobalSetting(string? pageName)
        {
            InitializeComponent();
        }

      

        private void LeftSideTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Frame_context.Content = null;
            Frame_context.Source = null;
            TreeViewItem selectedItem = LeftSideTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                string name = selectedItem.Name.ToString();
                switch (name)
                {
                    case "CardPanel":
                        Frame_context.Content = new Page_CardPanelSetting();
                        break;

                }
            }
        }
    }
}
