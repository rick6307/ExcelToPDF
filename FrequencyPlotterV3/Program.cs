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
            // 檢查是否有傳入 ID_Building 參數
            if (args.Length == 0 || !int.TryParse(args[0], out int ID_Building))
            {
                Console.WriteLine("請提供有效的 ID_Building 參數。");
                return;
            }

            // 設定日誌目錄和日誌檔案名稱
            string logDir = @"C:\SHM\FrequencyPlotterV3\Log";
            string debugLogFileName = $"{logDir}\\FrequencyPlotter{DateTime.Now.ToString("yyyyMMddhhmm")}.log";

            // 讀取設定檔，並從資料庫中取得參數
            if (!ReadInputFile(@"C:\SHM\FrequencyPlotterV3\config.txt", ID_Building, out string connstr, out double maxFreq, out double minFreq, out double maxFreq_safe, out double minFreq_safe, out string outputPath, debugLogFileName))
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 開啟設定檔失敗");
                return;
            }

            // 查詢 BuildingName
            string buildingName = GetBuildingName(connstr, ID_Building);
            if (string.IsNullOrEmpty(buildingName))
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 查詢 BuildingName 失敗");
                return;
            }

            // 創建一個 FreqData 物件來儲存頻率和時間資料
            FreqData freqdata = new FreqData
            {
                Freq = new List<double>(),
                Time = new List<DateTime>()
            };

            // 使用提供的連線字串連接到資料庫並讀取資料
            using (SqlConnection connection = new SqlConnection(connstr))
            {
                // 建立 SQL 查詢字串，查詢當前時間的過去兩周資料
                string query = "SELECT Time, Freq FROM dbo.BuildingFreq WHERE ID_Building = @ID_Building AND Time >= DATEADD(WEEK, -2, GETDATE());";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID_Building", ID_Building);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                // 讀取資料並將其加入 freqdata 物件中
                while (reader.Read())
                {
                    freqdata.Freq.Add(Convert.ToDouble(reader["Freq"]));
                    freqdata.Time.Add(Convert.ToDateTime(reader["Time"]));
                }

                // 關閉資料庫連線
                reader.Close();
                connection.Close();
            }

            // 使用 LINQ 和 DateTime.ToOADate() 將 DateTime[] 轉換為 double[]
            double[] xs = freqdata.Time.Select(x => x.ToOADate()).ToArray();

            // 繪製頻率圖形
            var plt = new Plot(1024, 768);
            plt.AddScatterPoints(xs, freqdata.Freq.ToArray());
            plt.XAxis.DateTimeFormat(true);
            plt.Title($"{buildingName}頻率監測");
            plt.YLabel("Frequency (Hz)");
            plt.XLabel("Date");

            // 添加安全範圍的垂直區間
            Color Greencolor = Color.FromArgb(70, 173, 255, 47);
            Color Redcolor = Color.FromArgb(70, 255, 47, 47);
            plt.AddVerticalSpan(minFreq_safe, maxFreq_safe, Greencolor, label: "安全範圍");
            plt.SetAxisLimitsY(0, 1.5);

            // 添加網格和圖例
            plt.Grid(true);
            plt.Legend();

            // 設定輸出檔案路徑並儲存圖形
            string filepath = outputPath + @"\" + ID_Building.ToString() + @"\" + freqdata.Time.Last().ToString("yyyyMMddHHmmss");
            Directory.CreateDirectory(filepath);
            plt.SaveFig(filepath + @"\frequencyChart.png");
        }

        // 定義 FreqData 類別來儲存頻率和時間資料
        public class FreqData
        {
            public List<double> Freq { get; set; }
            public List<DateTime> Time { get; set; }
        }

        // 讀取設定檔並從資料庫中取得參數
        public static bool ReadInputFile(string filePath, int ID_Building, out string connstr,
                                        out double maxFreq, out double minFreq,
                                        out double maxFreq_safe, out double minFreq_safe,
                                        out string outputPath, string debugLogFileName)
        {
            connstr = "";
            maxFreq = 0;
            minFreq = 0;
            maxFreq_safe = 0;
            minFreq_safe = 0;
            outputPath = "";
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            try
            {
                // 讀取設定檔中的參數
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // 將每一行的名稱和值分開
                        int index = line.IndexOf('=');
                        if (index == -1)
                        {
                            throw new Exception("Invalid input file format.");
                        }
                        string name = line.Substring(0, index).Trim();
                        string value = line.Substring(index + 1).Trim();

                        // 將參數名稱和值加入字典中
                        parameters.Add(name, value);
                    }
                }

                // 檢查是否包含必要的參數
                if (!parameters.ContainsKey("connstr") || !parameters.ContainsKey("outputPath"))
                {
                    throw new Exception("Missing parameter in input file.");
                }

                // 取得連線字串和輸出路徑
                connstr = parameters["connstr"];
                outputPath = parameters["outputPath"];

                // 使用連線字串連接到資料庫並讀取參數
                using (SqlConnection connection = new SqlConnection(connstr))
                {
                    string query = "SELECT maxFreq, minFreq, Freq_U, Freq_L FROM dbo.BuildingFreqLevel WHERE ID_Building = @ID_Building";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ID_Building", ID_Building);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    // 讀取資料並將其賦值給對應的變數
                    if (reader.Read())
                    {
                        maxFreq = Convert.ToDouble(reader["maxFreq"]);
                        minFreq = Convert.ToDouble(reader["minFreq"]);
                        maxFreq_safe = Convert.ToDouble(reader["Freq_U"]);
                        minFreq_safe = Convert.ToDouble(reader["Freq_L"]);
                    }

                    // 關閉資料庫連線
                    reader.Close();
                    connection.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                // 記錄錯誤訊息
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} Error reading input file: {ex.Message}");
                return false;
            }
        }

        // 查詢 BuildingName
        public static string GetBuildingName(string connstr, int ID_Building)
        {
            string buildingName = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connstr))
                {
                    string query = "SELECT BuildingName FROM dbo.Building WHERE ID= @ID_Building";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ID_Building", ID_Building);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        buildingName = reader["BuildingName"].ToString();
                    }

                    reader.Close();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying BuildingName: {ex.Message}");
            }

            return buildingName;
        }
    }
}
