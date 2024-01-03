using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alarm
{
    public class Alarm
    {
        /// <summary>
        /// Sends the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="notifyMethod">The notify method.</param>
        /// <param name="warnMessage">The warn message.</param>
        /// <param name="mobilePhone">The mobile phone.</param>
        /// <param name="email">The email.</param>
        /// <param name="name">The name.</param>
        /// <param name="title">The warn title.</param>
        /// <returns></returns>
        public class EqInfoMessage
        {
            public DateTime EventTime { get; set; }
            public String BuildingName { get; set; }
            public int NearestEqLevel { get; set; } // NearestEqIntensity
            public int BuildingDamageLevel { get; set; }
            public String ImgUrl { get; set; }
            public String ImgHtmlUrl { get; set; }
            public String DamageLevelDescUrl { get; set; }
            public String LastEqUrl { get; set; }
            public String[] LineIdList { get; set; }
        }
        public class MsgResult
        {
            public int status { get; set; }
            public String Message { get; set; }
            public bool? data { get; set; }
            public JObject errors { get; set; }
        }

        public bool Send(int type, int notifyMethod, string warnMessage, string mobilePhone, string email, string name, string title, out string error)
        {
            string stringSeparator = GetSeparator(type);
            bool ret = true;

            error = "";
            switch (notifyMethod)
            {
                case 1:
                    ret = SendSms(warnMessage, mobilePhone, stringSeparator, out error);
                    break;
                case 2:
                    ret = SendEmail(warnMessage, email, name, title, stringSeparator);
                    break;
                case 3:
                    
                    ret = SendLine(warnMessage, out error);
                    break;
                    //case 3:
                    //     ret = SendSms(warnMessage, mobilePhone, stringSeparator);
                    //     ret = SendEmail(warnMessage, email, name, title, stringSeparator);
                    //break;
            }
            return ret;
        }

        /// <summary>
        /// Sends the SMS.
        /// </summary>
        /// <param name="warnMessage">The warn message.</param>
        /// <param name="phone">The phone.</param>
        /// <param name="stringSeparator">The string separator.</param>
        /// <returns></returns>
        private bool SendSms(string warnMessage, string phone, string stringSeparator, out string error)
        {
            bool ret = true;
            error = "";
            if (warnMessage.Contains(stringSeparator))
            {
                
                string[] result = GetSplitMessages(warnMessage, stringSeparator);

                for (int i = 0; i < result.Length; i++)
                {
                    if (!Sms.SendSms(result[i], phone, out error))
                    {
                        ret = false;
                    }
                    Thread.Sleep(7000);
                }
            }
            else
            {
                if (!Sms.SendSms(warnMessage, phone, out error))
                {
                    ret = false;
                }
            }
            return ret;
        }

        public string[] GetSplitMessages(string warnMessage, string stringSeparator)
        {
            string[] stringSeparators = new string[] { stringSeparator };
            string[] ret= warnMessage.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            for(int i=0; i< ret.Length; i++)
            {
                ret[i] = stringSeparator + ret[i];
            }
            return ret;
        }

        /// <summary>
        /// Gets the separator.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public string GetSeparator(int type)
        {
            string stringSeparator;
            if (type == 1)
            {
                stringSeparator = "明志科大地震通報";
            }
            else
            {
                stringSeparator = "明志科大建安通報";
            }

            return stringSeparator;
        }

        /// <summary>
        /// Sends the email.
        /// </summary>
        /// <param name="warnMessage">The warn message.</param>
        /// <param name="email">The email.</param>
        /// <param name="name">The name.</param>
        /// <param name="title">The Warn title.</param>
        /// <returns></returns>
        private bool SendEmail(string warnMessage, string email, string name, string title, string sender)
        {
            bool ret = true;
            //SmtpClient smtpClient = new SmtpClient("mail.sinotech.org.tw");
            SmtpClient smtpClient = new SmtpClient("smtp.office365.com");
            // https://msdn.microsoft.com/zh-tw/library/system.net.mail.smtpclient.enablessl(v=vs.110).aspx  
            // 不支援使用 SSL 465，只支援SSL 587 。
            smtpClient.Port = 587;
            smtpClient.EnableSsl = true;

			NetworkCredential basicCredential = new NetworkCredential("mcut0560@o365.mcut.edu.tw", "Mc905001Z1");
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = basicCredential;


            MailMessage message = new MailMessage();
            //message.From = new MailAddress(email, name);
            message.From = new MailAddress("mcut0560@o365.mcut.edu.tw", sender);
            message.To.Add(new MailAddress(email, name));
            message.IsBodyHtml = true;
            message.Subject = title;
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            message.Body = "<html><head></head><body><div>";// + name + " &nbsp;您好：</div><br /><div>&nbsp;&nbsp;";
            message.Body += warnMessage;
            message.Body += "</div></body></html>";
            message.BodyEncoding = System.Text.Encoding.UTF8;

            //try
            //{
                smtpClient.Send(message); //真正寄
            //}
            //catch (Exception  ex)
            //{
            //    ret = false;
            //    Console.WriteLine(ex.Message + " " + ex.StackTrace);
            //}
            message.Dispose();
            return ret;
        }
        private bool SendLine(string warnMessage, out string error)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] 1. 剛進入SendLine");
            error = "";
            bool ret = true;
            byte[] bytes;
            String url = "https://shm.sinotech.org.tw/service/api/LineBot/EqNotify";
            EqInfoMessage obj = new EqInfoMessage() //造物件
            {
                EventTime = DateTime.Parse("2022-12-10 04:14:47.000"),
                BuildingName = "東京大樓",
                NearestEqLevel = 3,
                BuildingDamageLevel = 2,
                ImgUrl = "https://scweb.cwb.gov.tw/webdata/OLDEQ/202212/2022121004144740176_H.png",
                ImgHtmlUrl = "https://scweb.cwb.gov.tw/webdata/OLDEQ/202212/2022121004144740176_H.png",
                DamageLevelDescUrl = "https://shm.sinotech.org.tw/service/EqNotify/BuildingDamage",
                LastEqUrl = "https://shm.sinotech.org.tw/service/EqNotify/LastEarthquake",
                LineIdList = new string[] { "Uf94e6fe2a747d9381a6377abba7f06d3" },
            };


            string jsonData = JsonConvert.SerializeObject(obj);  //將物件序列化成字串
            EqInfoMessage test = JsonConvert.DeserializeObject<EqInfoMessage>(jsonData);
            bytes = Encoding.GetEncoding("utf-8").GetBytes(test.BuildingName);
            string BuildingNameencode = Convert.ToBase64String(bytes);
            bytes = Encoding.GetEncoding("utf-8").GetBytes(test.LineIdList[0]);
            string LineIdencode = Convert.ToBase64String(bytes);
            test.LastEqUrl = test.LastEqUrl + "?n="+HttpUtility.UrlEncode(BuildingNameencode) +"&i="+ HttpUtility.UrlEncode(LineIdencode);
            string  msg =SendMsg(jsonData, url).Result;//等待 SendMsg 方法的非同步作業結束
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] 2. 剛過SendMsg 行");
            //Console.WriteLine(jsonData);
            //Console.ReadLine();
             MsgResult MsgResult=JsonConvert.DeserializeObject<MsgResult>(msg);

            if (MsgResult.status != 200)
            {
                if (MsgResult.Message != null)
                {
                    error = MsgResult.Message;
                }
                else
                {
                    try
                    {
                        error = MsgResult.errors.ToString();
                    }
                    catch
                    {

                    }
                }
                ret = false;
             }
            return ret;
        }
        public async Task<String> SendMsg(string jsonData, string url)
        {
            StringContent data = new StringContent(jsonData, Encoding.UTF8, "application/json");
            HttpClient client = new HttpClient();
            string responseContent = "";
            //寫法1
            await Task.Run(async () =>
            {
                // 呼叫 API 並等待回應
                var response = await client.PostAsync(url, data);
                // 取得回應的內容
                responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] 3. 執行完SendMsg");
                Console.WriteLine("Response: " + responseContent);
                //return response.IsSuccessStatusCode;
                
            }).ConfigureAwait(false); //加設ConfigureAwait;
            return responseContent;

            //寫法2
            // 呼叫 API 並等待回應
            //var response = await client.PostAsync(url, data);

            //    // 取得回應的內容
            //    var responseContent = await response.Content.ReadAsStringAsync();

            //    Console.WriteLine("Response: " + responseContent);
            //    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] 3. 執行完SendMsg");
            //    //return response.IsSuccessStatusCode;
            //    return responseContent;

        }



    }
}
