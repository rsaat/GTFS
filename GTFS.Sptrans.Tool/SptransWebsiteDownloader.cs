using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTFS.Sptrans.WebsiteDownloader;
using System.IO;

namespace GTFS.Sptrans.Tool
{
    public class SptransWebsiteDownloader
    {
        private const string SptranslinkslinedetailsHtm = "SptransLinksLineDetails.htm";

        public void CreateFileList()
        {

            UpdateFileFromSptransSite();
            
            var text = File.ReadAllText(SptranslinkslinedetailsHtm);

            var parser = new SptransWebPageLinkFinder();

            var baseAddress = "http://200.99.150.170/PlanOperWeb/";

            var lineDetailsLocations = parser.FindAllUrlsWithLineDetails(baseAddress, text);

            var d = new DownloadFileList(lineDetailsLocations);

            d.CreteFileList("sptrans_details_file_list.txt");

        }

        private void UpdateFileFromSptransSite()
        {
            var lastWriteDate = DateTime.MinValue; 

            if (File.Exists(SptranslinkslinedetailsHtm))
            {
                lastWriteDate = File.GetLastWriteTime(SptranslinkslinedetailsHtm);
            }
            if (lastWriteDate.Date < DateTime.Now.Date)
                {
                    var httpClient = new SptransWebsiteHttpClient();
                    var textHtml = httpClient.DownloadPageWithAllLineLinks();
                    File.WriteAllText(SptranslinkslinedetailsHtm, textHtml, Encoding.UTF8);
                }
            
        }
    }
}
