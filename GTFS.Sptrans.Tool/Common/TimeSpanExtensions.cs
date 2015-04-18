using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFS.Sptrans.Tool.Common
{
    public static class TimeSpanExtensions
    {
        public static string ToGtsftime(this TimeSpan ts)
        {
            var gtfsTime =  string.Format("{0:00}:{1:00}:{2:00}", Math.Truncate(ts.TotalHours), ts.Minutes, ts.Seconds);
            return gtfsTime;
        }
    }
}
