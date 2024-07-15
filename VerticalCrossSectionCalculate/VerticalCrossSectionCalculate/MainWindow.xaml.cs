using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace VerticalCrossSectionCalculate
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private SectionCalculate secCal;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ReadingDataButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opfdl = new OpenFileDialog()
            {
                Title = "读取数据文件",
                Multiselect = false,
                RestoreDirectory = true,
                Filter = "文本文档(*.txt)|*.txt"
            };
            if (opfdl.ShowDialog() == true)
            {
                secCal = new SectionCalculate(opfdl.FileName);
                dataTextBox.Text = "点名     \tX,                    \tY,                    \tH\n";
                IEnumerable<string> ptStr = from p in secCal.Pts.Values select $"{p.Name},\t{p.X:F3},\t{p.Y:F3},\t{p.H:F3}";
                dataTextBox.Text += string.Join("\n", ptStr);
                tab.SelectedIndex = 0;
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> results = secCal.CalculateResult();
            resultTextBox.Text = string.Join("\n", results);
            tab.SelectedIndex = 1;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfdl = new SaveFileDialog()
            {
                Title = "读取数据文件",
                RestoreDirectory = true,
                Filter = "文本文档(*.txt)|*.txt",
            };
            if (sfdl.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(sfdl.FileName, resultTextBox.Text);
                    MessageBox.Show("保存成功！");
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("保存失败：" + ex.Message);
                }
            }
        }
    }
}
