using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace GTFS.Sptrans.WebsiteDownloader
{
    public class SptransWebsiteHttpClient
    {
        private HttpClientHandler _handler;
        private IEnumerable<Cookie> _responseAuthCookies;

        public string DownloadPageWithAllLineLinks()
        {
            string resultContent = "";
            var lineNumber = "%";
            
            using (var httpClient = CreateSpTransHttpClientForLineWebSearch())
            {
                resultContent = httpClient.GetStringAsync("/PlanOperWeb/linhaselecionada.asp?Linha=" + lineNumber).Result;
            }
            
            return resultContent;
        }

        private HttpClient CreateSpTransHttpClientForLineWebSearch()
        {
            return CreateSpTransHttpClient("http://200.99.150.170");
        }

        private HttpClient CreateSpTransHttpClient(string uriString)
        {
            var uri = new Uri(uriString);

            CreateHttpHandler(uri);

            var httpClient = new HttpClient(_handler);
            httpClient.BaseAddress = uri;
            httpClient.Timeout = new TimeSpan(0, 0, 0, 10);

            return httpClient;
        }

        private void CreateHttpHandler(Uri uri)
        {
            var cookies = new CookieContainer();

            if (_responseAuthCookies != null)
            {
                foreach (var responseAuthCookie in _responseAuthCookies)
                {
                    cookies.Add(uri, responseAuthCookie);
                }
            }

            _handler = new HttpClientHandler();
            _handler.CookieContainer = cookies;
        }

    }
}
