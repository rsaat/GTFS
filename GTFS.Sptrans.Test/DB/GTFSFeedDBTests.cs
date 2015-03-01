// The MIT License (MIT)

// Copyright (c) 2014 Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GTFS.DB;
using GTFS.IO;
using GTFS.IO.CSV;
using NUnit.Framework;

namespace GTFS.Sptrans.Test.DB
{
    /// <summary>
    /// Contains test methods that can be applied to any GTFS feed db.
    /// </summary>
    [TestFixture]
    public abstract class GTFSFeedDBTests
    {
      
        /// <summary>
        /// Builds a test feed.
        /// </summary>
        /// <returns></returns>
        protected virtual IGTFSFeed BuildTestFeed()
        {

            // create the reader.
            var reader = new GTFSReader<GTFSFeed>(false);

            // execute the reader.
            var feed = reader.Read(new GTFSDirectorySource(new DirectoryInfo(@"H:\netprojects\GTFS\GTFS.Sptrans.Test\GtfsSpTrans")));

            return feed;
        }

        /// <summary>
        /// Creates a new test db.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        protected abstract IGTFSFeedDB CreateDB(string connectionString);
        
        /// <summary>
        /// Tests adding a feed.
        /// </summary>
        [Test]
        public void TestAddFeed()
        {
            string sqliteConnectionString = @"Data Source=H:\netprojects\GTFS\GTFS.Sptrans.Test\DB\SQLite\gtfssptrans.db;Version=3;";
            // get test db.
            var db = this.CreateDB(sqliteConnectionString);

            // build test feed.
            var feed = this.BuildTestFeed();

            // add/get to/from db and compare all.
            var feedId = db.AddFeed(feed);
            //GTFSAssert.AreEqual(feed, db.GetFeed(feedId));
        }

        /// <summary>
        /// Test removing a feed.
        /// </summary>
        [Test]
        public void TestRemoveFeed()
        {
            // get test db.
            var db = this.CreateDB("");

            // build test feed.
            var feed = this.BuildTestFeed();

            // add feed.
            var feedId = db.AddFeed(feed);

            db.RemoveFeed(feedId);

            // get feed.
            feed = db.GetFeed(feedId);
            Assert.IsNull(feed);
        }

        /// <summary>
        /// Test get feeds.
        /// </summary>
        [Test]
        public void TestGetFeeds()
        {
            // get test db.
            var db = this.CreateDB("");

            // build test feed.
            var feed = this.BuildTestFeed();

            // add feed.
            var feedId = db.AddFeed(feed);

            db.RemoveFeed(feedId);

            // get feed.
            feed = db.GetFeed(feedId);
            Assert.IsNull(feed);
        }
    }
}
