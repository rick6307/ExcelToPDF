using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            int notifyMethod = 3;
            string warnMessage = "111";
            string mobilePhone = "111";
            string email = "111";
            string name = "111";
            string title = "111";
            string output;
            Alarm.Alarm alarm = new Alarm.Alarm();
            bool truefalse = alarm.Send(1, notifyMethod, warnMessage, mobilePhone, email, name, title, out output);
        }
    }
}
