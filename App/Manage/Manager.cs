using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading;
using System.Windows;

namespace Insta_DM_Bot_server_wpf
{
    internal static class Manager
    {
        public static readonly Queue<ICommand> Queue = new Queue<ICommand>();
        public static List<string> GlobalMessage = new List<string>();

        //Settings
        //FE => 10000 -> 11000
        //SE => 60000 -> 240000
        // public const int WaitMin = 60000;
        // public const int WaitMax = 240000;
        public const int WaitMin = 360000;
        public const int WaitMax = 600000;
        public static int DriverCount = 2;
        public static bool IsPaused = false;
        private const int NewWindowWaitTime = 60000;

        //Connection checker
        public static bool ConnectionToNet;
        public static bool ConnectionToServer;

        //URLs
        private const string BaseUrl = "https://devbot.one2.ir/api/";
        const string FetchUrl = BaseUrl + "client/fetch";
        const string UpdateUrl = BaseUrl + "client/update";
        const string LogUrl = BaseUrl + "client/log";
        const string NetworkUrl = BaseUrl + "client/network";

        //Xpaths
        public static string xpathSaveFilePath = "./App/Xpath.bin";
        public static Xpath? xpaths = new Xpath();


        public class Task
        {
            public bool status;
            public data data;
        }

        public class data
        {
            public task task;
            public List<target> targets;
            public List<Message> messages;
        }

        public class Message
        {
            public string message;
        }
        public class task
        {
            public string uid;
            public string username;
            public string password;
        }

        public class target
        {
            public string uid;
            public string username;
            public string message;
        }

        public static async System.Threading.Tasks.Task FetchTask(bool firstTime)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    // Set the token header
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "1241");

                    // Make the GET request
                    HttpResponseMessage response = await httpClient.GetAsync(FetchUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var fetchedJob = JsonConvert.DeserializeObject<Task>(responseBody);
                        if (!fetchedJob.status) return;
                        var taskId = fetchedJob.data.task.uid;
                        string user = fetchedJob.data.task.username;
                        string pass = fetchedJob.data.task.password;
                        var targetsArray = new List<target>(fetchedJob.data.targets);
                        var messageArray = new List<Message>(fetchedJob.data.messages);
                        var waitTime = 7 * 60 * 1000;
                        if (firstTime)
                        {
                            waitTime = (Queue.Count) * NewWindowWaitTime;
                        }

                        if (waitTime < 0) waitTime = 0;
                        var newUser = new User(taskId, user, pass, targetsArray, waitTime , messageArray);
                        Queue.Enqueue(newUser);
                    }
                    else
                    {
                        MessageBox.Show("Request failed with status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }


        public static void StartSending(int count)
        {
            for (var i = 0; i < count; i++)
            {
                System.Threading.Tasks.Task.Run(
                    () =>
                    {
                        if (Queue.Count > 0)
                            Queue.Dequeue().Execute();
                    });
            }
        }

        public static bool IsConnectedToInternet()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public static bool IsConnectedToServer()
        {
            try
            {
                using var client = new WebClient();
                using (client.OpenRead(NetworkUrl))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static async void ServerLog(string uid, string status)
        {
            // Debug.SendUser(target, workerUserName, jobId.ToString(), true);
            var formData = new Dictionary<string, string>
            {
                { "uid", uid },
                { "status", status }
            };

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    // Set the token header
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "1241");

                    // Make the GET request
                    var formContent = new FormUrlEncodedContent(formData);
                    var response = await httpClient.GetAsync(LogUrl + @"?uid=" + uid + @"&status=" + status);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Request failed with status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static bool TryTilGetConnection()
        {
            var time = new TimeSpan(1, 0, 0);
            var dt = DateTime.Now + time;
            do
            {
                var isConnected = NetworkInterface.GetIsNetworkAvailable();
                Thread.Sleep(60000);
                if (!isConnected) continue;
                return DateTime.Now < dt;
            } while (true);
        }
        
        public static async void Update(string uid, string status)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    // Set the token header
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "1241");
                    var formData = new Dictionary<string, string>
                    {
                        { "taskId", uid },
                        { "status", status }
                    };
                    // Make the GET request
                    var formContent = new FormUrlEncodedContent(formData);

                    var response = await httpClient.GetAsync(UpdateUrl + "?taskid=" + uid + "&status=" + status);

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Request failed with status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static void CheckRegistration()
        {
            var credential = FileManager.ReadCredential();
            if (credential.registered == false)
            {
                MessageBox.Show("First Register Client");
            }
        }
    }
}