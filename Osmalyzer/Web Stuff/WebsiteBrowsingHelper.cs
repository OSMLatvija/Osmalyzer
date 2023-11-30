using System;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Osmalyzer;
using SeleniumExtras.WaitHelpers;

public static class WebsiteBrowsingHelper
{
    private static IWebDriver? _driver;

    [MustUseReturnValue]
    public static string Read(string url, bool canUseCache, (string, string)[]? cookies = null, params BrowsingAction[] browsingActions)
    {
        if (canUseCache)
            if (WebsiteCache.IsCached(url))
                return WebsiteCache.GetCached(url);
        
        IWebDriver driver = PrepareChrome();

        string result;

        driver.Navigate().GoToUrl(url);

        if (cookies != null)
            foreach ((string name, string value) in cookies)
                driver.Manage().Cookies.AddCookie(new Cookie(name, value));
        //driver.Manage().Cookies.AddCookie(new Cookie(name, value, new Uri(url).Host, "", null)); -- can't set this before navigate apparently
        
        if (browsingActions.Length> 0)
        {
            foreach (BrowsingAction browsingAction in browsingActions)
            {
                switch (browsingAction)
                {
                    case WaitForElementOfClass waitForElementOfClass:
                    {
                        IWait<IWebDriver> wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        wait.Until(ExpectedConditions.ElementExists(By.ClassName(waitForElementOfClass.ClassName)));
                        break;
                    }

                    case ClickElementOfClass clickElementOfClass:
                    {
                        IWebElement nthElement;
                        
                        int attempts = 0;
                        
                        do
                        {
                            try
                            {
                                // todo: non multiple version
                                nthElement = driver.FindElements(By.ClassName(clickElementOfClass.ClassName)).ElementAt(clickElementOfClass.Index);
                                nthElement.Click();
                                break;
                            }
                            catch (StaleElementReferenceException) // not even joking - this is official doc recommendation for handling this
                            {
                                attempts++;
                                if (attempts == 10)
                                    throw;

                                Thread.Sleep(100);
                            }
                            
                        } while (true);
                        
                        break;
                    }

                    case WaitForElementOfClassToBeClickable waitForElementOfClassToBeClickable:
                    {
                        IWait<IWebDriver> wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName(waitForElementOfClassToBeClickable.ClassName)));
                        break;
                    }
                    
                    case WaitForTime waitForTime:
                        Thread.Sleep(waitForTime.Milliseconds);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(browsingAction));
                }
            }

            // We cannot use page source, which is the original source we received, but the DOM can change after that - we need to grab the current page
            IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
            result = (string)jsExecutor.ExecuteScript("return document.documentElement.outerHTML;");
        }
        else
        {
            result = driver.PageSource;
        }

        if (canUseCache)
            WebsiteCache.Cache(url, result);

        return result;
    }

    public static void DownloadPage(string url, string fileName, bool canUseCache = true, params BrowsingAction[] browsingActions)
    {
        // Headless browsing needs a full site load, so there's no way to directly write to file, we just have to dump the results 
        File.WriteAllText(fileName, Read(url, canUseCache, null, browsingActions));
    }
    

    [MustUseReturnValue]
    private static IWebDriver PrepareChrome()
    {
        if (_driver != null)
            return _driver;
        
        ChromeDriverService service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true; // "Starting ChromeDriver" spam

        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--headless");
        //options.AddArgument("--enable-javascript");
        options.AddArgument("--window-size=1600x1200");
        //options.AddArgument("--lang=en-US");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-notifications");
        options.SetLoggingPreference(LogType.Driver, LogLevel.Severe);
        options.AddArgument("--log-level=3");
        // it still has "ChromeDriver was started successfully." spam that I don't know how to disable

        _driver = new ChromeDriver(service, options);
        
        return _driver;
    }
}