using System;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

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

        public class MsgResult
        {
            public int status { get; set; }
            public String Message { get; set; }
            public bool? data { get; set; }
            public JObject errors { get; set; }
        }
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
            public String DamageLevelUrl { get; set; }
            public String[] LineIdList { get; set; }
        }
        public class FreqInfoMessage
        {
            public DateTime EventTime { get; set; }
            public String BuildingName { get; set; }
            public double IdentifyFreq { get; set; } 
            public double MinRange { get; set; }
            public double MaxRange { get; set; }
            public String LastFreqUrl { get; set; }
            public String[] LineIdList { get; set; }
        }
        public class EqFreqInfoMessage
        {
            public DateTime EventTime { get; set; }
            public String BuildingName { get; set; }
            public double EqFreqBefore { get; set; }
            public DateTime EqFreqBeforeTime { get; set; }
            public double EqFreqAfter { get; set; }
            public DateTime EqFreqAfterTime { get; set; }
            public double MinRange { get; set; }
            public double MaxRange { get; set; }
            public String LastFreqUrl { get; set; }
            public String[] LineIdList { get; set; }
        }
        public bool Send(int type, int notifyMethod, string warnMessage,  out string error)
        {
            
            bool ret = true;
            error = "";
            switch (notifyMethod)
            {

                case 3:
                    ret = SendLine(type,warnMessage, out error);
                    break;

            }
            return ret;
        }


        private bool SendLine(int type,string warnMessage, out string error)
        {
            error = "";
            bool ret = true;
            byte[] bytes;
            String url ="";
            string LineIdEncode;
            switch (type)
            {
                case 3:
                    url = "https://shm.sinotech.org.tw/service/api/LineBot/FreqAlarm";
                    FreqInfoMessage obj_freq = JsonConvert.DeserializeObject<FreqInfoMessage>(warnMessage);
                    bytes = Encoding.GetEncoding("utf-8").GetBytes(obj_freq.LineIdList[0]);
                    LineIdEncode = Convert.ToBase64String(bytes);
                    obj_freq.LastFreqUrl = string.Format(
                        "{0}?n={1}&i={2}",
                        obj_freq.LastFreqUrl,
                        HttpUtility.UrlEncode(obj_freq.BuildingName),
                        HttpUtility.UrlEncode(LineIdEncode)
                    );
                    warnMessage = JsonConvert.SerializeObject(obj_freq);
                 break;
                case 4:
                    url = "https://shm.sinotech.org.tw/service/api/LineBot/FreqByEq";
                    EqFreqInfoMessage obj_Eqfreq = JsonConvert.DeserializeObject<EqFreqInfoMessage>(warnMessage);
                    bytes = Encoding.GetEncoding("utf-8").GetBytes(obj_Eqfreq.LineIdList[0]);
                    LineIdEncode = Convert.ToBase64String(bytes);
                    obj_Eqfreq.LastFreqUrl = string.Format(
                        "{0}?n={1}&i={2}",
                        obj_Eqfreq.LastFreqUrl,
                        HttpUtility.UrlEncode(obj_Eqfreq.BuildingName),
                        HttpUtility.UrlEncode(LineIdEncode)
                    );
                    warnMessage = JsonConvert.SerializeObject(obj_Eqfreq);
                    break;
            }
            string msg = SendMsg(warnMessage, url).Result;//傳送資料，並取回httpclient之結果值
            MsgResult MsgResult = JsonConvert.DeserializeObject<MsgResult>(msg);
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
                    catch (Exception e)
                    {
                        error = e.Message;
                    }
                }
                ret = false;
            }
            Thread.Sleep(2000);
            return ret;
        }
        public async Task<string>SendMsg(string jsonData, string url)
        {
            StringContent data = new StringContent(jsonData, Encoding.UTF8, "application/json");
            HttpClient client = new HttpClient();
            string responseContent = "";
            await Task.Run(async () => {
                // 呼叫 API 並等待回應
                HttpResponseMessage response = await client.PostAsync(url, data);
                // 取得回應的內容
                 responseContent = await response.Content.ReadAsStringAsync();

            }).ConfigureAwait(false); ;
            return responseContent;
            //return response.IsSuccessStatusCode;
        }
    }
}
