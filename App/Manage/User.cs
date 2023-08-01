using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Insta_DM_Bot_server_wpf
{
    internal class User : ICommand
    {
        private ChromeDriver? _driver;
        public WorkerAssigned WorkerAssigned;
        public Worker? Worker;
        private readonly int _waitTime;

        private readonly string _username;
        private readonly string _password;
        private readonly List<string> _users;
        private readonly int _jobId;
        private readonly List<string?> _messages;
        private string? _messageTemp;
        private readonly dynamic _targets;

        public bool isDead;
        private static ErrorCode errCode = ErrorCode.FreeWorker;

        private int _tryTimes;
        private int SomethingWentWrongTimes;
        private bool _successful = true;
        private bool netDisconnected;
        private bool gotBanned;
        private List<string> _userTemp = new List<string>();


        public User(int jobId, string username, string password, List<string> targets, List<string?> messages,
            dynamic jsonTargets, int waitTime, WorkerAssigned workerAssigned)
        {
            _username = username;
            _password = password;
            _users = targets;
            _messages = messages;
            _jobId = jobId;
            this._targets = jsonTargets;
            _waitTime = waitTime;
            WorkerAssigned = workerAssigned;
            switch (WorkerAssigned)
            {
                case WorkerAssigned.Worker1:
                    Worker = Manager.worker1;
                    break;
                case WorkerAssigned.Worker2:
                    Worker = Manager.worker2;
                    break;
                case WorkerAssigned.Worker3:
                    Worker = Manager.worker3;
                    break;
                case WorkerAssigned.Worker4:
                    Worker = Manager.worker4;
                    break;
            }
        }


        //Timer stuff
        private Timer _timer;
        const float TimerInterval = 9;
        private bool hasSuccsesfulDirect = false;

        private void TimerInitialize()
        {
            if (_timer != null) _timer.Dispose();
            _timer = new Timer(9 * 60000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = true;
            _timer.AutoReset = true;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs args)
        {
            if (isDead) return;
            if (gotBanned)
            {
                _timer.Stop();
                _timer.Dispose();
                return;
            }

            if (!hasSuccsesfulDirect && !netDisconnected)
            {
                isDead = true;
                _timer.Stop();
                _timer.Dispose();
                StartNewDriver();
                errCode = ErrorCode.Timer;
            }

            hasSuccsesfulDirect = false;
            if (!Manager.IsConnectedToInternet())
            {
                Manager.TryTilGetConnection();
            }
        }

        public bool Execute()
        {
            Thread.Sleep(_waitTime);
            _driver = new ChromeDriver();
            _driver.Manage().Window.Size = new Size(516, 703);
            TimerInitialize();
            if (!SignIn(_username, _password))
            {
                StartNewDriver();
                return false;
            }

            if (isDead) return false;
            Worker.Username = _username;
            if (!PrepareForSendDirects())
            {
                StartNewDriver();
                return false;
            }

            if (isDead) return false;

            if (!SendMessage(_users.ToArray(), _messages))
            {
                StartNewDriver();
                return false;
            }

            if (isDead) return false;

            Manager.DestroyWorker(WorkerAssigned);
            _driver.Quit();
            if (!Manager.SendWorkerEnd(_username, _jobId, _userTemp)) return false;

            if (Manager.IsPaused) return _successful;
            if (isDead) return false;
            isDead = true;
            Manager.GetUserFromServer(false);
            Thread.Sleep(5000);
            if (Manager.Queue.Count > 0)
            {
                Manager.Queue.Dequeue().Execute();
            }

            return _successful;
        }

        public void StartNewDriver()
        {
            _driver?.Quit();
            if (!gotBanned)
                Manager.CancelWorker(_targets, _username, _password, _jobId.ToString(), WorkerAssigned, errCode);
            isDead = true;
            Manager.DestroyWorker(WorkerAssigned);
            if (Manager.IsPaused) return;
            Thread.Sleep(2000);
            Manager.GetUserFromServer(true);
            Thread.Sleep(5000);
            Manager.StartSending(1);
        }


        private bool SignIn(string username, string password)
        {
            SignIn:
            try
            {
                _driver?.Navigate().GoToUrl("https://www.instagram.com/accounts/login/");
            }
            catch (WebDriverException)
            {
                netDisconnected = true;
                if (Manager.TryTilGetConnection())
                {
                    _driver?.Navigate().Refresh();
                    goto SignIn;
                }

                return false;
            }

            Thread.Sleep(5000);
            Login:

            //Allow Cookies Button 
            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Allow all cookies')]")).Click();
            }
            catch (NoSuchElementException)
            {
                //Not Important
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            Thread.Sleep(5000);
            IWebElement? usernameInput;
            IWebElement? passwordInput;
            try
            {
                usernameInput =
                    _driver?.FindElement(By.Name("username"));
            }
            catch (NoSuchElementException)
            {
                return false;
            }

            try
            {
                passwordInput =
                    _driver?.FindElement(By.Name("password"));
            }
            catch (NoSuchElementException)
            {
                return false;
            }

            Thread.Sleep(2000);
            usernameInput?.SendKeys(username);
            passwordInput?.SendKeys(password);
            Thread.Sleep(1000);
            passwordInput?.Submit();
            Thread.Sleep(5000);
            try
            {
                var errorText = _driver?.FindElement(By.Id("slfErrorAlert")).Text;
                Thread.Sleep(5000);
                if (errorText.Contains("username"))
                {
                    errCode = ErrorCode.UserProb;
                }
                else if (errorText.Contains("password"))
                {
                    errCode = ErrorCode.PassProb;
                }
                else if (errorText.Contains("connect") || errorText.Contains("problem"))
                {
                    errCode = ErrorCode.FreeWorker;
                }
                else if (errorText.Contains("terms"))
                {
                    Manager.BanUser(username, _jobId);
                    gotBanned = true;
                    return false;
                }

                return false;
            }
            catch (WebDriverTimeoutException)
            {
                netDisconnected = true;

                if (Manager.TryTilGetConnection())
                {
                    _driver?.Navigate().Refresh();
                    goto Login;
                }

                return false;
            }
            catch (Exception)
            {
                //Nothing
            }


            //Not Now Button
            try
            {
                _driver?.FindElement(By.CssSelector(".xjqpnuy")).Click();
            }
            catch (NoSuchElementException)
            {
                //Not Important
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Thread.Sleep(5000);
//Notification Not now button
            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Not Now')]")).Click();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Thread.Sleep(5000);
            // Inbox button -> 
            // _driver?.FindElement(By.CssSelector(@".x1o5bo1o > .\_ab6-")).Click();
            
            if (_driver.Url.Contains("challenge"))
            {
                Manager.BanUser(username, _jobId);
                gotBanned = true;
                return false;
            }

            return true;
        }

        private bool PrepareForSendDirects()
        {
            Prepare:
            Thread.Sleep(5000);
            if (!Manager.IsConnectedToInternet())
            {
                netDisconnected = true;

                if (Manager.TryTilGetConnection())
                {
                    _driver?.Navigate().Refresh();
                    goto Prepare;
                }

                errCode = ErrorCode.FreeWorker;
                return false;
            }

            try
            {
                _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
            }
            catch
            {
                goto Prepare;
            }

            Thread.Sleep(5000);
            return true;
        }

        private bool SendMessage(string[] targets, List<string?> message)
        {
            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Not Now')]")).Click();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Thread.Sleep(5000);
            for (var i = 0; i < targets.Length; i++)
            {
                ClickNewDirect:
                //New Direct Button
                _driver?.FindElement(By.XPath("/html/body/div[2]/div/div/div[2]/div/div/div/div[1]/div[1]/div[2]/section/div/div/div/div[1]/div/div[1]/div/div[1]/div")).Click();

                Thread.Sleep(1000);

                //Search bar
                _driver?.FindElement(By.Name("queryBox")).SendKeys(targets[i]);

                Thread.Sleep(5000);
                //.x1cy8zhl css
                _driver?.FindElement(By.CssSelector(@".x1i10hfl:nth-child(1) .x1lliihq > circle")).Click();

                Thread.Sleep(1000);

                //.xt0psk2 css
                _driver?.FindElement(By.CssSelector(@".xn3w4p2")).Click();

                Thread.Sleep(5000);

                var random = new Random();

                IWebElement? textField = null;
                Thread.Sleep(5000);

                try
                {
                    textField = _driver?.FindElement(By.XPath("//p"));
                }
                catch (WebDriverTimeoutException)
                {
                    netDisconnected = true;
                    if (Manager.TryTilGetConnection())
                    {
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }

                    errCode = ErrorCode.Halfway;
                    return false;
                }
                catch (Exception e)
                {
                    try
                    {
                        textField = _driver?.FindElement(By.XPath(
                            "/html/body/div[1]/div/div/div/div[1]/div/div/div/div[1]/div[1]/section/div/div[2]/div/div/div[2]/div[2]/div/div[2]/div/div/div[2]/textarea"));
                    }
                    catch
                    {
                        Debug.Log(e.Message);
                        if (SomethingWentWrongTimes < 3)
                        {
                            Manager.FailedSending(_users[i], _username, _jobId);
                            PrepareForSendDirects();
                            SomethingWentWrongTimes++;
                            continue;
                        }
                        else
                        {
                            errCode = ErrorCode.SWW;
                            return false;
                        }
                    }
                }

                try
                {
                    SomethingWentWrongTimes = 0;
                    textField?.SendKeys(message[random.Next(0, message.Count)]);
                    Thread.Sleep(1000);
                    textField?.SendKeys(Keys.Enter);
                    hasSuccsesfulDirect = true;
                    _tryTimes = 0;

                    Manager.ChangeTargetStatusInServer(targets[i], _username, _jobId);
                    _userTemp.Add(targets[i]);
                    // Worker?.SetLastSend(targets[i] + " _ " + DateTime.Now.ToString("HH:mm:ss"));
                    PrepareForSendDirects();
                    Thread.Sleep(random.Next(Manager.WaitMin, Manager.WaitMax));
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
            }

            return true;
        }

        public string GetDescription()
        {
            return "User: " + _username + " | pass: " + _password + " | Users: " + _users.Count;
        }
    }
}