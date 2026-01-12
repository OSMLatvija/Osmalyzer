using System.Net.Http;

namespace Osmalyzer;

public static class WebsiteDownloadHelper
{
    public static bool BrowsingEnabled { get; set; }

    
    [MustUseReturnValue]
    public static string Read(string url, bool canUseCache)
    {
        if (!BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");
        
        if (canUseCache)
            if (WebsiteCache.IsCached(url))
                return WebsiteCache.GetCached(url);
            
        using HttpClient client = new HttpClient();
        Uri uri = new Uri(url, UriKind.Absolute);
        using HttpResponseMessage response = client.GetAsync(uri).Result;
        using HttpContent content = response.Content;
        string result = content.ReadAsStringAsync().Result;

        if (canUseCache)
            WebsiteCache.Cache(url, result);

        return result;
    }

    public static void Download(string url, string fileName)
    {
        if (!BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");

        using HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = false };
        using HttpClient client = new HttpClient(handler);
        HttpResponseMessage response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        // Follow one redirect, i.e. Geobarik https://blog.geofabrik.de/index.php/2025/07/24/download-geofabrik-de-to-use-http-redirects-for-latest-files/
        if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
        {
            string redirectUrl = response.Headers.Location?.IsAbsoluteUri == true
                ? response.Headers.Location.ToString()
                : new Uri(new Uri(url), response.Headers.Location!).ToString();
            response.Dispose();
            response = client.GetAsync(redirectUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        }
        response.EnsureSuccessStatusCode();
        using Stream stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.CopyTo(fileStream);
    }

    public static void DownloadPost(string url, (string, string)[] postFields, string fileName)
    {
        if (!BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");

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

    public static void DownloadPostJson(string url, string jsonBody, string fileName)
    {
        if (!BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");

        using HttpClient client = new HttpClient();
        
        StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        Uri uri = new Uri(url, UriKind.Absolute);
        HttpResponseMessage response = client.PostAsync(uri, content).Result;

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP request failed with status code {response.StatusCode}");

        string result = response.Content.ReadAsStringAsync().Result;
        
        File.WriteAllText(fileName, result);
    }

    public static DateTime? ReadHeaderDate(string url)
    {
        if (!BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");

        using HttpClient client = new HttpClient();
        Uri uri = new Uri(url, UriKind.Absolute);
        using HttpResponseMessage response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).Result;

        DateTimeOffset? lastModifedOffset = response.Content.Headers.LastModified;

        if (lastModifedOffset == null)
            return null;
            
        return lastModifedOffset.Value.UtcDateTime;
    }

    public static string? GetRedirectUrl(string url)
    {
        if (!BrowsingEnabled)
            throw new Exception("Web browsing should only be performed in Download()");

        HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false
        };
        using HttpClient client = new HttpClient(handler);
        Uri uri = new Uri(url, UriKind.Absolute);
        using HttpResponseMessage response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).Result;
        return response.Headers.Location?.AbsolutePath ?? null;
    }
}