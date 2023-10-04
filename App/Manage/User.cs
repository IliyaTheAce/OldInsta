﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Size = System.Drawing.Size;
using Timer = System.Timers.Timer;

namespace Insta_DM_Bot_server_wpf
{
    internal class User : ICommand
    {
        private ChromeDriver? _driver;
        private readonly int _waitTime;

        private readonly string _username;
        private readonly string _password;
        private readonly List<Manager.target> _targets;

        public bool isDead; 
        private int _tryTimes;
        private bool _successful = true;
        private bool netDisconnected;
        private bool gotBanned;
        private List<string> _userTemp = new List<string>();
        private string taskId;

        private int failedTimes = 0;
        public User(string taskId, string username, string password, List<Manager.target> targets, int waitTime)
        {
            _username = username;
            _password = password;
            _waitTime = waitTime;
            _targets = targets;
            this.taskId = taskId;
        }


        //Timer stuff
        private Timer _timer;
        const float TimerInterval = 10;
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
            if (gotBanned || isDead)
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
                StartNewDriver(true);
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
            var options = new ChromeOptions();
            options.AddArgument(
                "--user-agent=Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1");
            _driver = new ChromeDriver(options);
            _driver.Manage().Window.Size = new Size(516, 703);
            // To get sessionid 
            // _driver.Manage().Cookies.GetCookieNamed("sessionid").Value
            // To set sessionid 
            // _driver.Manage().Cookies.AddCookie(new Cookie("sessionid" ,session ));
            TimerInitialize();
            // _driver.Manage().Cookies.AddCookie(new Cookie("sessionid" ,session ));
            if (!SignIn(_username, _password))
            {
                StartNewDriver(false);
                return false;
            }
            
            if (isDead) return false;
            if (!PrepareForSendDirects())
            {
                StartNewDriver(false);
                return false;
            }

            if (isDead) return false;

            if (!SendMessage())
            {
                StartNewDriver(false);
                return false;
            }

            if (isDead) return false;

            _driver.Quit();
            isDead = true;
            Manager.Update(taskId , "200");

            if (Manager.IsPaused) return _successful;
            if (isDead) return false;
            isDead = true;
            Manager.FetchTask(false);
            Thread.Sleep(5000);
            if (Manager.Queue.Count > 0)
            {
                Manager.Queue.Dequeue().Execute();
            }

            return _successful;
        }

        async void StartNewDriver(bool timer)
        {
            _driver?.Quit();
            if (!gotBanned)
            {
                if (timer)
                {
                    Manager.Update(taskId, "520");
                }
                else
                {
                    Manager.Update(taskId, "510");
                }
            }

            isDead = true;

            if (Manager.IsPaused) return;
            Thread.Sleep(2000);
            await Manager.FetchTask(true);
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
                if (errorText.Contains("username") || errorText.Contains("password") || errorText.Contains("connect") || errorText.Contains("problem"))
                {
                    Manager.Update(taskId, "510");
                    gotBanned = true;
                    return false;
                }
                else if (errorText.Contains("terms"))
                {
                    Manager.Update(taskId, "530");
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

            //Home screen Shortcut
            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Cancel')]")).Click();
            }
            catch (Exception e)
            {
                // Debug.Log(e.Message);
            }

            Thread.Sleep(5000);

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
                Debug.Log(e.Message + "___ save info button not found");
            }

            Thread.Sleep(5000);
//Notification Not now button
            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Not Now')]")).Click();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + "___ notification not now button not found");
            }

            Thread.Sleep(5000);
            // Inbox button -> 
            // _driver?.FindElement(By.CssSelector(@".x1o5bo1o > .\_ab6-")).Click();

            if (_driver.Url.Contains("challenge") || _driver.Url.Contains("suspend") || _driver.Url.Contains("login"))
            {
                Manager.Update(taskId, "530");
                gotBanned = true;
                return false;
            }

            Manager.SubmitSession(_driver.Manage().Cookies.GetCookieNamed("sessionid").Value, taskId);

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

        private bool SendMessage()
        {
            //Home screen Shortcut
            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Cancel')]")).Click();
            }
            catch (Exception e)
            {
                // Debug.Log(e.Message);
            }

            Thread.Sleep(5000);


            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Not Now')]")).Click();
            }
            catch (Exception e)
            {
                //Not important
            }

            Thread.Sleep(5000);

            foreach (var target in _targets){
                if (failedTimes >= 5)
                {
                    //TODO: Change this
                    // Manager.CancelWorker(targets - _userTemp.ToArray());
                }
                ClickNewDirect:
                //New Direct Button
                try
                {
                    _driver?.FindElement(By.CssSelector(@".xexx8yu > .x1lliihq")).Click();
                }
                catch (Exception e)
                {
                    Manager.ServerLog(target.uid, "610");
                    Debug.Log(e.Message);
                    PrepareForSendDirects();
                    failedTimes++;
                    failedTimes++;
                    continue;
                }

                Thread.Sleep(1000);
                //Search bar
                try
                {
                    _driver?.FindElement(By.Name("queryBox")).SendKeys(target.username);
                }
                catch (Exception e)
                {
                    Manager.ServerLog(target.uid, "610");
                    // Debug.Log(e.Message);
                    PrepareForSendDirects(); 
                    continue;
                }

                Thread.Sleep(5000);
                //Select first contact on list
                var success = false;
                for (int j = 0; j < 7; j++)
                {
                    try
                    {
                        _driver?.FindElement(By.CssSelector(".x1cy8zhl")).Click();
                        success = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);

                        Thread.Sleep(10000);
                        if (j >= 6)
                        {
                            PrepareForSendDirects();
                            failedTimes++;
                        }
                    }
                }

                if (!success)
                {
                    Manager.ServerLog(target.uid, "610");
                }


                Thread.Sleep(1000);

                //Chat button
                try
                {
                    _driver?.FindElement(By.CssSelector(".xt0psk2")).Click();
                }
                catch (Exception e)
                {
                    Manager.ServerLog(target.uid, "610");
                    // Debug.Log(e.Message);
                    PrepareForSendDirects();
                    failedTimes++;
                    continue;
                }

                Thread.Sleep(5000);

                var random = new Random();

                IWebElement? textField = null;
                Thread.Sleep(5000);

                try
                {
                    textField = _driver?.FindElement(By.ClassName("notranslate"));
                }
                catch (WebDriverTimeoutException)
                {
                    netDisconnected = true;
                    if (Manager.TryTilGetConnection())
                    {
                        _driver?.Navigate().GoToUrl("https://www.instagram.com/direct/inbox");
                        goto ClickNewDirect;
                    }

                    return false;
                }
                catch (Exception e)
                {
                    // try
                    // {
                    //     textField = _driver?.FindElement(By.XPath(
                    //         "/html/body/div[1]/div/div/div/div[1]/div/div/div/div[1]/div[1]/section/div/div[2]/div/div/div[2]/div[2]/div/div[2]/div/div/div[2]/textarea"));
                    // }
                    // catch
                    // {
                    //     Debug.Log(e.Message);
                    //     if (SomethingWentWrongTimes < 3)
                    //     {
                    //         Manager.FailedSending(_users[i], _username, _jobId);
                    //         PrepareForSendDirects();
                    //         SomethingWentWrongTimes++;
                    //         continue;
                    //     }
                    //     else
                    //     {
                    //         errCode = ErrorCode.SWW;
                    //         return false;
                    //     }
                    // }
                }

                try
                {
                    textField?.SendKeys(target.message);
                    Thread.Sleep(1000);
                    textField?.SendKeys(Keys.Enter);
                    hasSuccsesfulDirect = true;
                    _tryTimes = 0;
                    failedTimes = 0;
                    Manager.ServerLog(target.uid, "200");
                    TimerInitialize();
                    _userTemp.Add(target.username);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }

                Thread.Sleep(6000);
                // try
                // {
                //     var randomScroll = new Random();
                //     // var humanize = new Humanize(_driver, randomScroll.Next(7, 16));
                //     // var HumanizeTask = Task.Run(humanize.Start);
                //     // Task.WaitAll(HumanizeTask);
                //     Thread.Sleep(randomScroll.Next(60000,24000));
                // }
                // catch (Exception e)
                // {
                //     Debug.Log(e.Message);
                // }

                Thread.Sleep(random.Next(Manager.WaitMin, Manager.WaitMax));
                PrepareForSendDirects();
            }

            return true;
        }
    }
}