using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SuperControls.Style;
using SuperControls.Style.Windows;

namespace SuperCom
{
    /// <summary>
    /// 频段开关解析器 - 根据十进制数据序列计算所有开启的频段编号
    /// </summary>
    public partial class Window_BandCalc : BaseWindow
    {
        public Window_BandCalc()
        {
            InitializeComponent();
        }

        private void BtnCalc_Click(object sender, RoutedEventArgs e)
        {
            string input = TxtInput.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                TxtResult.Text = "";
                TxtStats.Text = "";
                DgDetail.ItemsSource = null;
                BtnCopy.IsEnabled = false;
                return;
            }

            // 解析输入
            string[] parts = input.Split(new char[] { ',', '，', ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> values = new List<int>();
            List<string> errors = new List<string>();

            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int v))
                {
                    if (v >= 0 && v <= 255)
                        values.Add(v);
                    else
                        errors.Add($"{part} (超出0~255范围)");
                }
                else
                {
                    errors.Add(part);
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show($"以下输入无法解析或超出范围：\n{string.Join("\n", errors)}", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (values.Count == 0)
            {
                TxtResult.Text = "";
                TxtStats.Text = "无有效数据";
                DgDetail.ItemsSource = null;
                BtnCopy.IsEnabled = false;
                return;
            }

            // 计算频段
            List<int> allBands = new List<int>();
            List<BandDetailRow> details = new List<BandDetailRow>();

            for (int i = 0; i < values.Count; i++)
            {
                byte val = (byte)values[i];
                string binary = Convert.ToString(val, 2).PadLeft(8, '0');
                List<int> onBits = new List<int>();
                List<int> bands = new List<int>();

                for (int b = 0; b < 8; b++)
                {
                    if ((val & (1 << b)) != 0)
                    {
                        onBits.Add(b);
                        int bandNum = i * 8 + b + 1;
                        bands.Add(bandNum);
                        allBands.Add(bandNum);
                    }
                }

                details.Add(new BandDetailRow
                {
                    Index = i + 1,
                    Value = val,
                    Binary = binary,
                    OnBits = onBits.Count > 0 ? string.Join(", ", onBits) : "无",
                    Bands = bands.Count > 0 ? string.Join(", ", bands) : "无"
                });
            }

            // 输出结果（已按升序排列，因为遍历顺序就是升序）
            TxtResult.Text = string.Join(", ", allBands);
            TxtStats.Text = $"共 {values.Count} 个字节，{values.Count * 8} 个频段位，其中 {allBands.Count} 个开启";
            DgDetail.ItemsSource = details;
            BtnCopy.IsEnabled = allBands.Count > 0;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtInput.Text = "";
            TxtResult.Text = "";
            TxtStats.Text = "";
            DgDetail.ItemsSource = null;
            BtnCopy.IsEnabled = false;
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtResult.Text))
            {
                Clipboard.SetText(TxtResult.Text);
                MessageCard.Success("已复制到剪贴板");
            }
        }
    }

    /// <summary>
    /// 位解析详情行数据
    /// </summary>
    public class BandDetailRow
    {
        public int Index { get; set; }
        public int Value { get; set; }
        public string Binary { get; set; }
        public string OnBits { get; set; }
        public string Bands { get; set; }
    }
}
