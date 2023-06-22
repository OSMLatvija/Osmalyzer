using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Osmalyzer
{
    public static class WebsiteDownloadHelper
    {
        [MustUseReturnValue]
        public static string Read(string url)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = client.GetAsync(url).Result;
            using HttpContent content = response.Content;
            return content.ReadAsStringAsync().Result;
        }

        public static void Download(string url, string fileName)
        {
            using HttpClient client = new HttpClient();
            using Task<Stream> stream = client.GetStreamAsync(url);
            using FileStream fileStream = new FileStream(fileName, FileMode.Create);
            stream.Result.CopyTo(fileStream);
        }
    }
}