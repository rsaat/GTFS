using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AngleSharp;

namespace GTFS.Sptrans.WebsiteDownloader
{
    public class SptransWebPageLinkFinder
    {

        public string FindFirstUrlWithLineDetails(string htmlText)
        {
            var url = "";

            var doc = DocumentBuilder.Html(htmlText);

            var links = from l in doc.Links
                        where l.OuterHtml.ToLower().Contains("detalheLinha.asp".ToLower())
                        select l;
            if (links.Any())
            {
                var attributes =
                    links.First().Attributes.Where(a => a.Value.ToLower().Contains("detalheLinha.asp".ToLower()));
                if (attributes.Any())
                {
                    url = attributes.First().Value;
                }
            }

            doc.Dispose();

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Line detail url not found.", "htmlText");
            }
            return url;
        }
        
        public IList<LineDetailLocations> FindAllUrlsWithLineDetails(string baseAddress, string htmlText)
        {
            var url = "";
            var lineDetailsLocations = new List<LineDetailLocations>();

            var doc = DocumentBuilder.Html(htmlText);

            var allDivs = doc.QuerySelectorAll("div").Where(d => d.ClassName.EmptyIfNull().ToLower().Contains("labelhold"));

            foreach (var div in allDivs)
            {
                var links = div.QuerySelectorAll("a").Where(d => d.ClassName.EmptyIfNull().ToLower().Contains("linkdetalhes")).ToList();

                var hrefs = from l in links
                            where (!string.IsNullOrEmpty(l.GetAttribute("href")))
                            select l.GetAttribute("href");

                var detailPagePath = hrefs.First(h => h.ToLower().Contains("CdPjOID".ToLower()));
                var apiPath = hrefs.First(h => h.ToLower().Contains("olhovivo".ToLower()));

                lineDetailsLocations.Add(new LineDetailLocations(baseAddress, detailPagePath, apiPath));
            }

            doc.Dispose();
            return lineDetailsLocations;
        }

    }
}
