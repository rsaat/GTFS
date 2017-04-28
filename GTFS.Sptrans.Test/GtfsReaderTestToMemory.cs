using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTFS.IO;
using NUnit.Framework;

namespace GTFS.Sptrans.Test
{
    [TestFixture]
    public class GtfsReaderTestToMemory 
    {
        [Test]
        public void ReadExampleTest()
        {
           // create the reader.
           var reader = new GTFSReader<GTFSFeed>(false);
            

           // execute the reader.
           var feed = reader.Read(new GTFSDirectorySource(new DirectoryInfo(@"H:\netprojects\GTFS\GTFS.Sptrans.Test\GtfsSpTrans")));

            var agency = feed.Agencies.First();
            Assert.That(agency.Name.ToLower(), Is.StringContaining("sptrans"));

        }
    }
}
