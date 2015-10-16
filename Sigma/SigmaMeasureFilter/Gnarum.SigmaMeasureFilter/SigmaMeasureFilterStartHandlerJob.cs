using System;
using System.Linq;
using Gnarum.Log;
using Gnarum.SigmaMeasureFilter.Model.Services;
using Gnarum.SigmaMeasureFilter.Ninject;
using Gnarum.SigmaMeasureFilter.Providers;
using Quartz;
using System.Collections.Generic;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using System.Configuration;

namespace Gnarum.SigmaMeasureFilter
{
    public class SigmaMeasureFilterStartHandlerJob : IJob, IHasLogger
    {
        #region private Properties
        private ILogger _logger = NullLogger.Instance;

        private string _webAPIURL;

        private DateTime? _referenceDate;

        private int _daysBeforeReferenceDate;

        private int _daysAfterReferenceDate;

        private ISigmaMeasureService _sigmaMeasureService;

        private int _reattemptsToSendJSON;
        #endregion

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }



        public void Execute(IJobExecutionContext context)
        {
            try
            {
                initPropertiesFromJobExecutionContext(context);
                execute();
            }
            catch (Exception e)
            {
                Logger.Error("Exception ", e);
            }
        }

        private void execute()
        {
            Logger.Info(String.Format("Launching SigmaMeasureFilterStart at {0}", DateTime.Now));

            SigmaMeasureFilterStart sigmaMeasureFilterStart = new SigmaMeasureFilterStart();
            sigmaMeasureFilterStart.Logger = Logger;
            sigmaMeasureFilterStart.SigmaMeasureService = _sigmaMeasureService;
            sigmaMeasureFilterStart.ISAPProv = ProvidersProvider.GetDependency<SAPProvider>();

            sigmaMeasureFilterStart.WebAPIURL = _webAPIURL;

            IWebAPIProvider IWebAPIProv = ProvidersProvider.GetDependency<IWebAPIProvider>();

            sigmaMeasureFilterStart.Plants = getPlants(IWebAPIProv);
               
            sigmaMeasureFilterStart.IWepAPIProv = IWebAPIProv;

            if (_referenceDate == null)
            {
                sigmaMeasureFilterStart.SigmaMeasureFilterBeginningDate = DateTime.Now.Date.AddDays(-_daysBeforeReferenceDate);
                sigmaMeasureFilterStart.SigmaMeasureFilterEndingDate = DateTime.Now.Date.AddDays(_daysAfterReferenceDate);
            }
            else
            {
                sigmaMeasureFilterStart.SigmaMeasureFilterBeginningDate = _referenceDate.Value.Date.AddDays(-_daysBeforeReferenceDate);
                sigmaMeasureFilterStart.SigmaMeasureFilterEndingDate = _referenceDate.Value.Date.AddDays(_daysAfterReferenceDate);
            }
            Logger.Info(String.Format("SigmaMeasureFilterStart.SigmaMeasureFilterBeginningDate  {0}", sigmaMeasureFilterStart.SigmaMeasureFilterBeginningDate.ToShortDateString()));
            Logger.Info(String.Format("SigmaMeasureFilterStart.SigmaMeasureFilterEndingDate {0}", sigmaMeasureFilterStart.SigmaMeasureFilterEndingDate.ToShortDateString()));

            sigmaMeasureFilterStart.ReattemptsToSendJSON = _reattemptsToSendJSON;

            //executing the instance of SigmaMeasureFilterStart
            sigmaMeasureFilterStart.StartExecution();
            Logger.Info(String.Format("SigmaMeasureFilterStart ended at {0}", DateTime.Now));

        }

        private IList<Plant> getPlants(IWebAPIProvider IWebAPIProv)
        {                        
            var plants=IWebAPIProv.GetAllActiveWithSigmaConditionFromURL(_webAPIURL);
            if (plants == null || !plants.Any())
                return new List<Plant>();

            var technologiesAllowed = getTechnologiesAllowed();

            if (technologiesAllowed != null && technologiesAllowed.Any())
            {
                Logger.Info((String.Format("Selected technologies number:{0}", technologiesAllowed.Count())));
                technologiesAllowed.ForEach(logTechnology);
                return plants.Where(x => technologiesAllowed.Contains(x.Technology)).ToList();
            }
            Logger.Info((String.Format("Selected All Technologies")));
            return plants;
        }

        private void logTechnology(string tecnology)
        {
            Logger.Info((String.Format("Selected Technologies:{0}", tecnology)));
        }

        private List<string> getTechnologiesAllowed()
        {
            String technologies = ConfigurationManager.AppSettings["Technologies"];
            if (technologies == null || technologies == "ALL")
                return new List<string>();
            return technologies.Split(';').ToList();
        }

        private void initPropertiesFromJobExecutionContext(IJobExecutionContext context)
        {
            var tmp = context.JobDetail.JobDataMap.Get("Logger") as ILogger;
            if (tmp != null)
                Logger = tmp;

            var sigmaMeasureService = context.JobDetail.JobDataMap.Get("SigmaMeasureService") as ISigmaMeasureService;
            if (sigmaMeasureService != null)
                _sigmaMeasureService = sigmaMeasureService;

            _referenceDate = context.JobDetail.JobDataMap.GetDateTime("ReferenceDate");
            if (_referenceDate == DateTime.MinValue)
            {
                _referenceDate = null;
            }

            var daysBeforeReferenceDate = context.JobDetail.JobDataMap.GetInt("DaysBeforeReferenceDate");
            _daysBeforeReferenceDate = daysBeforeReferenceDate;

            _daysAfterReferenceDate = context.JobDetail.JobDataMap.GetInt("DaysAfterReferenceDate"); 

            var webAPIURL = context.JobDetail.JobDataMap.GetString("WebAPIURL");
            if (webAPIURL != null)
                _webAPIURL = webAPIURL;

            _reattemptsToSendJSON = context.JobDetail.JobDataMap.GetInt("ReattemptsToSendMeasuresPerPlant");
        }

    }
}
