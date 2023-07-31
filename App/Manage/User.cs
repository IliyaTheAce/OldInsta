using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
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
        private int SomethingWenWrongTimes;
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
                case WorkerAssigned.Worker1: Worker = Manager.worker1; break;
                case WorkerAssigned.Worker2: Worker = Manager.worker2; break;
                case WorkerAssigned.Worker3: Worker = Manager.worker3; break;
                case WorkerAssigned.Worker4: Worker = Manager.worker4; break;
            }
        }



        //Timer stuff
        private Timer _timer;
        const float TimerInterval = 9;
        private bool hasSuccsesfulDirect = false;
        private void TimerInitialize()
        {
            if(_timer != null) _timer.Dispose();
            _timer = new Timer(9 * 60000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = true;
            _timer.AutoReset = true;

        }

        private void TimerElapsed(object sender , ElapsedEventArgs args)
        {
            if(isDead) return;
            if(gotBanned)
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
            _driver.Manage().Window.Maximize();
            TimerInitialize();
            if (!SignIn(_username, _password))
            {
                StartNewDriver();
                return false;
            }
            if (isDead) return false ;
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
                Manager.CancelWorker(_targets , _username, _password, _jobId.ToString(), WorkerAssigned, errCode);
            isDead = true;
            Manager.DestroyWorker(WorkerAssigned);
            if (Manager.IsPaused) return;
            Thread.Sleep(2000);
            Manager.GetUserFromServer(true);
            Thread.Sleep(10000);
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
            
            Login:
            try
            {
                _driver?.FindElement(By.XPath("/html/body/div[4]/div/div/button[2]")).Click();
            }
            catch (NoSuchElementException)
            {
                //
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
            Thread.Sleep(10000);
            IWebElement? usernameInput;
            IWebElement? passwordInput;
            try
            {
                usernameInput =
                _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/div/div/div[1]/div[2]/form/div/div[1]/div/label/input"));

            }
            catch (NoSuchElementException)
            {
                try
                {
                    usernameInput =
                    _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/article/div[2]/div[1]/div[2]/form/div/div[1]/div/label/input"));
                }
                catch
                {
                    try
                    {
                        usernameInput =
                        _driver?.FindElement(By.XPath("/html/body/div[1]/div/div/section/main/div/div/div[1]/div[2]/form/div/div[1]/div/label/input"));
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            try
            {
                passwordInput =
                  _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/div/div/div[1]/div[2]/form/div/div[2]/div/label/input"));

            }
            catch (NoSuchElementException)
            {
                try
                {
                    passwordInput =
                    _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/article/div[2]/div[1]/div[2]/form/div/div[2]/div/label/input"));
                }
                catch
                {
                    try
                    {
                        passwordInput =
                        _driver?.FindElement(By.XPath("/html/body/div[1]/div/div/section/main/div/div/div[1]/div[2]/form/div/div[2]/div/label/input"));
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            Thread.Sleep(2000);
            usernameInput?.SendKeys(username);
            passwordInput?.SendKeys(password);
            Thread.Sleep(1000);
            passwordInput?.Submit();
            Thread.Sleep(10000);

            Thread.Sleep(10000);
            try
            {
               var errorText =  _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/div/div/div[1]/div[2]/form/div[2]/p")).Text;
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

            foreach (var xpath in Manager.xpaths.saveInfo)
            {
                try
                {
                    _driver?.FindElement(By.XPath(xpath)).Click();
                    Thread.Sleep(500);
                }
                catch
                {
                    //ignored
                }
            }
            Thread.Sleep(10000);
            foreach (var xpath in Manager.xpaths.notification)
            {
                try
                {
                    _driver?.FindElement(By.XPath(xpath)).Click();
                    Thread.Sleep(500);
                }
                catch
                {
                    //ignored
                }
            }
        
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
            Thread.Sleep(10000);
            if (!Manager.IsConnectedToInternet())
            {
                netDisconnected = true;

                if (Manager.TryTilGetConnection()) { 
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
            Thread.Sleep(10000);
            return true;
        }

        private bool SendMessage(string[] targets, List<string?> message)
        {

            for (var i = 0; i < targets.Length; i++)
            {
                ClickNewDirect:
                foreach(var xpath in Manager.xpaths.newDirect)
                {
                    try
                    {
                        _driver?.FindElement(By.XPath(xpath)).Click();

                        Thread.Sleep(500);
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
                    catch
                    {
                        //ignored
                    }
                }
                var byPassTargetInput = false;
                var failedTimes = 0;
                try
                {
                     _driver?.FindElement(By.ClassName("_aaie  _aaid _aaiq")).SendKeys(targets[i]);
                    byPassTargetInput = true;
                }
                catch
                {
                    //ignored
                }
                if (!byPassTargetInput)
                {
                    try
                    {
                        _driver?.FindElement(By.Name("queryBox")).SendKeys(targets[i]);
                        byPassTargetInput = true;
                    }
                    catch
                    {
                        //ignored
                    }
                }
                if (!byPassTargetInput)
                {
                    try
                    {

                        _driver?.FindElement(By.XPath("/html/body/div[5]/div/div/div[2]/div[1]/div/div[2]/input")).SendKeys(targets[i]);
                        byPassTargetInput = true;
                    }
                    catch
                    {
                        //ignored
                    }
                }
                if (!byPassTargetInput)
                {
                    try
                    {
                        _driver?.FindElement(By.XPath(
          "/html/body/div[1]/div/div[1]/div/div[2]/div/div/div[1]/div/div[2]/div/div/div/div/div/div/div[2]/div[1]/div/div[2]/input")).SendKeys(targets[i]);
                        byPassTargetInput = true;
                    }
                    catch
                    {
                        //ignored
                    }
                }

                if (!byPassTargetInput)
                {
                    if (failedTimes >= 4)
                    {
                        Manager.FailedSending(_users[i], _username, _jobId);
                        PrepareForSendDirects();
                        continue;
                    }
                    else
                    {
                        failedTimes++;
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }
                }
                    Thread.Sleep(10000);
                var byPassSelecting = false;
                try
                {
                    _driver?.FindElement(By.XPath("/html/body/div[5]/div/div/div[2]/div[2]/div[1]")).Click();
                    Thread.Sleep(500);
                    byPassSelecting = true;
                }
                catch (WebDriverTimeoutException)
                {
                    netDisconnected = true;
                    if (Manager.TryTilGetConnection()) { 
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }
                    errCode = ErrorCode.Halfway;

                    return false;
                }
                catch (NoSuchElementException) { //ignored
                                                 }
                catch (Exception)
                {
                    if (!Manager.IsConnectedToInternet())
                    {
                        netDisconnected = true;
                        if (Manager.TryTilGetConnection()) { 
                            _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                            goto ClickNewDirect;
                        }
                        errCode = ErrorCode.Halfway;
                        return false;
                    }
                    Thread.Sleep(10000);
                }
                if (!byPassSelecting)
                {
                    try
                    {
                        _driver?.FindElement(By.XPath("/html/body/div[1]/div/div[1]/div/div[2]/div/div/div[1]/div/div[2]/div/div/div/div/div/div/div[2]/div[2]/div")).Click();
                        Thread.Sleep(500);
                        byPassSelecting = true;
                    }
                    catch (NoSuchElementException)
                    {
                        //ignored
                    }
                    catch (Exception)
                    {
                        if (!Manager.IsConnectedToInternet())
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
                        Thread.Sleep(10000);
                    }
                }
                if (!byPassSelecting)
                {
                    try
                    {
                        _driver?.FindElement(By.XPath("/html/body/div[5]/div/div/div[2]/div[2]/div[1]/div/div[2]/div/div")).Click();
                        Thread.Sleep(500);
                        byPassSelecting = true;
                    }
                    catch (NoSuchElementException)
                    {
                        //ignored
                    }
                    catch (Exception)
                    {
                        if (!Manager.IsConnectedToInternet())
                        {
                            netDisconnected = true;
                            if (Manager.TryTilGetConnection())
                            {
                                _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                                goto ClickNewDirect;
                            }
                            return false;
                        }
                        Thread.Sleep(10000);
                    }
                }
                if (!byPassSelecting)
                {
                    try
                    {

                        _driver?.FindElement(By.XPath("/html/body/div[5]/div/div/div[2]/div[2]/div[1]")).Click();
                        Thread.Sleep(500);
                        byPassSelecting = true;
                    }
                    catch (NoSuchElementException)
                    {
                        //ignored
                    }
                    catch (Exception)
                    {
                        if (!Manager.IsConnectedToInternet())
                        {
                            netDisconnected = true;
                            if (Manager.TryTilGetConnection())
                            {
                                _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                                goto ClickNewDirect;
                            }
                            return false;
                        }
                        Thread.Sleep(10000);
                    }
                }
                if (!byPassSelecting)
                {
                    try
                    {
                        var buttom = RelativeBy.WithLocator(By.ClassName("-qQT3")).Below(By.ClassName("TGYkm"));
                        _driver?.FindElement(buttom).Click();
                        Thread.Sleep(500);
                        byPassSelecting = true;
                    }
                    catch (NoSuchElementException)
                    {
                        //ignored
                    }
                    catch (Exception)
                    {
                        if (!Manager.IsConnectedToInternet())
                        {
                            netDisconnected = true;
                            if (Manager.TryTilGetConnection())
                            {
                                _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                                goto ClickNewDirect;
                            }
                            return false;
                        }
                        Thread.Sleep(10000);
                    }
                }

                var byPassNextButtom = false;
                IWebElement? newDirectNextButtom;
            ClickDirect:

                    foreach (var xpath in Manager.xpaths.NextButtom)
                    {
                        try
                        {
                            _driver?.FindElement(By.XPath(xpath)).Click();
                            byPassNextButtom = true;
                            Thread.Sleep(500);
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
                        catch
                        {
                            //ignored
                        }
                    }
                
                if (_tryTimes >= 6)
                {
                    Manager.FailedSending(_users[i], _username, _jobId);
                    PrepareForSendDirects();
                    continue;
                }
                if (!byPassNextButtom)
                {
                    _tryTimes++;
                    Thread.Sleep(10000);
                    goto ClickDirect;
                }
                _tryTimes = 0;
                Thread.Sleep(5000);

                var random = new Random();

                IWebElement? textField = null;
                Thread.Sleep(10000);

                try
                {
                    textField = _driver?.FindElement(By.TagName("textarea"));
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
                        textField = _driver?.FindElement(By.XPath("/html/body/div[1]/div/div/div/div[1]/div/div/div/div[1]/div[1]/section/div/div[2]/div/div/div[2]/div[2]/div/div[2]/div/div/div[2]/textarea"));
                    }
                    catch
                    {
                        Debug.Log(e.Message);
                        if (SomethingWenWrongTimes < 3)
                        {
                            Manager.FailedSending(_users[i], _username, _jobId);
                            PrepareForSendDirects();
                            SomethingWenWrongTimes++;
                            continue;
                        }
                        else
                        {
                            errCode = ErrorCode.SWW;
                            return false;
                        }
                    }
                }
                SomethingWenWrongTimes = 0;
                textField?.SendKeys(message[random.Next(0, message.Count - 1)]);
                Thread.Sleep(1000);
                textField?.SendKeys(Keys.Enter);

                hasSuccsesfulDirect = true;
                _tryTimes = 0;

                Manager.ChangeTargetStatusInServer(targets[i], _username, _jobId);
                _userTemp.Add(targets[i]);
                Worker?.SetLastSend(targets[i] + " _ " + DateTime.Now.ToString("HH:mm:ss"));
                PrepareForSendDirects();
                Thread.Sleep(random.Next(Manager.WaitMin, Manager.WaitMax));
            }
            return true;
        }

        public string GetDescription()
        {
            return "User: " + _username + " | pass: " + _password + " | Users: " + _users.Count;
        }


    }
}
