using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Osmalyzer;

public static class WebsiteDownloadHelper
{
    private static readonly List<(string url, string content)> _cachedWebsites = new List<(string url, string content)>();


    [MustUseReturnValue]
    public static string ReadDirect(string url, bool canUseCache)
    {
        if (canUseCache)
        {
            (string _, string cachedContent) = _cachedWebsites.FirstOrDefault(cw => cw.url == url);

            if (cachedContent != null)
                return cachedContent;
        }
            
        using HttpClient client = new HttpClient();
        Uri uri = new Uri(url, UriKind.Absolute);
        using HttpResponseMessage response = client.GetAsync(uri).Result;
        using HttpContent content = response.Content;
        string result = content.ReadAsStringAsync().Result;

        if (canUseCache)
            _cachedWebsites.Add((url, result));

        return result;
    }
    
    [MustUseReturnValue]
    public static string ReadAsBrowser(string url, bool canUseCache, (string, string)[]? cookies, params BrowsingAction[] browsingActions)
    {
        if (canUseCache)
        {
            (string _, string cachedContent) = _cachedWebsites.FirstOrDefault(cw => cw.url == url);

            if (cachedContent != null)
                return cachedContent;
        }
        
        ChromeDriverService service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true; // "Starting ChromeDriver" spam
        
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--window-size=1600x1200");
        //options.AddArgument("--lang=en-US");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-notifications");
        
        //options.SetLoggingPreference(LogType.Driver, LogLevel.Severe);
        //options.AddArgument("--log-level=3");
        // it still has "ChromeDriver was started successfully." spam that I don't know how to disable

        string result;

        using IWebDriver driver = new ChromeDriver(service, options);

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

            IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
            result = (string)jsExecutor.ExecuteScript("return document.documentElement.outerHTML;");
        }
        else
        {
            result = driver.PageSource;
        }

        if (canUseCache)
            _cachedWebsites.Add((url, result));

        return result;
    }

    public static void Download(string url, string fileName)
    {
        using HttpClient client = new HttpClient();
        Uri uri = new Uri(url, UriKind.Absolute);
        using Task<Stream> stream = client.GetStreamAsync(uri);
        using FileStream fileStream = new FileStream(fileName, FileMode.Create);
        stream.Result.CopyTo(fileStream);
    }

    public static void DownloadPost(string url, (string, string)[] postFields, string fileName)
    {
        using HttpClient client = new HttpClient();
        
        client.DefaultRequestHeaders.Add("Accept", "application/json"); // todo: dehardcode

        FormUrlEncodedContent content = new FormUrlEncodedContent(postFields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

        Uri uri = new Uri(url, UriKind.Absolute);
        HttpResponseMessage response = client.PostAsync(uri, content).Result;

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException();

        string result = response.Content.ReadAsStringAsync().Result;
        
        File.WriteAllText(fileName, result);
    }

    public static DateTime? ReadHeaderDate(string url)
    {
        using HttpClient client = new HttpClient();
        Uri uri = new Uri(url, UriKind.Absolute);
        using HttpResponseMessage response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).Result;

        DateTimeOffset? lastModifedOffset = response.Content.Headers.LastModified;

        if (lastModifedOffset == null)
            return null;
            
        return lastModifedOffset.Value.UtcDateTime;
    }
}


public abstract class BrowsingAction
{
    
}

public class WaitForElementOfClass : BrowsingAction
{
    public string ClassName { get; }

    
    public WaitForElementOfClass(string className)
    {
        ClassName = className;
    }
}

public class WaitForElementOfClassToBeClickable : BrowsingAction
{
    public string ClassName { get; }

    
    public WaitForElementOfClassToBeClickable(string className)
    {
        ClassName = className;
    }
}

public class ClickElementOfClass : BrowsingAction
{
    public string ClassName { get; }
    
    public int Index { get; }


    public ClickElementOfClass(string className, int index = 0)
    {
        ClassName = className;
        Index = index;
    }
}

public class WaitForTime : BrowsingAction
{
    public int Milliseconds { get; }

    
    public WaitForTime(int milliseconds)
    {
        Milliseconds = milliseconds;
    }
}