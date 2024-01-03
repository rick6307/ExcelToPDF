using System.Collections;
using System.Diagnostics;
using System;
using System.IO;


namespace Alarm
{
    /// <summary>
    /// 各種送出簡訊的Static Method，目前已實做一種
    /// </summary>
    public class Sms
    {
        static string _mWorkingDirectory = "";

        /// <summary>
        /// 送出手機簡訊
        /// </summary>
        /// <param name="smsText">簡訊內容</param>
        /// <param name="phones">手機號碼陣列集合</param>
        /// <returns>成功時傳回true，失敗傳回false</returns>
        public static bool SendSms(string smsText, ArrayList phones, out string error)
        {
            string phone = "";
            error = "";
            int i = 1;
            foreach (string str in phones)
            {
                if (i == phones.Count)
                {
                    phone += str;
                }
                else
                {
                    phone += str + ",";
                }
                i++;
            }
            bool isSms;
            //try
            //{
                isSms = SendSms4Mail(smsText, phone, out error);
            //}
            //catch(Exception ex)
            //{
            //    isSms = false;
            //    throw ex;
            //}
            // 目前使用 SMS4Mail，以後可更換其它Method
            return isSms;
        }
        /// <summary>
        /// 送出手機簡訊
        /// </summary>
        /// <param name="smsText">簡訊內容</param>
        /// <param name="phone">手機號碼</param>
        /// <returns>成功時傳回true，失敗傳回false</returns>
        public static bool SendSms(string smsText, string phone, out string error)
        {
            // 目前使用 SMS4Mail，以後可更換其它Method
            bool isSms;
            error = "";
            try
            {
                isSms = SendSms4Mail(smsText, phone, out error);
            }
            catch
            {
                isSms = false;
            }

            return isSms;
        }
        /// <summary>
        /// 送出手機簡訊
        /// </summary>
        /// <param name="smsText">簡訊內容</param>
        /// <param name="phones">手機號碼陣列</param>
        /// <returns>成功時傳回true，失敗傳回false</returns>
        public static bool SendSms(string smsText, string[] phones, out string error)
        {
            string phone = "";
            error = "";
            for (int i = 0; i < phones.Length; i++)
            {
                if (i == (phones.Length - 1))
                {
                    phone += phones[i];
                }
                else
                {
                    phone += phones[i] + ",";
                }
            }

            // 目前使用 SMS4Mail，以後可更換其它Method
            bool isSms;
            try
            {
                isSms = SendSms4Mail(smsText, phone, out error);
            }
            catch
            {
                isSms = false;
            }

            return isSms;
        }

        /// <summary>
        /// 送出手機簡訊，使用smscmd.exe，需要人工設定sms4mail.txt
        /// </summary>
        /// <param name="smsText">簡訊內容，記得加上送簡訊者為何，因為號碼固定</param>
        /// <param name="phone">手機號碼，多號碼時用逗號分隔，中間無空白</param>
        /// <returns>成功時傳回true，失敗傳回false</returns>
        private static bool SendSms4Mail(string smsText, string phone, out string error)
        {
            bool ret = true;
            const int maxSmsWords = 355;
            error = "";
            if (smsText.Length <= maxSmsWords)
            {
                ret = SendSms4MailReallySend(smsText, phone, out error);
                if (!error.Contains("簡訊傳送完畢"))
                {
                    ret = false;
                }
            }
            else
            {
                int nCount = smsText.Length / maxSmsWords;
                int tail = smsText.Length % maxSmsWords;
                if (tail > 0)
                {
                    nCount++;
                }
                for (int i = 0; i < nCount; i++)
                {
                    System.Threading.Thread.Sleep(1500);
                    bool tempRet = SendSms4MailReallySend(smsText.Substring(maxSmsWords * i, maxSmsWords), phone, out error);
                    if(!tempRet || !error.Contains("簡訊傳送完畢"))
                    {
                        ret = false;
                    }
                }
            }
            System.Threading.Thread.Sleep(1500);
            return ret;
        }

        private static bool SendSms4MailReallySend(string smsText, string phone, out string output)
        {
            output = "";
            smsText = "\"" + smsText + "\" \"" + phone + "\"";
            string smscmdPath = AppDomain.CurrentDomain.BaseDirectory + "smscmd.exe";
            if (File.Exists(smscmdPath))
            {
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                smscmdPath = @"c:\smscmd\smscmd.exe";
                WorkingDirectory = @"c:\smscmd\";
            }
            if (!File.Exists(smscmdPath))
            {
                return false;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(smscmdPath);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = smsText;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            if (Directory.Exists(WorkingDirectory))
            {
                startInfo.WorkingDirectory = WorkingDirectory;
            }
            Process p = Process.Start(startInfo);
            StreamReader reader = p.StandardOutput;
            output = reader.ReadToEnd();
            if (p != null)
            {
                p.WaitForExit();
                if (p.ExitCode == 1)
                {
                    if(!string.IsNullOrWhiteSpace(output))
                    {
                        if (output.Contains("簡訊傳送完畢"))
                        { 
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static string WorkingDirectory
        {
            set
            {
                _mWorkingDirectory = value;
            }
            get
            {
                if (_mWorkingDirectory.Trim().Length == 0)
                {
                    _mWorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                }
                return _mWorkingDirectory;
            }
        }
    }
}