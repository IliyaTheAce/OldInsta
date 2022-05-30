using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace Insta_DM_Bot_server_wpf
{

    internal static class Manager
    {
        public const int WaitMin = 60000;
        public const int WaitMax = 240000;

        public static readonly Queue<ICommand> Queue = new Queue<ICommand>();

        public static int DriverCount;
        public static List<string> GlobalMessage = new List<string>();
        public static bool IsPaused = false;
        public static string? PackageId;
        public static bool ConnectionToNet;
        public static bool ConnectionToServer;
        private const int NewWindowWaitTime = 60000;

        public static void GetUserFromServer()
        {
            var webClient = new WebClient();
            var result = webClient.DownloadString("https://ranande.ir/api/fetchJob");

            dynamic stuff = JsonConvert.DeserializeObject(result);
            if (stuff.status != "true") return;
            int jobId = stuff.jobId;
            string user = stuff.worker.username;
            string pass = stuff.worker.password;

            var targets = new List<string>();
            var messages = new List<string?>();
            foreach (var target in stuff.targets)
            {
                targets.Add((string)target);
            }

            foreach (var message in stuff.messages)
            {
                var htmlText = (string)message.text;
                var text = htmlText.Replace("/<[^>] +>/ g", "");
                text = text.Replace("<br>", "\n");
                messages.Add(text);
            }
            PackageId = stuff.packageId;
            var waitTime = (Queue.Count) * NewWindowWaitTime;
            if (waitTime < 0) waitTime = 0;
            var newUser = new User(jobId, user, pass, targets, messages, stuff.targets, waitTime);
            Manager.Queue.Enqueue(newUser);
        }

        public static void StartSending()
        {
            for (var i = 0; i < DriverCount; i++)
            {
                Task.Run(
                    () => {
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
                using (client.OpenRead("https://ranande.ir"))
                    return true;
            }
            catch
            {
                return false;
            }

        }

        public static void ChangeTargetStatusInServer(string target, string workerUserName, int jobId)
        {

            var client = new WebClient();
            client.QueryString.Add("target", target);
            client.QueryString.Add("packageId", PackageId);
            client.QueryString.Add("worker", workerUserName);
            client.QueryString.Add("job", jobId.ToString());
            client.DownloadString("https://ranande.ir/api/sendDirect");
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

        public static void CancelWorker(dynamic targets, string? packageId, string worker, string jobId)
        {
            var client = new WebClient();
            var res = JsonConvert.SerializeObject(targets);
            client.QueryString.Add("target", res);
            client.QueryString.Add("packageId", packageId);
            client.QueryString.Add("worker", worker);
            client.QueryString.Add("jobId", jobId);

            client.DownloadString("https://ranande.ir/api/cancleJob");
        }

        public static void FailedSending(string failedTarget, string worker, string? packageId)
        {
            var client = new WebClient();
            client.QueryString.Add("target", failedTarget);
            client.QueryString.Add("packageId", packageId);
            client.QueryString.Add("worker", worker);
            client.DownloadString("https://ranande.ir/api/jumpTarget");
        }
    }
}
