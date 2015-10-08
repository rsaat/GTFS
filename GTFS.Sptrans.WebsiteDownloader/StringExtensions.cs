using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFS.Sptrans.WebsiteDownloader
{
    public static class StringExtensions
    {

        public static string EmptyIfNull(this object value)
        {
            if (value == null)
                return "";
            return value.ToString();
        }

        public static bool ContainsCaseIgnored(this object value, string text)
        {
            var result = value.EmptyIfNull().ToLower().Contains(text.ToLower());
            return result;
        }
    }
}
