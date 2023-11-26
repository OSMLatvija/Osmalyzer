using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Osmalyzer;

public static class WebsiteDownloadHelper
{
    private static readonly List<(string url, string content)> _cachedWebsites = new List<(string url, string content)>();
        
        
    [MustUseReturnValue]
    public static string Read(string url, bool canUseCache)
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