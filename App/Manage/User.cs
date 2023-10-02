using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using OpenQA.Selenium.Chromium;
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
        private List<Manager.Message> _messages = new ();
        private string taskId;
        private int failedToSelectContactTimes = 0;
        private int failedTimes = 0;
        public User(string taskId, string username, string password, List<Manager.target> targets, int waitTime,List<Manager.Message> messages)
        {
            _username = username;
            _password = password;
            _waitTime = waitTime;
            _targets = targets;
            _messages = messages;
            this.taskId = taskId;
        }


        //Timer stuff
        private Timer _timer;
        const float TimerInterval = 10;
        private bool hasSuccsesfulDirect = false;

        private void TimerInitialize()
        {
            if (_timer != null) _timer.Dispose();
            _timer = new Timer(5 * 60000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = true;
            _timer.AutoReset = false;
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
            // options.AddArgument("--auto-open-devtools-for-tabs");
            options.EnableMobileEmulation("iPhone 12 Pro");
             options.AddArgument(
                "--user-agent=Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1");
            _driver = new ChromeDriver(options);
            // _driver.Manage().Window.Maximize();
            TimerInitialize();
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
            Thread.Sleep(2000);
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
                    Manager.Update(taskId, "511");
                    gotBanned = true;
                    return false;
                }
                else if (errorText.Contains("password"))
                {
                    Manager.Update(taskId, "512");
                    gotBanned = true;
                    return false;
                }
                else if(errorText.Contains("connect"))
                {
                    Manager.Update(taskId, "513");
                    gotBanned = true;
                    return false;
                }else if (errorText.Contains("problem"))
                {
                    Manager.Update(taskId, "514");
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
                // Debug.Log(e.Message + "___ save info button not found");
            }

            Thread.Sleep(5000);
//Notification Not now button
            try
            {
                _driver?.FindElement(By.XPath("//button[contains(.,'Not Now')]")).Click();
            }
            catch (Exception e)
            {
                // Debug.Log(e.Message + "___ notification not now button not found");
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

            return true;
        }

        private bool PrepareForSendDirects()
        {
            Prepare:
            Thread.Sleep(5000);
            // if (!Manager.IsConnectedToInternet())
            // {
            //     netDisconnected = true;
            //
            //     if (Manager.TryTilGetConnection())
            //     {
            //         _driver?.Navigate().Refresh();
            //         goto Prepare;
            //     } 
            //     return false;
            // }

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
            if (_targets.Count % 5 != 0)
            {
                MessageBox.Show("تعداد تارگت ها حتما باید ضریب 5 باشد");
                Manager.Update(taskId , "");
                return false;
            }

            var cycleCount = _targets.Count;
        
            for (int i =0; i<= cycleCount; i++){
                if (failedTimes >= 5)
                {
                    Manager.Update(taskId, "560");
                }
                //Go to the post
                try
                {
                    _driver?.Navigate().GoToUrl(_messages[i].link);
                    Thread.Sleep(10000);
                }
                catch (Exception e)
                {
                    MessageBox.Show("در تغییر url به پست مشکلی بوجود آمده");
                    Manager.Update(taskId , "");
                    return false;
                }         
                //Click on Share button
                try
                {
                    _driver?.FindElement(By.ClassName("_abl-")).Click();
                }
                catch (Exception e)
                {

                    MessageBox.Show("دکمه شیر پیدا نشد ، xpath چک شود");
                    Manager.Update(taskId , "");
                    return false;
                }
                Thread.Sleep(5000);

                for (int j = 0; j < 5; j++)
                {
                    //Pasting username on the input
                    int trycount = 0;
                    findingQueryBox:
                    if (trycount >= 5)
                    {
                        Manager.Update(taskId, "");
                    }
                    IWebElement queryBox;
                    try
                    {
                        queryBox= _driver?.FindElement(By.Name("queryBox"));
                    }
                    catch (Exception e)
                    {
                        trycount++;
                        Thread.Sleep(5000);
                        goto findingQueryBox;
                    }
                    try
                    {
                        queryBox?.SendKeys(_targets[(i * 5) + j].username);
                    }
                    catch (Exception e)
                    {

                        Manager.ServerLog(_targets[(i * 5) + j].uid, "610");
                        Debug.Log(e.Message);
                        failedTimes++;
                        continue;
                    }
                    Thread.Sleep(10000);
                    //Selecting first user
                    try
                    {
                        _driver?.FindElement(By.Name("ContactSearchResultCheckbox")).Click();
                    }
                    catch (Exception e)
                    {
                        queryBox?.Clear();
                        Manager.ServerLog(_targets[i*5 +j].uid,"611");
                        Thread.Sleep(2000);
                        continue;
                    }
                    Manager.ServerLog(_targets[i*5 +j].uid,"200");

                    Thread.Sleep(5000);

                        hasSuccsesfulDirect = true;
                        _tryTimes = 0;
                        failedTimes = 0;
                }
                try
                {
                    _driver?.FindElement(By.CssSelector(".x1lq5wgf")).Click();

                    Thread.Sleep(2000);
                }
                catch (Exception e)
                {
                    MessageBox.Show("دکمه ارسال پیدا نشد، چک شود");
                }

                Thread.Sleep(6000);
                TimerInitialize();
                try
                {
                    var randomScroll = new Random();
                    var humanize = new Humanize(_driver, randomScroll.Next(5, 12));
                    var HumanizeTask = Task.Run(humanize.Start);
                    Task.WaitAll(HumanizeTask);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }

                // Thread.Sleep(random.Next(Manager.WaitMin, Manager.WaitMax));
                PrepareForSendDirects();
            }

            return true;
        }
    }
}