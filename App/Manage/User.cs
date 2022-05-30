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
        private readonly string _username;
        private readonly string _password;
        private readonly List<string> _users;
        private readonly int _jobId;
        private readonly List<string?> _messages;
        private int _tryTimes;
        private bool _firstMessage;
        private readonly dynamic _targets;
        private readonly int _waitTime;
        private Timer? _timer;
        private string? _messageTemp;
        private bool _successful = true;
        public User(int jobId, string username, string password, List<string> targets, List<string?> messages,
            dynamic jsonTargets, int waitTime)
        {
            _username = username;
            _password = password;
            _users = targets;
            _messages = messages;
            _jobId = jobId;
            this._targets = jsonTargets;
            _waitTime = waitTime;
        }
        private ChromeDriver? _driver;

        public bool Execute()
        {
            Thread.Sleep(_waitTime);
            _driver = new ChromeDriver();
            _timer = new Timer();
            _timer.Elapsed += Destroy;
            _timer.Interval = 360000;
            _timer.Enabled = true;
            _timer.AutoReset = true;
            _timer.Start();

            if (!SignIn(_username, _password))
            {

                _driver.Quit();
                Manager.CancelWorker(_targets, Manager.PackageId, _username, _jobId.ToString());
                Thread.Sleep(2000);
                Manager.GetUserFromServer();
                Thread.Sleep(10000);
                if (Manager.Queue.Count > 0)
                {
                    Manager.Queue.Dequeue().Execute();
                }
                return false;
            }
            PrepareForSendDirects();

            SendMessage(_users.ToArray(), _messages);
            if (!Manager.IsPaused) return _successful;

            Manager.GetUserFromServer();
            Thread.Sleep(5000);
            if (Manager.Queue.Count > 0)
            {
                Manager.Queue.Dequeue().Execute();
            }
            return _successful;
        }

        private void Destroy(object? sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (!_firstMessage)
                _driver?.Close();
            _firstMessage = false;
            if (!Manager.IsConnectedToInternet())
            {
                Manager.TryTilGetConnection();
            }
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
                if (Manager.TryTilGetConnection())
                {
                    _driver?.Navigate().Refresh();
                    goto SignIn;
                }

                Manager.CancelWorker(_targets, Manager.PackageId, _username, _jobId.ToString());
                Manager.GetUserFromServer();
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
                usernameInput =
                _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/article/div[2]/div[1]/div[2]/form/div/div[1]/div/label/input"));
                ///html/body/div[1]/div/div/section/main/div/div/div[1]/div[2]/form/div/div[1]/div/label/input
            }
            try
            {
                passwordInput =
                  _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/div/div/div[1]/div[2]/form/div/div[2]/div/label/input"));

            }
            catch (NoSuchElementException)
            {
                passwordInput =
                _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/article/div[2]/div[1]/div[2]/form/div/div[2]/div/label/input"));
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
                _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/div/div/div[1]/div[2]/form/div[2]/p"));
                Thread.Sleep(5000);

                return false;
            }
            catch (WebDriverTimeoutException)
            {
                if (Manager.TryTilGetConnection())
                {
                    _driver?.Navigate().Refresh();
                    goto Login;
                }

                Manager.CancelWorker(_targets, Manager.PackageId, _username, _jobId.ToString());
                Manager.GetUserFromServer();
            }
            catch (Exception)
            {
                //Nothing
            }

            try
            {
                _driver?.FindElement(By.XPath("/html/body/div[1]/section/main/div/div/div/div/button")).Click();
            }
            catch (WebDriverTimeoutException)
            {
                if(Manager.TryTilGetConnection()) { 
                    _driver?.Navigate().Refresh();
                    goto Login;
                }

                Manager.CancelWorker(_targets, Manager.PackageId, _username, _jobId.ToString());
                Manager.GetUserFromServer();
            }
            catch (Exception)
            {
                /*                PrintLog("Save info tab passed");*/
            }
            Thread.Sleep(5000);
            try
            {
                _driver?.FindElement(By.XPath("/html/body/div[1]/div/div/section/main/div/div/div/div/button")).Click();

            }
            catch (WebDriverTimeoutException)
            {
                if (Manager.TryTilGetConnection()) { 
                    _driver?.Navigate().Refresh();
                    goto Login;
                }
                Manager.CancelWorker(_targets, Manager.PackageId, _username, _jobId.ToString());
                Manager.GetUserFromServer();
            }
            catch (Exception)
            {
                /*                PrintLog("Save info tab passed");*/
            }
            Thread.Sleep(10000);
            try
            {
                _driver?.FindElement(By.XPath("/html/body/div[5]/div/div/div/div[3]/button[2]")).Click();
            }
            catch (WebDriverTimeoutException)
            {
                if (Manager.TryTilGetConnection()) { 
                    _driver?.Navigate().Refresh();
                    goto Login;
                }

                Manager.CancelWorker(_targets, Manager.PackageId, _username, _jobId.ToString());
                Manager.GetUserFromServer();
            }
            catch (Exception)
            {
                /*                PrintLog(e.ToString());*/
            }
            return true;
        }

        private void PrepareForSendDirects()
        {
            Prepare:
            Thread.Sleep(10000);
            if (!Manager.IsConnectedToInternet())
            {
                if (Manager.TryTilGetConnection()) { 
                    _driver?.Navigate().Refresh();
                    goto Prepare;
                }

                Manager.CancelWorker(_targets, Manager.PackageId, _username, _jobId.ToString());
                Manager.GetUserFromServer();
            }
            _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
            Thread.Sleep(10000);
        }

        private void SendMessage(string[] targets, List<string?> message)
        {

            for (var i = 0; i < targets.Length; i++)
            {
                ClickNewDirect:
                try
                {
                    _driver?.FindElement(By.XPath("/html/body/div[1]/section/div/div[2]/div/div/div[1]/div[1]/div/div[3]/button")).Click();
                    Thread.Sleep(500);
                }
                catch (WebDriverTimeoutException)
                {
                    if (Manager.TryTilGetConnection()) { 
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }
                    else
                    {
                        Manager.CancelWorker(targets, Manager.PackageId, _username, _jobId.ToString());
                        Manager.GetUserFromServer();
                    }
                }
                catch (NoSuchElementException)
                {
                    //    PrintLog(e.ToString());
                    Thread.Sleep(10000);
                    try
                    {
                        _driver?.FindElement(By.XPath("/html/body/div[1]/div/div/section/div/div[2]/div/div/div[1]/div[1]/div/div[3]/button")).Click();
                    }
                    catch (WebDriverTimeoutException)
                    {
                        if (Manager.TryTilGetConnection())
                        {
                            _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                            goto ClickNewDirect;
                        }

                        Manager.CancelWorker(targets, Manager.PackageId, _username, _jobId.ToString());
                        Manager.GetUserFromServer();
                    }
                    catch (Exception)
                    {
                        goto ClickNewDirect;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    break;
                }
                var targetInput = _driver?.FindElement(By.XPath("/html/body/div[5]/div/div/div[2]/div[1]/div/div[2]/input"));
                Thread.Sleep(1000);
                targetInput?.SendKeys(targets[i]);
                Thread.Sleep(10000);

                try
                {
                    _driver?.FindElement(By.XPath("/html/body/div[5]/div/div/div[2]/div[2]/div[1]")).Click();
                    Thread.Sleep(500);
                }
                catch (WebDriverTimeoutException)
                {
                    if (Manager.TryTilGetConnection()) { 
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }
                    else
                    {
                        Manager.CancelWorker(targets, Manager.PackageId, _username, _jobId.ToString());
                        Manager.GetUserFromServer();
                    }
                }
                catch (NoSuchElementException)
                {
                    if (!Manager.IsConnectedToInternet())
                    {
                        if (Manager.TryTilGetConnection()) { 
                            _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                            goto ClickNewDirect;
                        }
                        else
                        {
                            Manager.CancelWorker(targets, Manager.PackageId, _username, _jobId.ToString());
                            Manager.GetUserFromServer();
                        }
                    }
                    Thread.Sleep(10000);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    break;
                }

                ClickDirect:
                try
                {
                    _driver.FindElement(By.XPath("/html/body/div[5]/div/div/div[1]/div/div[3]/div/button")).Click();
                }
                catch (WebDriverTimeoutException)
                {
                    if (Manager.TryTilGetConnection()) { 
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }
                    else
                    {
                        Manager.CancelWorker(targets, Manager.PackageId, _username, _jobId.ToString());
                        Manager.GetUserFromServer();
                    }
                }
                catch (Exception)
                {
                    if (_tryTimes > 6)
                    {
                        Manager.FailedSending(_users[i], _username, Manager.PackageId);
                        PrepareForSendDirects();
                        continue;
                    }
                    _tryTimes++;
                    Thread.Sleep(10000);
                    goto ClickDirect;
                }
                _tryTimes = 0;
                Thread.Sleep(5000);

                var random = new Random();

                FindAndEnterText:
                try
                {
                    var messageInput = _driver?.FindElement(By.XPath(
                        "/html/body/div[1]/div/div/section/div/div[2]/div/div/div[2]/div[2]/div/div[2]/div/div/div[2]/textarea"));
                    _messageTemp = message[random.Next(0, message.Count - 1)];
                    messageInput?.SendKeys(_messageTemp);
                    Thread.Sleep(1000);
                    messageInput.SendKeys(Keys.Enter);
                }
                catch (WebDriverTimeoutException)
                {
                    if (Manager.TryTilGetConnection()) { 
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }
                    else
                    {
                        Manager.CancelWorker(targets, Manager.PackageId, _username, _jobId.ToString());
                        Manager.GetUserFromServer();
                    }
                }
                catch (NoSuchElementException)
                {
                    try
                    {
                        var messageInput = _driver?.FindElement(By.XPath("/html/body/div[1]/section/div/div[2]/div/div/div[2]/div[2]/div/div[2]/div/div/div[2]/textarea"));
                        messageInput?.SendKeys(message[random.Next(0, message.Count - 1)]);
                        Thread.Sleep(1000);
                        messageInput?.SendKeys(Keys.Enter);
                    }
                    catch (WebDriverTimeoutException)
                    {
                        if (Manager.TryTilGetConnection()) { 
                            _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                            goto ClickNewDirect;
                        }
                        else
                        {
                            Manager.CancelWorker(targets, Manager.PackageId, _username, _jobId.ToString());
                            Manager.GetUserFromServer();
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        Thread.Sleep(10000);
                        goto FindAndEnterText;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    break;
                }
                Manager.ChangeTargetStatusInServer(targets[i], _username, _jobId);
                Thread.Sleep(random.Next(Manager.WaitMin, Manager.WaitMax));
                _firstMessage = true;
            }
            _driver?.Quit();
        }

        public string GetDescription()
        {
            return "User: " + _username + " | pass: " + _password + " | Users: " + _users.Count;
        }


    }
}
