using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            EqInfoMessage obj = new EqInfoMessage() //造物件
            {
                EventTime = DateTime.Parse("2023-02-02 04:14:47.000"),
                BuildingName = "中興研究大樓",
                NearestEqLevel = 3,
                BuildingDamageLevel = 2,
                ImgUrl = "https://scweb.cwb.gov.tw/webdata/OLDEQ/202212/2022121004144740176_H.png",
                ImgHtmlUrl = "https://scweb.cwb.gov.tw/webdata/OLDEQ/202212/2022121004144740176_H.png",
                DamageLevelDescUrl = "https://shm.sinotech.org.tw/service/EqNotify/BuildingDamage",
                LastEqUrl = "https://shm.sinotech.org.tw/service/EqNotify/LastEarthquake",
                LineIdList = new string[] { "Uf94e6fe2a747d9381a6377abba7f06d3" },
            };

            String json = JsonConvert.SerializeObject(obj);
            HttpContent data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpClient client = new HttpClient();
            String url = "https://shm.sinotech.org.tw/service/api/LineBot/EqNotify";
            HttpResponseMessage response = await client.PostAsync(url, data);
            String result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
            Console.WriteLine(response);

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
            public String[] LineIdList { get; set; }
        }
    }
}
