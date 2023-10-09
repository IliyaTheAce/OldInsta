using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private string session;
        private string userAgent;

        private int failedTimes = 0;
        public User(string taskId, string username, string password, List<Manager.target> targets, int waitTime,string session , string agent)
        {
            _username = username;
            _password = password;
            this.session = session;
            _waitTime = waitTime;
            _targets = targets;
            this.taskId = taskId;
            userAgent = agent;
        }


        //Timer stuff
        private Timer _timer;
        const float TimerInterval = 10;
        private bool hasSuccsesfulDirect = false;

        private void TimerInitialize()
        {
            if (_timer != null) _timer.Dispose();
            _timer = new Timer(5 * 60000 * (_targets.Count +1));
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
                options.AddArgument("--user-agent=" + userAgent);
                _driver = new ChromeDriver(options);
                _driver.Manage().Window.Size = new Size(516, 703);
                // To get sessionid 
                // _driver.Manage().Cookies.GetCookieNamed("sessionid").Value
                // To set sessionid 
                // _driver.Manage().Cookies.AddCookie(new Cookie("sessionid" ,session ));
                TimerInitialize();
                if (!SignIn(_username, _password))
                {
                    StartNewDriver(false);
                    return false;
                }

                var beforeRandom = new Random();
                Thread.Sleep(beforeRandom.Next(3 * 60000, 5 * 60000));
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
                // isDead = true;
                Manager.Update(taskId, "200");

                if (Manager.IsPaused) return _successful;
                if (isDead) return false;
                StartNewDriver(false);
            return _successful;
        }

        async void StartNewDriver(bool timer)
        {
            try
            {
                _driver?.Quit();
            }
            catch
            {
                //ignored
            }

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
            if (true)
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

            if (_driver.Url.Contains("challenge") || _driver.Url.Contains("suspend") || _driver.Url.Contains("login"))
            {
                Manager.Update(taskId, "530");
                gotBanned = true;
                return false;
            }

            Manager.SubmitSession(_driver.Manage().Cookies.GetCookieNamed("sessionid").Value, taskId);
            }else
            {
                try
                {
                    _driver.Navigate().GoToUrl("https://instagram.com");
                    Thread.Sleep(5000);
                    _driver.Manage().Cookies.AddCookie(new Cookie("sessionid" ,session ));
                    Thread.Sleep(5000);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
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
                    try
                    {
                        var querybox = _driver?.FindElement(By.Name("queryBox"));
                        querybox.Clear();
                        Thread.Sleep(10000);
                        querybox.SendKeys(target.username.Substring(0, target.username.Length - 4));
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
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
                }

                try
                {
                    textField.SendKeys(target.message);
                    Thread.Sleep(1000);
                    textField.SendKeys(Keys.Enter);
                    hasSuccsesfulDirect = true;
                    _tryTimes = 0;
                    failedTimes = 0;
                    Manager.ServerLog(target.uid, "200");
                    _userTemp.Add(target.username);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }

                Thread.Sleep(6000);

                Thread.Sleep(target.Equals(_targets.Last()) ? 20000 : random.Next(Manager.WaitMin, Manager.WaitMax));
                PrepareForSendDirects();
            }

            return true;
        }
    }
}