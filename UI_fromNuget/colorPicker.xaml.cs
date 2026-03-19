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
    /// colorPicker.xaml 的互動邏輯
    /// </summary>
    public partial class colorPicker : Window
    {
        public Color SelectedColor { get; private set; }
        public colorPicker(Color oldColor)
        {
            InitializeComponent();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = picker.SelectedColor;
            DialogResult = true;
        }
    }
}
