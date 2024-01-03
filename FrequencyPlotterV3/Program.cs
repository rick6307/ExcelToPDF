using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using ScottPlot;
using System.Drawing;
using System.IO;
namespace FrequencyPlotterV3
{
    class Program
    {
        static void Main(string[] args)
        {

            string logDir = @"C:\SHM\FrequencyPlotterV3\Log";
            string debugLogFileName = $"{logDir}\\FrequencyPlotter{DateTime.Now.ToString("yyyyMMddhhmm")}.log";
            if (!ReadInputFile(@"C:\SHM\FrequencyPlotterV3\config.txt", out int ID_Building, out double maxFreq, out double minFreq, out double maxFreq_safe, out double minFreq_safe, out string outputPath, debugLogFileName))
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 開啟設定檔失敗");
            }


            // 建立 SQL Server 連線字串
            string connectionString = "database=VM05076;server=192.168.10.129;uid=VM05076;pwd=$in0t3ch;Pooling=True";
            // 創建一個 PlotModel 物件

            FreqData freqdata = new FreqData
            {
                Freq = new List<double>(),
                Time = new List<DateTime>()
            };

            // 讀取資料
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // 建立 SQL 查詢字串 查詢當前時間的過去兩周資料
                string query = "SELECT Time,Freq FROM dbo.BuildingFreq WHERE ID_Building =" +ID_Building.ToString()+" AND Time >= DATEADD(WEEK, -2, GETDATE());";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                // 建立頻率資料陣列
                
                
                while (reader.Read())
                {
                    // 讀取資料欄位
                    freqdata.Freq.Add(Convert.ToDouble(reader["Freq"]));
                    freqdata.Time.Add(Convert.ToDateTime(reader["Time"]));
                }
                // 關閉資料庫連線
                reader.Close();
                connection.Close();
            }
            // use LINQ and DateTime.ToOADate() to convert DateTime[] to double[]
            double[] xs = freqdata.Time.Select(x => x.ToOADate()).ToArray();
            
            // 繪製頻率圖形
            var plt = new Plot(1024, 768);
            plt.AddScatterPoints(xs, freqdata.Freq.ToArray());
            plt.XAxis.DateTimeFormat(true);
            //plt.PlotScatter(freqdata.time, freqdata.freq, lineWidth: 0, markerSize: 2);
            plt.Title("富邦東京大樓頻率監測");
            plt.YLabel("Frequency (Hz)");
            plt.XLabel("Date");

            // add axis spans
            Color Greencolor = Color.FromArgb(70, 173, 255, 47);
            Color Redcolor = Color.FromArgb(70, 255, 47, 47);
            plt.AddVerticalSpan(minFreq_safe, maxFreq_safe, Greencolor, label:"安全範圍");
            //plt.AddVerticalSpan(minFreq, minFreq_safe, Redcolor, label: "異常範圍");
            //plt.AddVerticalSpan(maxFreq_safe, maxFreq, Redcolor);
            plt.SetAxisLimitsY(0, 1.5);

            plt.Grid(true);
            plt.Legend();
            string filepath = outputPath +@"\"+ freqdata.Time.Last().ToString("yyyyMMddHHmmss");
            Directory.CreateDirectory(filepath);
            plt.SaveFig(filepath+ @"\frequencyChart.png");
        }
        public class FreqData
        {
            public List<double> Freq { get; set; }
            public List<DateTime> Time { get; set; }
        }
        public static bool ReadInputFile(string filePath, out int ID_Building,
                                        out double maxFreq, out double minFreq,
                                        out double maxFreq_safe, out double minFreq_safe,
                                        out string outputPath, string debugLogFileName)
        {
            ID_Building = 0;
            maxFreq = 0;
            minFreq = 0;
            maxFreq_safe = 0;
            minFreq_safe = 0;
            outputPath = "";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // 將每一行的名稱和值分開
                        string[] parts = line.Split('=');
                        if (parts.Length != 2)
                        {
                            throw new Exception("Invalid input file format.");
                        }
                        string name = parts[0].Trim();
                        string value = parts[1].Trim();

                        // 將參數名稱和值加入字典中
                        parameters.Add(name, value);
                    }
                }

                // 檢查字典中是否包含必要的參數
                if (!parameters.ContainsKey("ID_Building") || !parameters.ContainsKey("maxFreq") ||
                    !parameters.ContainsKey("minFreq") || !parameters.ContainsKey("maxFreq_safe") ||
                    !parameters.ContainsKey("minFreq_safe")     || !parameters.ContainsKey("outputPath"))
                {
                    throw new Exception("Missing parameter in input file.");
                }

                // 解析各個參數的值
                ID_Building = int.Parse(parameters["ID_Building"]);
                maxFreq = double.Parse(parameters["maxFreq"]);
                minFreq = double.Parse(parameters["minFreq"]);
                maxFreq_safe = double.Parse(parameters["maxFreq_safe"]);
                minFreq_safe = double.Parse(parameters["minFreq_safe"]);
                outputPath = parameters["outputPath"];

                return true;
            }
            catch (Exception ex)
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} Error reading input file: {ex.Message}");
                return false;
            }
        }
    }
}
