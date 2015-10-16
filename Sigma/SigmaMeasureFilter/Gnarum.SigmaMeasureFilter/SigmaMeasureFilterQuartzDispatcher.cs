using System;
using Gnarum.Log;
using Gnarum.Quartz.Dispatcher;
using Gnarum.SigmaMeasureFilter.Model.Repositories;
using Gnarum.SigmaMeasureFilter.Model.Services;
using Quartz;
using Quartz.Impl;
using Gnarum.SigmaMeasureFilter.Ninject;

namespace Gnarum.SigmaMeasureFilter
{
    public class SigmaMeasureFilterQuartzDispatcher<T> : IQuartzDispatcher<T>, IHasLogger where T:IJob
    {
        #region private properties
        private ILogger _logger = NullLogger.Instance;
        private string _jobName = null;
        private ISchedulerFactory _schedulerFactory = null;
        private IScheduler _scheduler = null;
        private ISigmaMeasureService _sigmaMeasureService = null;

        #endregion

        #region Protected Methods
        protected void Init()
        {
            Logger.Info(String.Format("Init Sigma Measure Filter dispatcher"));
        }

        protected void Finish()
        {
            Logger.Info(String.Format("Finishing Sigma Measure Filter dispatcher"));
        }
        #endregion
 
        #region Constructor

        public SigmaMeasureFilterQuartzDispatcher()
        {
            // construct a scheduler factory
            _schedulerFactory = new StdSchedulerFactory();

            // get a scheduler
            _scheduler = _schedulerFactory.GetScheduler();

            _jobName = Guid.NewGuid().ToString();

            _sigmaMeasureService = ModelProvider.GetDependency<ISigmaMeasureService>();
        }

        #endregion

        public DateTime LocalStartTime
        {
            get;
            set;
        }
        public int RepeatIntervalMinutes
        {
            get;
            set;
        } 

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public DateTime? ReferenceDate { get; set; }

        public int DaysBeforeReferenceDate { get; set; }

        public int DaysAfterReferenceDate { get; set; }

        public string WebAPIURL { get; set; }

        public int ReattemptsToSendMeasuresPerPlant { get; set; }

        #region Protected Properties

        protected string JobGroupName
        {
            get { return null; }
        }

        protected string JobName
        {
            get { return _jobName; }
        }

        #endregion

        private void PutVariablesInJobDataMap(JobDataMap jobDataMap)
        {
            jobDataMap.Put("Logger", Logger);
            jobDataMap.Put("SigmaMeasureService", _sigmaMeasureService);
            jobDataMap.Put("ReferenceDate", ReferenceDate);
            jobDataMap.Put("DaysBeforeReferenceDate", DaysBeforeReferenceDate);
            jobDataMap.Put("DaysAfterReferenceDate", DaysAfterReferenceDate);
            jobDataMap.Put("WebAPIURL", WebAPIURL);
            jobDataMap.Put("ReattemptsToSendMeasuresPerPlant", ReattemptsToSendMeasuresPerPlant);
        }

        public void Dispatch()
        {
            Init();

            JobDataMap jobDataMap = new JobDataMap();
            PutVariablesInJobDataMap(jobDataMap);
            

            IJobDetail jobDetail = JobBuilder.Create().OfType(typeof(T)).WithIdentity(new JobKey(JobName, JobGroupName)).UsingJobData(jobDataMap).Build();

            DateTime startTimeUtc = LocalStartTime.ToUniversalTime();

            _scheduler.ScheduleJob(
                               jobDetail
                               , TriggerBuilder.Create()
                                   .StartAt(LocalStartTime)
                                   .WithSimpleSchedule(x => x.WithIntervalInMinutes(RepeatIntervalMinutes).RepeatForever())
                                   .Build()
                           );
           
            _scheduler.Start();

            Finish();
        }

        public void Dispose()
        {
            Logger.Debug("Disposing Sigma Measure Filter Quartz dispatcher");
            if (_scheduler != null)
            {
                _scheduler.Shutdown(false);
                _scheduler = null;
            }
        }
    }
}
