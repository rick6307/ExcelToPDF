using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FTPFileMove
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string configFilePath = @"C:\SHM\FTPFileMove\config.txt";
            string ftpServer="";
            string ftpUsername = "";
            string ftpPassword = "";
            string remoteFolderPath = "";
            string localFolderPath = "";
            long fileminsize= 0;
            string logDir = @"C:\SHM\FTPFileMove\Log";
            string debugLogFileName = $"{logDir}\\{DateTime.Now.ToString("yyyyMMddhhmm")}.log";

            //string ftpServer = "172.30.31.233";
            //string ftpUsername = "admin";
            //string ftpPassword = "admin";
            //string remoteFolderPath = "/WebDAQ01/";
            //string localFolderPath = "C:/SHM/SHMFiles/Monitor/TI22028/";

            try
            {
                // 讀取 config.txt 檔案內容
                string[] configLines = File.ReadAllLines(configFilePath);

                // 解析並設定連線資訊
                foreach (string line in configLines)
                {
                    if (line.StartsWith("ftpServer="))
                    {
                        ftpServer = line.Substring("ftpServer=".Length);
                    }
                    else if (line.StartsWith("ftpUsername="))
                    {
                        ftpUsername = line.Substring("ftpUsername=".Length);
                    }
                    else if (line.StartsWith("ftpPassword="))
                    {
                        ftpPassword = line.Substring("ftpPassword=".Length);
                    }
                    else if (line.StartsWith("remoteFolderPath="))
                    {
                        remoteFolderPath = line.Substring("remoteFolderPath=".Length);
                    }
                    else if (line.StartsWith("localFolderPath="))
                    {
                        localFolderPath = line.Substring("localFolderPath=".Length);
                    }
                    else if (line.StartsWith("fileminsize="))
                    {
                        fileminsize = Convert.ToInt64(line.Substring("fileminsize=".Length));
                    }
                }


                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer}{remoteFolderPath}");
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftpRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                using (FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                using (Stream responseStream = ftpResponse.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string directoryListing = reader.ReadToEnd();
                    string[] fileList = directoryListing.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    var wddFiles = Array.FindAll(fileList, f => f.EndsWith(".wdd"));
                    Array.Sort(wddFiles, StringComparer.OrdinalIgnoreCase);
                    foreach (string file in wddFiles)
                    {
                        string fileName = file.Substring(file.LastIndexOf(' ') + 1);
                        bool ret=DownloadFile(ftpServer, remoteFolderPath, fileName, localFolderPath, ftpUsername, ftpPassword, debugLogFileName, fileminsize);
                        
                        if (ret)
                        {
                            DeleteOriginalFiles(ftpServer, remoteFolderPath, fileName, ftpUsername, ftpPassword, debugLogFileName);
                        }
                        Thread.Sleep(20000);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");
                Environment.Exit(0);
            }
        }

        static bool DownloadFile(string ftpServer, string remoteFolderPath, string fileName, string localFolderPath, string ftpUsername, string ftpPassword, string debugLogFileName, long fileminsize)
        {
            try
            {
                bool ret = false;
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer}{remoteFolderPath}{fileName}");
                ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                ftpRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                string tempFolderPath = $"C:/SHM/SHMFiles/Monitor/TI22028/temp";
                using (FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                using (Stream responseStream = ftpResponse.GetResponseStream())
                using (FileStream fileStream = File.Create(Path.Combine(tempFolderPath, fileName)))
                {
                    responseStream.CopyTo(fileStream);
                    Console.WriteLine($"Downloaded file to tempFolder: {fileName}");
                }
                string[] files = Directory.GetFiles(tempFolderPath);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Length >= fileminsize)
                    {
                        File.Copy(file, Path.Combine(localFolderPath, fileName),true);
                        Console.WriteLine($"Copyed file to {localFolderPath} : {fileName}");
                        ret = true;
                    }
                    else
                    {
                        ret = false;
                    }
                    File.Delete(file);
                }
                return ret;
            }
            catch (Exception ex)
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} Error downloading file: {ex.Message}");
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return false;
            }
        }

        static void DeleteOriginalFiles(string ftpServer, string remoteFolderPath, string fileName, string ftpUsername, string ftpPassword, string debugLogFileName)
        {
            try
            {

                    FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer}{remoteFolderPath}{fileName}");
                    ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                    ftpRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                    using (FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                    {
                        Console.WriteLine($"Deleted file: {fileName}");
                    }
            }
            catch (Exception ex)
            {
                Log.log(debugLogFileName, $"{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")} Error deleting files: {ex.Message}");
                Console.WriteLine($"Error deleting files: {ex.Message}");
            }
        }
    }
}
