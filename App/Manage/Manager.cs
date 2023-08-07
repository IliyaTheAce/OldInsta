using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
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
        const string fetchJob = "https://instagram.one2.ir/api/fetchJob";
        const string sendDirect = "https://instagram.one2.ir/api/sendDirect";
        const string cancelJob = "https://instagram.one2.ir/api/cancleJob";
        const string jumpTarget = "https://instagram.one2.ir/api/jumpTarget";
        const string checkNet = "https://instagram.one2.ir";
        const string Error = "https://instagram.one2.ir/api/error";
        const string challange = "https://instagram.one2.ir/api/challeng";
        const string SuccesfulWorker = "https://instagram.one2.ir/api/reportJob";

        //Xpaths
        public static string xpathSaveFilePath = "./App/Xpath.bin";
        public static Xpath? xpaths = new Xpath();


        public static void GetUserFromServer(bool firstTime)
        {
            try
            {
                var webClient = new WebClient();
                var result = webClient.DownloadString(fetchJob);

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

                var waitTime = 7 * 60 * 1000;
                if (firstTime)
                {
                    waitTime = (Queue.Count) * NewWindowWaitTime;
                }

                if (waitTime < 0) waitTime = 0;
                var newUser = new User(jobId, user, pass, targets, messages, stuff.targets, waitTime);
                Manager.Queue.Enqueue(newUser);
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
                Task.Run(
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
                using (client.OpenRead(checkNet))
                    return true;
            }
            catch
            {
                return false;
            }

        }

        public static void ChangeTargetStatusInServer(string target, string workerUserName, int jobId)
        {
            Debug.SendUser(target, workerUserName, jobId.ToString(), true);

            try
            {
                var get = sendDirect + "?target=" + target + "&worker=" + workerUserName + "&job=" + jobId;
                var client = new WebClient();
                client.QueryString.Add("target", target);
                client.QueryString.Add("worker", workerUserName);
                client.QueryString.Add("job", jobId.ToString());
                client.DownloadString(sendDirect);
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

        public static void CancelWorker(dynamic targets, string worker, string password, string jobId,
            ErrorCode errCode)
        {
            try
            {
                var client = new WebClient();
                var res = JsonConvert.SerializeObject(targets);
                client.QueryString.Add("target", res);
                client.QueryString.Add("worker", worker);
                client.QueryString.Add("jobId", jobId);
                client.QueryString.Add("errCode", errCode.GetHashCode().ToString());
                client.QueryString.Add("password", password);
                client.DownloadString(cancelJob);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static void FailedSending(string failedTarget, string worker, int jobId)
        {
            Debug.SendUser(failedTarget, worker, jobId.ToString(), false);


            try
            {
                var client = new WebClient();
                client.QueryString.Add("target", failedTarget);
                client.QueryString.Add("worker", worker);
                client.QueryString.Add("jobId", jobId.ToString());
                client.DownloadString(jumpTarget);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static void SendError(string exception, string worker, int jobId)
        {
            try
            {
                var client = new WebClient();
                client.QueryString.Add("error", exception);
                client.QueryString.Add("worker", worker);
                client.QueryString.Add("jobId", jobId.ToString());
                client.DownloadString(Error);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static void BanUser(string worker, int jobId)
        {
            try
            {
                var client = new WebClient();
                client.QueryString.Add("worker", worker);
                client.QueryString.Add("jobId", jobId.ToString());
                client.DownloadString(challange);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static void LoadXpaths()
        {
            var stuff = FileManager.ReadFromBinaryFile<Xpath>(xpathSaveFilePath);
            xpaths = stuff;

        }

        public static void UpdateXpaths()
        {
            Console.WriteLine("Started updateing");
            try
            {
                xpaths = new Xpath();
                var webClient = new WebClient();
                var result = webClient.DownloadString("https://instagram.one2.ir/api/xPath");

                dynamic stuff = JsonConvert.DeserializeObject(result);

                foreach (var xpath in stuff)
                {
                    switch (xpath.section.ToString())
                    {
                        case "username":
                            xpaths.username.Add(xpath.xPath.ToString());
                            break;
                        case "password":
                            xpaths.password.Add(xpath.xPath.ToString());
                            break;
                        case "ErrorText":
                            xpaths.ErrorText.Add(xpath.xPath.ToString());
                            break;
                        case "saveInfo":
                            xpaths.saveInfo.Add(xpath.xPath.ToString());
                            break;
                        case "notification":
                            xpaths.notification.Add(xpath.xPath.ToString());
                            break;
                        case "newDirect":
                            xpaths.newDirect.Add(xpath.xPath.ToString());
                            break;
                        case "targetInput":
                            xpaths.targetInput.Add(xpath.xPath.ToString());
                            break;
                        case "selectUser":
                            xpaths.selectUser.Add(xpath.xPath.ToString());
                            break;
                        case "NextButtom":
                            xpaths.NextButtom.Add(xpath.xPath.ToString());
                            break;
                        case "TextArea":
                            xpaths.TextArea.Add(xpath.xPath.ToString());
                            break;
                        case "allowCookies":
                            xpaths.allowCookies.Add(xpath.xPath.ToString());
                            break;
                    }
                }

                FileManager.WriteToBinaryFile<Xpath>(xpathSaveFilePath, xpaths, true);
                Console.WriteLine("Xpathes updated");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                LoadXpaths();
            }
        }

        public static bool SendWorkerEnd(string worker, int jobId, List<string> targets)
        {
            var succ = true;
            int tryTimes = 0;
            do
            {
                try
                {
                    var client = new WebClient();
                    var res = JsonConvert.SerializeObject(targets);
                    var result = client.DownloadString("https://instagram.one2.ir/api/reportJob?jobId=" + jobId +
                                                       "&worker=" + worker + "&targets=" + res);
                    return result.Contains("true");
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                    tryTimes++;
                    if (tryTimes > 6)
                    {
                        succ = false;
                    }
                }

                Thread.Sleep(50000);
            } while (succ);

            MainWindow.ShowMessage("Couldn't send request to server!", " Internal error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return false;
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
