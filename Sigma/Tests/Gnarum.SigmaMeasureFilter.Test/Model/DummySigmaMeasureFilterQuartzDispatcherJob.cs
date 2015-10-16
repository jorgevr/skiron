using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using System.IO;

namespace Gnarum.SigmaMeasureFilter.Test.Model
{
    public class DummySigmaMeasureFilterQuartzDispatcherJob : IJob
    {
        public const string FOLDER_KEY_NAME = "folder";
        public const string FILE_EXTENSION = ".test1";

        public void Execute(IJobExecutionContext context)
        {
            string TEST_FOLDER = String.Format("{0}\\test_quartz", Path.GetTempPath());
            //Thread.Sleep(new Random(DateTime.Now.Millisecond).Next(1000));
            File.Create(TEST_FOLDER + "\\" + Guid.NewGuid() + FILE_EXTENSION).Dispose();
        }
    }
}
