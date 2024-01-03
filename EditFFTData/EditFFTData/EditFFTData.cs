using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using ExcelDataReader;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EditFFTData
{
    public class EditFFTData
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="Constr"></param>
        /// <param name="Time"></param>
        /// <returns></returns>
        public static void EditFFT(string inputPath, string outputPath, double maxFreq,  double minFreq,double Amplitude,  string Connstr,int ID_Building,int Type,string logDir,string debugLogFileName,bool IsOutPutFile)
        {


            try
            {
                bool ret = false;
                string PngFileName = "FFT.png";
                string PngSourceFile = Path.Combine(inputPath, PngFileName);
                string PngTargetFile = Path.Combine(outputPath, PngFileName);
                List<string> files = new List<string>();
                string[] extensions = new[] { ".csv", ".xlsx"};
                //string[] files = Directory.GetFiles(SourcePath, "*." + extension);
                foreach (string file in Directory.EnumerateFiles(inputPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => extensions.Any(ext => ext == Path.GetExtension(s))))
                {
                    files.Add(file);
                  //  Console.WriteLine(file);
                }

                //string[] files = Directory.EnumerateFiles(SourcePath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp3") || s.EndsWith(".jpg"));
                if (files.Count==0)
                {
                    Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} csv file not exist ");
                    //Console.WriteLine("file not exist");
                    //return "file not exist";
                }
                else // file exist
                {
                    foreach(string file in files)
                    {
                        string filename = Path.GetFileNameWithoutExtension(file);
                        string dateTimeFormat = "yyyy-MM-ddTHH-mm-ss-fff";

                        //int startIndex = filename.IndexOf("WebDAQ01_") + "WebDAQ01_".Length;
                        //string dateTimeString = filename.Substring(startIndex);

                        string pattern = @"\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}-\d{3}";
                        Match match = Regex.Match(filename, pattern);
                        string dateTimeString = "";
                        if (match.Success)
                        {
                            dateTimeString = match.Value;
                            Console.WriteLine(dateTimeString); // 在此處可以自行處理 dateTimeString
                        }
                        else
                        {
                            Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 未找到符合的時間格式");
                        }
                        DateTime dateTimeValue = DateTime.ParseExact(dateTimeString, dateTimeFormat, CultureInfo.InvariantCulture);
                        using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read))
                        {

                            FFTData fftdata = new FFTData();
                            switch (Type)
                            {
                                case 1:
                                    ret=FFTLibrary.FFT.Transform(stream, minFreq, maxFreq, Amplitude, out double Naturalfreq, out double Naturalfreq_A, out double Magnitude_avg, IsOutPutFile, outputPath, dateTimeString,filename);
                                    fftdata.Natural_Freq = Naturalfreq;
                                    fftdata.Natural_Freq_A = Naturalfreq_A;
                                    fftdata.Magnitude_avg=Magnitude_avg;
                                    stream.Close();
                                    break;
                                case 2:
                                    
                                    IExcelDataReader excelReader = ExcelReaderFactory.CreateReader(stream);//Excel 2007格式; *.xlsx
                                    DataSet result = excelReader.AsDataSet();
                                    stream.Close();
                                    result.AcceptChanges();

                                    //讀取xlsx檔，取得FFT數據
                                    fftdata = EditDataTable(result, minFreq, maxFreq, (int)Coodinate.RFXN);
                                    break;

                            }

                            //如果ret= true，表示符合微振等級，並且有成功分析頻率，寫入資料庫
                            //DataTable dt = StoredProcedureToDataTable("dbo.sp_ImportBuildingFreq", Connstr, dateTimeValue, ID_Building, fftdata.Natural_Freq, fftdata.Stability);
                            if(ret && fftdata.Natural_Freq_A > 4*fftdata.Magnitude_avg)
                            {
                                using (SqlConnection conn = new SqlConnection(Connstr))
                                {
                                    SqlCommand cmd = new SqlCommand("dbo.sp_ImportBuildingFreq", conn);
                                    cmd.CommandTimeout = 36000;
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add("@Time", SqlDbType.DateTime).Value = dateTimeValue;
                                    cmd.Parameters.Add("@Id_Building", SqlDbType.Int).Value = ID_Building; //東京大樓ID =30  ，未來有多棟再調整為動態
                                    cmd.Parameters.Add("@Freq", SqlDbType.Decimal).Value = fftdata.Natural_Freq;
                                    cmd.Parameters.Add("@Stability", SqlDbType.Int).Value = fftdata.Stability;
                                    //output
                                    SqlParameter paramErr = cmd.Parameters.Add("@ErrMsg", SqlDbType.NVarChar, 100);
                                    paramErr.Direction = ParameterDirection.Output;
                                    // ds.Tables["EqEvent"].Rows[0]["Time"];
                                    DataTable dt = new DataTable("Alarm");


                                    try
                                    {
                                        conn.Open();
                                        da.Fill(dt);
                                        conn.Close();
                                        //測試:目前東京大樓頻率平均0.7~0.72，低於0.7表示有異常，保留歷時檔不刪除
                                        if (fftdata.Natural_Freq <= maxFreq)
                                        {
                                            File.Copy(file, Path.Combine(inputPath, @"C:\SHM\SHMFiles\rawdata\"+ filename+".csv"),true);
                                        }
                                        File.Delete(files[0]);

                                    }
                                    catch (Exception e)
                                    {
                                        Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}DataTable Fill fail： {e.ToString()} ");
     
                                    }
                                    //如果通報數>0，表示偵測到頻率有異常情況，進行近期頻率繪圖
                                    if (dt.Rows.Count > 0)
                                    {
                                        Process process = new Process();
                                        process.StartInfo.FileName = @"C:\SHM\FrequencyPlotterV3\FrequencyPlotterV3.exe";
                                        process.StartInfo.UseShellExecute = false;
                                        process.Start();
                                        process.WaitForExit();
                                        Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}繪圖執行完畢 ");
                                    }


                                    //以下開始發頻率監測預警
                                    SendAlarm(dt, debugLogFileName, 3, conn);


                                    //以下開始檢查搜尋該微振數據的時間點是否有氣象局地震通報發送
                                    cmd = new SqlCommand("dbo.sp_Warn_EqFreq", conn);
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    da = new SqlDataAdapter(cmd);
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add("@Time", SqlDbType.DateTime).Value = dateTimeValue;
                                    cmd.Parameters.Add("@Id_Building", SqlDbType.Int).Value = ID_Building; //東京大樓ID =30  ，未來有多棟再調整為動態
                                    //output
                                    paramErr = cmd.Parameters.Add("@ErrMsg", SqlDbType.NVarChar, 100);
                                    paramErr.Direction = ParameterDirection.Output;
                                    // ds.Tables["EqEvent"].Rows[0]["Time"];
                                    dt = new DataTable("Alarm");

                                    try
                                    {
                                        conn.Open();
                                        da.Fill(dt);
                                        conn.Close();
                                        File.Delete(files[0]);
                                    }
                                    catch (Exception e)
                                    {
                                        Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}DataTable Fill fail： {e.ToString()} ");
                                       
                                    }
                                    //如果通報數>0，表示有發送地震前後通報，進行近期頻率繪圖
                                    if (dt.Rows.Count > 0)
                                    {
                                        Process process = new Process();
                                        process.StartInfo.FileName = @"C:\SHM\FrequencyPlotterV3\FrequencyPlotterV3.exe";
                                        process.StartInfo.UseShellExecute = false;
                                        process.Start();
                                        process.WaitForExit();
                                        Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}繪圖執行完畢 ");
                                    }
                                    //以下開始發地震前後頻率監測預警
                                    SendAlarm(dt, debugLogFileName, 4, conn);
                                }
                            }
                            else if(ret && fftdata.Natural_Freq_A < 4 * fftdata.Magnitude_avg)
                            {
                                //測試:目前東京大樓頻率平均0.7~0.72，低於0.7表示有異常，保留歷時檔不刪除
                                if (fftdata.Natural_Freq <= maxFreq)
                                {
                                    File.Copy(file, Path.Combine(inputPath, @"C:\SHM\SHMFiles\rawdata\" + filename+"_test" + ".csv"), true);
                                    Log.log(debugLogFileName, filename + "：FFT振幅< 4倍預警範圍的平均值");
                                    File.Delete(files[0]);
                                }
                            }
                            else
                            {
                                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 加速度振幅不符合微振等級");
                            }
                        }
                    }
                //    return "file  exist";
                }
            }
            catch (Exception e)
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 發生錯誤： {e.Message} ");
                //Console.WriteLine(e.Message);
                //return e.Message;
            }

        }
        public enum Coodinate
        {
            Freq = 0,
            RFXN = 1,
        }
        public class FFTData
        {
            public float[] XArray { get; set; }
            public float[] YArray { get; set; }
            public double Natural_Freq { get; set; }
            public double Natural_Freq_A { get; set; }
            public double Magnitude_avg { get; set; }
            public int Stability { get; set; }
        }

        
        public static FFTData EditDataTable(DataSet ds, double minfreq, double maxfreq, int dir)
        {
            //
            // TODO: 在這裡新增建構函式邏輯
            //

            List<float> listX = new List<float>();
            List<float> listY = new List<float>();
            DataTable dt = ds.Tables["FourierTransform"];
            foreach (DataRow element in dt.Rows)
            {
                if (element.ItemArray[0].GetType().Name == "Double")
                {
                    if ((double)element.ItemArray[(int)Coodinate.Freq] >= minfreq && (double)element.ItemArray[(int)Coodinate.Freq] <= maxfreq)
                    {
                        listX.Add(Convert.ToSingle(element.ItemArray[(int)Coodinate.Freq]));
                        listY.Add(Convert.ToSingle(element.ItemArray[dir]));
                    }
                }
            }
            return new FFTData
            {
                XArray = listX.ToArray(),
                YArray = listY.ToArray(),
                Natural_Freq = Convert.ToSingle(ds.Tables["ModalProperties"].Rows[6].ItemArray[2]),
                Stability = Convert.ToInt32(ds.Tables["ModalProperties"].Rows[5].ItemArray[2])
            };
        }

        public static void SendAlarm(DataTable dt,string debugLogFileName,int type, SqlConnection conn)
        {
            Alarm.Alarm alarm = new Alarm.Alarm();
            //以下開始發頻率監測預警

            if (dt.Rows.Count > 0)
            {
                DataRow[] drLine = dt.Select("NotifyMethod = 3");  // 2023/02/04 增加 Line 推播
            }
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int notifyMethod = (int)dt.Rows[i]["NotifyMethod"];
                string warnMessage = (string)dt.Rows[i]["WarnMessage"];
                string LineID = (string)dt.Rows[i]["LineID"];  // 2022/02/04 配合 Line 推播新增
                string name = (string)dt.Rows[i]["RealName"];
                DateTime alarmDateTime = DateTime.Now;
                string output;

                try
                {
                    bool truefalse = alarm.Send(type, notifyMethod, warnMessage, out output);
                    //TODO: log
                    switch (notifyMethod)
                    {
                        case 3:
                            Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} After 發 Line {name} {LineID} : {truefalse}");
                            break;
                    }
                    string sql = "UPDATE WarnLogUser SET SentTime=@SentTime, IsSent=@IsSent, ErrMessage=@ErrMessage WHERE ID=@ID;";
                    SqlCommand updateCmd = new SqlCommand(sql, conn);
                    updateCmd.Parameters.Add("@ID", SqlDbType.Int).Value = (int)dt.Rows[i]["ID"];
                    updateCmd.Parameters.Add("@SentTime", SqlDbType.DateTime).Value = alarmDateTime;
                    updateCmd.Parameters.Add("@IsSent", SqlDbType.Bit).Value = truefalse;
                    updateCmd.Parameters.Add("@ErrMessage", SqlDbType.NVarChar, 1200).Value = output;
                    Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} Before Update DB: {sql}");
                    conn.Open();
                    updateCmd.ExecuteNonQuery();
                    conn.Close();
                    Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} After Update DB: {sql}");
                }
                catch (Exception ex)
                {
                    Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} 發生錯誤： {ex.Message}  {ex.StackTrace}");
                }
            }
        }
    }
}
