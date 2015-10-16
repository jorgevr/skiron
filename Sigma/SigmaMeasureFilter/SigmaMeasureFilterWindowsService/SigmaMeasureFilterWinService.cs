using System;
using System.Configuration;
using System.Globalization;
using System.ServiceProcess;
using System.Text;
using Gnarum.Log;
using Gnarum.Quartz.Dispatcher;
using Gnarum.SigmaMeasureFilter;
using SigmaMeasureFilterWindowsService.Util;

namespace SigmaMeasureFilterWindowsService
{
    partial class SigmaMeasureFilterWinService : SMServiceBase<SigmaMeasureFilterStartHandlerJob>, IHasLogger
    {
        #region const
        public const string SigmaMeasureWindowsServiceName = "SigmaMeasureFilterWindowsServiceName";
        // can be reused for each project
        private const string LogFileName = "sigmameasurefilter.logconfig";
        private const string SigmaMeasureModuleLogName = "Sigma Measure Filter LOG";

        #endregion

        public SigmaMeasureFilterWinService() :
            base(SigmaMeasureModuleLogName, SigmaMeasureWindowsServiceName, LogFileName)
        {
            InitializeComponent();
        }

        static void Main(string[] args)
        {

            SigmaMeasureFilterWinService service = new SigmaMeasureFilterWinService();

            if (Environment.UserInteractive)
            {
                service.OnStart(args);
                Console.WriteLine("Press any key to stop program");
                Console.ReadLine();
                service.OnStop();
            }
            else
            {
                ServiceBase.Run(service);
            }

        }

        protected override IQuartzDispatcher<SigmaMeasureFilterStartHandlerJob> getQuartzDispatcher()
        {
            SigmaMeasureFilterQuartzDispatcher<SigmaMeasureFilterStartHandlerJob> dispatcher = new SigmaMeasureFilterQuartzDispatcher<SigmaMeasureFilterStartHandlerJob>();

            dispatcher.Logger = Logger;

            string initDateString = ConfigurationManager.AppSettings["InitDate"];
            DateTime initDate;
            if (initDateString.ToLower().Equals("now"))
            {
                initDate = DateTime.Now;
            }
            else
            {
                CultureInfo provider = new CultureInfo("es-ES", false);
                initDate = DateTime.Parse(initDateString, provider);
            }
            dispatcher.LocalStartTime = initDate;

            string repeatIntervalMinutesString = ConfigurationManager.AppSettings["RepeatIntervalMinutes"];
            int repeatIntervalMinutes = int.Parse(repeatIntervalMinutesString);
            dispatcher.RepeatIntervalMinutes = repeatIntervalMinutes;

            string referenceDateString = ConfigurationManager.AppSettings["ReferenceDate"];
            DateTime? referenceDate = null;
            if (!referenceDateString.ToLower().Equals("now"))
            {
                CultureInfo provider = new CultureInfo("es-ES", false);
                referenceDate = DateTime.Parse(referenceDateString, provider);
            }
            dispatcher.ReferenceDate = referenceDate;

            string daysBeforeReferenceDateString = ConfigurationManager.AppSettings["DaysBeforeReferenceDate"];
            int daysBeforeReferenceDate = int.Parse(daysBeforeReferenceDateString);
            dispatcher.DaysBeforeReferenceDate = daysBeforeReferenceDate;

            string daysAfterReferenceDateString = ConfigurationManager.AppSettings["DaysAfterReferenceDate"];
            int daysAfterReferenceDate = int.Parse(daysAfterReferenceDateString);
            dispatcher.DaysAfterReferenceDate = daysAfterReferenceDate;

            string webAPIURL = ConfigurationManager.AppSettings["WebAPIURL"];
            if (webAPIURL.Length > 0 && webAPIURL.EndsWith("/"))
            {
                webAPIURL = webAPIURL.Substring(0, webAPIURL.Length - 1);
            }
            dispatcher.WebAPIURL = webAPIURL;

            string reattemptsToSendMeasuresPerPlantString = ConfigurationManager.AppSettings["ReattemptsToSendMeasuresPerPlant"];
            int reattemptsToSendMeasuresPerPlant = 1;
            int.TryParse(reattemptsToSendMeasuresPerPlantString, out reattemptsToSendMeasuresPerPlant);
            dispatcher.ReattemptsToSendMeasuresPerPlant = reattemptsToSendMeasuresPerPlant;

            logDispatcherInfo(dispatcher);

            return dispatcher;
        }

        protected override void configureSMLogger()
        {

            SimpleLogger wl = new SimpleLogger();
            wl.Log = Log;

            Logger = wl;
        }

        private void logDispatcherInfo(SigmaMeasureFilterQuartzDispatcher<SigmaMeasureFilterStartHandlerJob> dispatcher)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format("Configured Sigma Measure Filter Module"));
            sb.AppendLine(String.Format("-- LocalStartTime: {0}", dispatcher.LocalStartTime));
            sb.AppendLine(String.Format("-- RepeatIntervalMinutes: {0}", dispatcher.RepeatIntervalMinutes));

            //Logger.Info(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase));

            Logger.Info(sb.ToString());
        }
    }
}
