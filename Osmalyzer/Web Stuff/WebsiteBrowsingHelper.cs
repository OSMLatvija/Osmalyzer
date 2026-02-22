using System.Text.Json;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Support.UI;
using Osmalyzer;
using SeleniumExtras.WaitHelpers;

public static class WebsiteBrowsingHelper
{
    public static List<string> RecentRequestHeaders { get; } = new List<string>();
    
    public static List<string> RecentResponseHeaders { get; } = new List<string>();

    
    private static IWebDriver? _driver;

    // Download directory to capture headless downloads
    private static string? downloadDirectory;


    [MustUseReturnValue]
    public static string Read(string url, bool canUseCache, (string, string)[]? cookies = null, params BrowsingAction[] browsingActions)
    {
        if (!WebsiteDownloadHelper.BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");

        RecentRequestHeaders.Clear();
        RecentResponseHeaders.Clear();
        
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
        
        if (browsingActions.Length > 0)
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
            result = Convert.ToString(jsExecutor.ExecuteScript("return document.documentElement.outerHTML;")) ?? string.Empty;
        }
        else
        {
            // Always read current DOM to avoid stale PageSource differences and nullability issues
            IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
            result = Convert.ToString(jsExecutor.ExecuteScript("return document.documentElement.outerHTML;")) ?? string.Empty;
        }

        if (canUseCache)
            WebsiteCache.Cache(url, result);

        return result;
    }

    public static void DownloadPage(string url, string fileName, bool canUseCache = true, string? retryIfNotFoundInContent = null, params BrowsingAction[] browsingActions)
    {
        if (!WebsiteDownloadHelper.BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");

        // Ensure download directory exists and is cleared before navigation
        string downloads = downloadDirectory ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Downloads"));
        Directory.CreateDirectory(downloads);
        foreach (string path in Directory.GetFiles(downloads))
        {
            try { File.Delete(path); } catch { /* ignore file in use */ }
        }

        // Perform navigation/actions and capture page HTML
        string contents = Read(url, canUseCache, null, browsingActions);

        // If caller expects specific content on the page, permit a short retry window only
        if (retryIfNotFoundInContent != null)
        {
            int retry = 0;
            while (!contents.Contains(retryIfNotFoundInContent) && retry < 2)
            {
                Thread.Sleep(1000 * (retry + 1));
                contents = Read(url, canUseCache, null, browsingActions);
                retry++;
            }
        }

        // Detect if a download happened; allow a brief stabilization window (<= 10s)
        string? downloadedPath = WaitForAnyDownload(downloads, TimeSpan.FromSeconds(10));

        if (downloadedPath != null)
        {
            File.Move(downloadedPath, fileName, true);
            return;
        }

        // No download occurred; save page HTML
        File.WriteAllText(fileName, contents);

        // Local function: wait briefly for any file to appear and stabilize size
        static string? WaitForAnyDownload(string directory, TimeSpan timeout)
        {
            DateTime started = DateTime.UtcNow;
            string? candidatePath = null;
            long lastSize = -1;

            while (DateTime.UtcNow - started < timeout)
            {
                string[] files = Directory.GetFiles(directory);
                if (files.Length > 0)
                {
                    // pick the newest
                    FileInfo fi = files.Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTimeUtc).First();
                    if (candidatePath == null || candidatePath != fi.FullName)
                    {
                        candidatePath = fi.FullName;
                        lastSize = -1;
                    }

                    long size = fi.Length;
                    if (size > 0 && size == lastSize)
                        return candidatePath;

                    lastSize = size;
                }

                Thread.Sleep(300);
            }

            return null;
        }
    }

    public static string TryUnwrapJsonFromBoilerplateHtml(string source)
    {
        // TODO: we should be doing something like string json = driver.FindElement(By.TagName("body")).Text; and not this hard-coding
        
        // On remote it could be wrapped like this:
        // We were trying to parse: <html><head><meta name="color-scheme" content="light dark"><meta charset="utf-8"></head><body><pre>{JSON HERE...

        if (source.StartsWith("<"))
        {
            // Grab content between "<pre>", which seems what it is wrapped in
            source = source[
                (source.IndexOf("<pre>", StringComparison.Ordinal) + "<pre>".Length)
                ..
                source.LastIndexOf("</pre>", StringComparison.Ordinal)
            ];
        }

        return source;
    }


    [MustUseReturnValue]
    private static IWebDriver PrepareChrome()
    {
        if (_driver != null)
            return _driver;
        
        ChromeDriverService service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true; // "Starting ChromeDriver" spam
        
        ChromeOptions options = new ChromeOptions();

        options.AddArgument("--headless=new");
        //options.AddArgument("--enable-javascript");
        options.AddArgument("--window-size=1600x1200");
        //options.AddArgument("--lang=en-US");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-notifications");
        options.SetLoggingPreference(LogType.Driver, LogLevel.Severe);
        options.AddArgument("--log-level=3");
        // it still has "ChromeDriver was started successfully." spam that I don't know how to disable
        
        // Disable Chrome's optimization guide features that can interfere with downloads
        options.AddArgument("--disable-features=OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints");
        
        // Ignore any SSL and such problems, because sites seem to have issues on GitHub 
        options.AcceptInsecureCertificates = true;
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--ignore-ssl-errors");

        downloadDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Downloads"));
        Directory.CreateDirectory(downloadDirectory);
        options.AddUserProfilePreference("download.default_directory", downloadDirectory);
        options.AddUserProfilePreference("download.prompt_for_download", false);
        options.AddUserProfilePreference("download.directory_upgrade", true);
        options.AddUserProfilePreference("safebrowsing.enabled", true);

        ChromeDriver chromeDriver = new ChromeDriver(service, options);
        
        _driver = chromeDriver;

        chromeDriver.ExecuteCdpCommand(
            "Page.setDownloadBehavior",
            new Dictionary<string, object>
            {
                { "behavior", "allow" },
                { "downloadPath", downloadDirectory }
            }
        );

        DevToolsSession session = ((IDevTools)chromeDriver).GetDevToolsSession();
        
        session.Domains.Network.EnableNetwork();

        // chromeDriver.ExecuteCdpCommand(
        //     "Network.setUserAgentOverride",
        //     new Dictionary<string, object>
        //     {
        //         { "userAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" }
        //     }
        // );

        session.DevToolsEventReceived += OnDevToolsEventReceived;

        
        return _driver;

        
        void OnDevToolsEventReceived(object? sender, DevToolsEventReceivedEventArgs e)
        {
            // Debug.WriteLine("!!!!!!!! DevTools event: " + e.EventName);
            // Debug.WriteLine("!!!!!!!! DevTools domain name: " + e.DomainName);
            // Debug.WriteLine("!!!!!!!! DevTools data: " + e.EventData);
            
            if (e.EventName == "requestWillBeSentExtraInfo")
            {
                JsonElement eventData = e.EventData;
                if (eventData.TryGetProperty("headers", out JsonElement headers))
                    RecentRequestHeaders.Add(headers.ToString());
            }
            else if (e.EventName == "responseReceivedExtraInfo")
            {
                JsonElement eventData = e.EventData;
                if (eventData.TryGetProperty("headers", out JsonElement headers))
                    RecentResponseHeaders.Add(headers.ToString());
            }
        }
    }
}