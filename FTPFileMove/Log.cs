//using java.io;
using System;
using System.IO;
using System.Text;

namespace FTPFileMove
{
    /// <summary>
    /// Summary description for Log.
    /// </summary>
    public class Log
    {
        private const long FileMaxLength = 1024000000; //1GB

        public static void CopyFile(string source, string target)
        {
            using (FileStream fs = File.Open(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (FileStream outputStream = File.Open(target, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] b = new byte[1024];
                    int k;
                    do
                    {
                        k = fs.Read(b, 0, b.Length);
                        outputStream.Write(b, 0, k);
                    } while (k > 0);
                }                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public static void log(String path, String msg, string encoding)
        {
            log(path, msg, Encoding.GetEncoding(encoding));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public static void log(String path, String msg, Encoding encoding)
        {
            if (path != null && msg != null)
            {
                try
                {
                    WriteToFile(path, msg, encoding);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An exception occured in log: " + e);
                }
            }
            else
            {
                throw new Exception("Null value supplied to Log.log().");
            }
        }



        /// <summary>
        /// If overwirte is true, the target file will be overwritten
        /// </summary>
        /// <param name="path"></param>
        /// <param name="msg"></param>
        /// <param name="overwrite">是否覆寫</param>
        public static void log(String path, String msg, bool overwrite)
        {
            if (path != null && msg != null)
            {
                try
                {
                    WriteToFile(path, msg, overwrite);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An exception occured in log: " + e);
                }
            }
            else
            {
                throw new Exception("Null value supplied to Log.log().");
            }
        }

        /// <summary>
        /// 記錄加至檔案 append，檔案超過 FileMaxLength 時要Overwrite
        /// </summary>
        /// <param name="path"></param>
        /// <param name="msg"></param>
        public static void log(String path, String msg)
        {
            bool overwrite = false;
            if (File.Exists(path))
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Length > FileMaxLength)
                {
                    overwrite = true;
                }
            }
            WriteToFile(path, msg, overwrite);
        }


        private static void WriteToFile(string path, string msg, bool overwrite)
        {
            StreamWriter sw;
            if (overwrite)
            {
                sw = File.CreateText(path);
            }
            else
            {
                sw = File.AppendText(path);
            }

            sw.WriteLine(msg);
            sw.Flush();
            sw.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        private static void WriteToFile(string path, string msg, Encoding encoding)
        {
            StreamWriter sw = new StreamWriter(path, true, encoding);
            sw.WriteLine(msg);
            sw.Flush();
            sw.Close();
        }
    }
}