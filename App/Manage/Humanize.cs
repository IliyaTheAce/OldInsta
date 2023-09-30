using System;
using System.Threading.Tasks;
using System.Windows;
using OpenQA.Selenium;
namespace Insta_DM_Bot_server_wpf;

public class Humanize
{
    private const int LikeChance = 40; //In percent
    private WebDriver _driver;
    private int scrollNumber;

    public Humanize(WebDriver driver, int scrollNumber)
    {
        _driver = driver;
        this.scrollNumber = scrollNumber;
    }

    public async Task Start()
    {
        try
        {
            _driver.Navigate().GoToUrl("https://www.instagram.com/explore/");
            await Task.Delay(3000);
            var randomPost = new Random();
            _driver.ExecuteScript(
                $"document.getElementsByClassName('_aagw')[{randomPost.Next(1, 10)}].parentElement.parentElement.click()");
            for (int i = 0; i < scrollNumber; i++)
            {
                var chance = new Random();
                await Task.Delay(5000);
                var LikePost = chance.Next(0, 100) <= LikeChance;
                try
                {
                    if (LikePost)
                    {
                        _driver.ExecuteScript($"document.getElementsByClassName('_aamw')[{i}].children[0].click()");
                        await Task.Delay(5000);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Could not Like: " + e.Message + "\n");
                }
                await Task.Delay(5000);
                _driver.ExecuteScript("window.scrollBy(0,1000)");
                await Task.Delay(1000);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
            await Task.Delay(180000);
        }
    }
}