using System;
using System.Drawing;
using System.IO;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;
using System.Windows.Documents;
using Color = System.Drawing.Color;
using System.Collections.Generic;

namespace Insta_DM_Bot_server_wpf
{
    internal static class Debug
    {
        public static void Log(string log)
        {
            var logText = "\n " + DateTime.Now + ": \n" + log;
            File.AppendAllText("./Log/Log.txt", logText);
        }
        public static void SendUser(string log, bool sentStatus)
        {
            var logText = "\n " + DateTime.Now + ": \n" + log + " was sent " + (sentStatus ? "succsesfully" : "unsuccsesfully");
            File.AppendAllText("./Log/Send.txt", logText);
        }
        public static void SendSuccsesfulUser(string worker , List<string> targets , string jobId)
        {
            var logText = "\n worker:" + worker + "     Job id:" + jobId;
            foreach (var item in targets)
            {
                logText += "\n" + item;
            }
            File.AppendAllText("./Log/SentSuccsesfully.txt", logText);
        }
    }
    
}
