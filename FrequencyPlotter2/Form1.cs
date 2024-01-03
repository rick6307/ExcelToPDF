using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;


namespace FrequencyPlotter2
{
    public partial class Form1 : Form
    {
        private const int PADDING = 50; // 圖形留白大小
        private const int AXIS_WIDTH = 2; // 軸線寬度
        private const int TICK_LENGTH = 5; // 刻度線長度
        private const int LABEL_MARGIN = 5; // 刻度標籤與軸線的距離
        private const int LABEL_FONT_SIZE = 10; // 刻度標籤字型大小

        private string connectionString = "Ddatabase=VM05076;server=192.168.10.129;uid=VM05076;pwd=$in0t3ch;Pooling=True";
        private List<double> timeValues = new List<double>();
        private List<double> freqValues = new List<double>();
        private object picBox;

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load_1(object sender, EventArgs e)
        {
            // 從 SQL Server 讀取資料
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT TimeValue, FreqValue FROM dbo.BuildingFreq";
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    timeValues.Add(Convert.ToDouble(reader["TimeValue"]));
                    freqValues.Add(Convert.ToDouble(reader["FreqValue"]));
                }
                reader.Close();
            }
        }
        private void btnDraw_Click(object sender, EventArgs e)
        {
            // 創建圖像並設置畫布大小
            Bitmap bmp = new Bitmap(picBox.Width, picBox.Height);
            Graphics g = Graphics.FromImage(bmp);

            // 設置畫筆
            Pen axisPen = new Pen(Color.Black, AXIS_WIDTH);
            Pen gridPen = new Pen(Color.LightGray, 1);
            Font labelFont = new Font("Arial", LABEL_FONT_SIZE);

            // 計算座標系統的範圍和刻度值
            double minX = timeValues[0];
            double maxX = timeValues[timeValues.Count - 1];
            double minY = freqValues[0];
            double maxY = freqValues[freqValues.Count - 1];
            double xRange = maxX - minX;
            double yRange = maxY - minY;
            double xStep = GetStep(xRange);
            double yStep = GetStep(yRange);

            // 繪製座標軸
            g.DrawLine(axisPen, PADDING, picBox.Height - PADDING, PADDING, PADDING);
            g.DrawLine(axisPen, PADDING, picBox.Height - PADDING, picBox.Width - PADDING, picBox.Height - PADDING);

            // 繪製水平方向的刻度和標籤
            for (double x = minX; x <= maxX; x +=1)
            {
                // 計算刻度線的位置和標籤
                int tickX = Convert.ToInt32((x - minX) / xRange * (picBox.Width - 2 * PADDING) + PADDING);
                int tickY1 = picBox.Height - PADDING;
                int tickY2 = picBox.Height - PADDING - TICK_LENGTH;
                string label = x.ToString();

                // 繪製刻度線和標籤
                g.DrawLine(axisPen, tickX, tickY1, tickX, tickY2);
                SizeF labelSize = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, Brushes.Black, tickX - labelSize.Width / 2, tickY2 - LABEL_MARGIN - labelSize.Height);
            }
            // 繪製垂直方向的刻度和標籤
            for (double y = minY; y <= maxY; y += yStep)
            {
                // 計算刻度線的位置和標籤
                int tickX1 = PADDING;
                int tickX2 = PADDING + TICK_LENGTH;
                int tickY = picBox.Height - Convert.ToInt32((y - minY) / yRange * (picBox.Height - 2 * PADDING) + PADDING);
                string label = y.ToString();

                // 繪製刻度線和標籤
                g.DrawLine(axisPen, tickX1, tickY, tickX2, tickY);
                SizeF labelSize = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, Brushes.Black, tickX2 + LABEL_MARGIN, tickY - labelSize.Height / 2);
            }
            // 繪製頻率圖形
            for (int i = 0; i < timeValues.Count - 1; i++)
            {
                double x1 = timeValues[i];
                double x2 = timeValues[i + 1];
                double y1 = freqValues[i];
                double y2 = freqValues[i + 1];

                int px1 = Convert.ToInt32((x1 - minX) / xRange * (picBox.Width - 2 * PADDING) + PADDING);
                int px2 = Convert.ToInt32((x2 - minX) / xRange * (picBox.Width - 2 * PADDING) + PADDING);
                int py1 = picBox.Height - Convert.ToInt32((y1 - minY) / yRange * (picBox.Height - 2 * PADDING) + PADDING);
                int py2 = picBox.Height - Convert.ToInt32((y2 - minY) / yRange * (picBox.Height - 2 * PADDING) + PADDING);

                g.DrawLine(gridPen, px1, py1, px2, py2);
            }
            // 儲存圖像為 PNG 檔案
            bmp.Save("output.png", ImageFormat.Png);

            // 顯示圖像
            picBox.Image = bmp;
        }
        private double GetStep(double range)
        {
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range)) - 1); // 計算數值範圍的量級
            double fraction = range / magnitude; // 計算數值範圍相對於量級的比例

            double step; // 儲存計算出來的步進值

            // 根據比例的大小，決定步進值
            if (fraction <= 1.0)
            {
                step = magnitude / 2;
            }
            else if (fraction <= 2)
            {
                step = magnitude;
            }
            else if (fraction <= 5)
            {
                step = 2 * magnitude;
            }
            else
            {
                step = 5 * magnitude;
            }

            return step;
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
