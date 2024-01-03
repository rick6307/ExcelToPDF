// See https://aka.ms/new-console-template for more information

using ExcelToPDF;
using System.Drawing;
using System.Data;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OpenAI_API;
using OpenAI_API.Models;
using OpenAI_API.Chat;
using Spire.Xls;
using ScottPlot;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string logDir = @"C:\SHM\ExcelToPDF2\Log";
        string debugLogFileName = $"{logDir}\\ExcelToPDF2{DateTime.Now.ToString("yyyyMMddhhmm")}.log";
        if (!ReadInputFile(@"C:\SHM\ExcelToPDF2\config.txt", debugLogFileName, out string inputPath, out string outputPath))
        {
            Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 開啟設定檔失敗");
        }
        // 建立 SQL Server 連線字串
        string connectionString = "database=VM05076;server=192.168.10.129;uid=VM05076;pwd=$in0t3ch;Pooling=True;encrypt=false;";
        // 創建一個 EqInfo 物件
        EqInfo eqinfo = new EqInfo
        {
            Level = new List<int>(),
            StationIntensity = new List<int>(),
            Time = new List<DateTime>()
        };
        FreqData freqdata = new FreqData
        {
            Freq = new List<double>(),
            Time = new List<DateTime>()
        };


        using (SqlConnection connection = new SqlConnection(connectionString))
        {

        string inputFilePath = outputPath+@"\Template.xlsx";
        string saveFilePath = outputPath+@"\Template_output.xlsx";


        // 開啟 Template.xlsx 檔案
        FileInfo fileInfo = new FileInfo(inputFilePath);
        using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
        {
            // 另存新檔
            FileInfo saveFileInfo = new FileInfo(saveFilePath);
            excelPackage.SaveAs(saveFileInfo);
        }


        // 重新開啟 Template_output.xlsx 檔案
        FileInfo fileInfo2 = new FileInfo(saveFilePath);
        using (ExcelPackage excelPackage = new ExcelPackage(fileInfo2))
        {
            int i=0;
            // 獲取第一個工作表
            ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets["001"];
            //  1.寫入摘要
            worksheet.Cells[10, 7].Value = EqEventCount;
            worksheet.Cells[10, 14].Value = MaxStationIntensity;
            worksheet.Cells[10, 24].Value = MaxBuildingLevel;
            worksheet.Cells[11, 21].Value = MaxStationIntensityTime;
            worksheet.Cells[12, 7].Value = freqdata.Freq.Count();
            worksheet.Cells[12, 14].Value = MaxFreq;
            worksheet.Cells[12, 21].Value = MaxFreqTime;
            if (MaxFreq < maxFreq_safe & MaxFreq > minFreq_safe)
                worksheet.Cells[12, 28].Value = "正常";
            else
                worksheet.Cells[12, 28].Value = "異常";
            worksheet.Cells[13, 14].Value = MinFreq;
            worksheet.Cells[13, 21].Value = MinFreqTime;
            if (MinFreq < maxFreq_safe & MinFreq > minFreq_safe)
                worksheet.Cells[13, 28].Value = "正常";
            else
                worksheet.Cells[13, 28].Value = "異常";

            // 2.寫入地震事件統計
            if (EqEventCount == 0)
            {
                worksheet.Row(26).Hidden = true;
                worksheet.Row(27).Hidden = true;
                worksheet.Cells[28, 3].Value = "本月無地震通報。";
            }
            else
            {
                for (i = 0; i < EqEventCount; i++)
                {
                    if (i > 0)
                    {
                        worksheet.InsertRow(27 + i, 1, 27);
                        ExcelRange mergeRange = worksheet.Cells[27 + i, 7, 27 + i, 8];
                        mergeRange.Merge = true;
                        mergeRange = worksheet.Cells[27 + i, 9, 27 + i, 15];
                        mergeRange.Merge = true;
                        mergeRange = worksheet.Cells[27 + i, 16, 27 + i, 18];
                        mergeRange.Merge = true;
                        mergeRange = worksheet.Cells[27 + i, 19, 27 + i, 24];
                        mergeRange.Merge = true;
                    }
                    worksheet.Cells[27 + i, 7].Value = i + 1;
                    worksheet.Cells[27 + i, 9].Value = eqinfo.Time[i].ToString();
                    worksheet.Cells[27 + i, 16].Value = eqinfo.Level[i].ToString();
                    worksheet.Cells[27 + i, 19].Value = eqinfo.StationIntensity[i].ToString();
                }
            }
            //3.寫入頻率統計
            bool ret = FrequencyPoltter(freqdata, minFreq_safe, maxFreq_safe, outputPath);
            string filepath = outputPath + @"\frequencyChart.png";
            Image image = Image.FromFile(filepath);
            OfficeOpenXml.Drawing.ExcelPicture picture = worksheet.Drawings.AddPicture("pic", image);
            picture.SetPosition(31+i, 6,6,2);
            picture.SetSize(320, 240);
            // 存檔Excel檔案
            worksheet.Calculate();
            excelPackage.SaveAs(fileInfo2);
        }
        // 設定是否使用 OpenAI API
        if (IsOpenAIAPI)
        {
            // 取得檔案資訊並使用 ExcelPackage 開啟該檔案
            FileInfo fileInfo3 = new FileInfo(saveFilePath);
            using (ExcelPackage excelPackage = new ExcelPackage(fileInfo3))
            {
                // 從該檔案中取得指定的工作表
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets["001"];
                // 建立一個 OpenAI API 的物件並傳入金鑰
                var api = new OpenAIAPI(OpenAIKey);

                // 從 Excel 中取得對話訊息
                string messages = worksheet.Cells[3, 47].Value.ToString() + "\n" + worksheet.Cells[3, 48].Value.ToString() + "\n" + worksheet.Cells[3, 49].Value.ToString();

                // 使用 OpenAI API 進行聊天，傳入相關參數
                var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
                {
                    Model = Model.ChatGPTTurbo,
                    Temperature = 0.1,
                    MaxTokens = 1024,
                    Messages = new ChatMessage[] {
                    new ChatMessage(ChatMessageRole.User, messages)
                    }
                });
                // 取得 OpenAI API 回傳的訊息
                var reply = result.Choices[0].Message;
                // 在 Console 中輸出回應訊息
                Console.WriteLine($"{reply.Role}: {reply.Content.Trim()}");
                // 在 Excel 中指定的儲存格中儲存 OpenAI API 回傳的訊息
                worksheet.Cells[48, 4].Value = result;
                // 將修改後的 Excel 檔案另存至原本的檔案路徑
                excelPackage.SaveAs(fileInfo3);
            }
        }
        // 轉換Excel檔案為PDF格式
        Workbook workbook = new Workbook();
        workbook.LoadFromFile(@"C:\SHM\ExcelToPDF\Template_output.xlsx");
        Worksheet sheet = workbook.Worksheets["001"];
        sheet.SaveToPdf(@"C:\SHM\ExcelToPDF\Template_output.pdf");
        // 關閉Excel檔案
        //excelPackage.Dispose();
    }


    static bool ReadInputFile(string filePath, string debugLogFileName, out string inputPath, out string outputPath)
    {
        inputPath = "";
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
            if (!parameters.ContainsKey("outputPath")||!parameters.ContainsKey("inputPath"))
            {
                throw new Exception("Missing parameter in input file.");
            }

            // 解析各個參數的值
            inputPath = parameters["inputPath"];
            outputPath = parameters["outputPath"];


            return true;
        }
        catch (Exception ex)
        {
            Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} Error reading input file: {ex.Message}");
            return false;
        }
    }
    
    public class EqInfo
    {
        public List<int>? StationIntensity { get; set; }
        public List<DateTime>? Time { get; set; }
        public List<int>? Level { get; set; }
    }
    public class FreqData
    {
        public List<double> Freq { get; set; }
        public List<DateTime> Time { get; set; }
    }

}