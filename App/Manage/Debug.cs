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
using System.Linq;
using Newtonsoft.Json;

namespace Insta_DM_Bot_server_wpf
{
    internal static class Debug
    {
        public class Users
        {
            public string target;
            public bool sent;
        }
        public class LogObj
        {
            public string message;
            public DateTime time;
        }
        public class LogFile
        {
            public LogObj[] logs;
        }
        public class LogJson
        {
            public string worker, jobId;
            public List<Users> users = new();
        }
        public static void Log(string message)
        {
            File.AppendAllText("./App/Log/Log.txt" ,message );
            // var text = File.ReadAllText("./App/Log/Log.Json");
            // var json = JsonConvert.DeserializeObject<LogFile>(text);
            // json.logs.Append(new LogObj(){message = message , time = new DateTime()});
            // File.WriteAllText("./App/Log/Log.Json", JsonConvert.SerializeObject(json));
        }
        public static void SendUser(string target , string worker , string jobId, bool sentStatus)
        {
            try
            {
                var fileName = worker + "_" + jobId + ".json";
                var filePath = "./App/Log/" + fileName;
                if (!File.Exists(filePath))
                {
                    var obj = new LogJson() { worker = worker, jobId = jobId };
                    obj.users.Add(new Users() { target = target, sent = sentStatus });
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(obj));
                }
                else
                {
                    var text = File.ReadAllText(filePath);
                    var json = JsonConvert.DeserializeObject<LogJson>(text);
                    json.users.Add(new Users() { target = target, sent = sentStatus });
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(json));
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }

    }
    
}
