﻿using System.Net.Http;
using System.Threading.Tasks;

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

        using HttpClient client = new HttpClient();
        Uri uri = new Uri(url, UriKind.Absolute);
        using Task<Stream> stream = client.GetStreamAsync(uri);
        using FileStream fileStream = new FileStream(fileName, FileMode.Create);
        stream.Result.CopyTo(fileStream);
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