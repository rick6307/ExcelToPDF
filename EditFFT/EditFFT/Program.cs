using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFTLibrary;

namespace EditFFT
{
    class Program
    {
        static void Main(string[] args)
        {

            string connstr = "database=VM05076;server=192.168.10.129;uid=VM05076;pwd=$in0t3ch;Pooling=True";
           
            int Type = 1; // Type 1 :以CSV作資料處理只作FFT求出頻率，匯入DB ，Type 2：以FreqID.exe 產生之result.xlsx取得頻率，輸入DB。

            string logDir = @"C:\SHM\EditFFT\Log";
            //string logDir = @"C:\Users\2239\Documents\C#\EditFFT\Log";
            string debugLogFileName = $"{logDir}\\EditFFTAlarm{DateTime.Now.ToString("yyyyMMddhhmm")}.log";
            if (!ReadInputFile(@"C:\SHM\EditFFT\config.txt", out int ID_Building, out double maxFreq, out double minFreq, out double Amplitude, out string inputPath, out string outputPath, debugLogFileName,out bool IsOutPutFile))
            {
               EditFFTData.Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 開啟設定檔失敗");
            }

            try
            {
                EditFFTData.EditFFTData.EditFFT(inputPath, outputPath, maxFreq, minFreq, Amplitude, connstr, ID_Building, Type, logDir, debugLogFileName, IsOutPutFile);
                
            }
            catch (Exception ex)
            {
                EditFFTData.Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 發生錯誤：{ex.Message}");
                //Console.WriteLine(DateTime.Now.ToString() + "------------" + "Error writing output file: " + ex.Message);
            }
            
        }
        public static bool ReadInputFile(string filePath, out int ID_Building,
                                        out double maxFreq, out double minFreq,out double Amplitude, out string inputPath,
                                        out string outputPath, string debugLogFileName,out bool IsOutPutFile)
        {
            ID_Building = 0;
            maxFreq = 0;
            minFreq = 0;
            Amplitude = 0;
            inputPath = "";
            outputPath = "";
            IsOutPutFile = false;
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
                if (!parameters.ContainsKey("ID_Building")||!parameters.ContainsKey("maxFreq") || !parameters.ContainsKey("minFreq") ||
                    !parameters.ContainsKey("inputPath") || !parameters.ContainsKey("outputPath") || !parameters.ContainsKey("Amplitude") || 
                    !parameters.ContainsKey("IsOutPutFile"))
                {
                    throw new Exception("Missing parameter in input file.");
                }

                // 解析各個參數的值
                ID_Building = int.Parse(parameters["ID_Building"]);
                maxFreq = double.Parse(parameters["maxFreq"]);
                minFreq = double.Parse(parameters["minFreq"]);
                Amplitude = double.Parse(parameters["Amplitude"]);
                inputPath = parameters["inputPath"];
                outputPath = parameters["outputPath"];
                IsOutPutFile = bool.Parse(parameters["IsOutPutFile"]);


                return true;
            }
            catch (Exception ex)
            {
                EditFFTData.Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} Error reading input file: {ex.Message}");
                //Console.WriteLine("Error reading input file: " + ex.Message);
                return false;
            }
        }

    }
}
