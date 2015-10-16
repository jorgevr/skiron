using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Gnarum.SigmaMeasureFilter.Test.Model;
using Gnarum.Log;
using Quartz;
using Gnarum.SigmaMeasureFilter;

namespace Gnarum.SigmaMeasureFilter.Test
{
    [TestClass]
    public class SigmaMeasureFilterQuartzDispatcherTest
    {
        [TestMethod]
        public void Can_Launch_Jobs_with_repeat_interval()
        {
            string TEST_FOLDER = String.Format("{0}\\test_quartz", Path.GetTempPath());
            string FILE_EXTENSION = ".test1";
            prepareTestFolder(TEST_FOLDER);

            SigmaMeasureFilterQuartzDispatcher<DummySigmaMeasureFilterQuartzDispatcherJob> sigmaMeasureFilterQuartzDispatcher = new SigmaMeasureFilterQuartzDispatcher<DummySigmaMeasureFilterQuartzDispatcherJob>();

            sigmaMeasureFilterQuartzDispatcher.Logger = NullLogger.Instance;
            sigmaMeasureFilterQuartzDispatcher.RepeatIntervalMinutes = 1;
            sigmaMeasureFilterQuartzDispatcher.LocalStartTime = DateTime.Now.AddSeconds(20);
            sigmaMeasureFilterQuartzDispatcher.Dispatch();
            System.Threading.Thread.Sleep(120000);

            Assert.AreEqual(2, Directory.GetFiles(TEST_FOLDER, "*" + FILE_EXTENSION).Length);

        }

        private void prepareTestFolder(string testFolder)
        {
            if (Directory.Exists(testFolder) && Directory.GetFiles(testFolder, "*" + DummySigmaMeasureFilterQuartzDispatcherJob.FILE_EXTENSION).Length > 0)
                new DirectoryInfo(testFolder).GetFiles("*" + DummySigmaMeasureFilterQuartzDispatcherJob.FILE_EXTENSION).ToList().ForEach(f => f.Delete());
            else Directory.CreateDirectory(testFolder);
        }
    }
}
