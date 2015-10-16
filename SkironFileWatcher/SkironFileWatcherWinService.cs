using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Gnarum.Log;
using Gnarum.SkironLoader;
using Gnarum.SkironLoader.Model.Util;
using log4net;
using log4net.Config;

namespace SkironFileWatcher
{
    public partial class SkironFileWatcherWinService : ServiceBase
    {
             #region const

        public const string MFIServiceName = "SkironLoaderWindowsServiceName";
        // can be reused for each project
        private const string MFILogFileName = "skironloader.logconfig";
        private const string MFILogName = "Skiron Loader LOG";

        #endregion

        #region private properties

        protected ILogger _logger = NullLogger.Instance;
        protected ILog _log = NullLog.Instance;
        protected string _logFileName = null;
        protected string _moduleName = null;
        protected SkironLoaderStart _process = null;

        private string _webApiURL;
        private string _directoryPathToWatch;
        private string _directoryToMoveProcessedFiles;
        private string _datavariableList;
        private string _weatherModel;
        private string _resolution;
        private int _utcGeneratedHour;
        private int _dateTimeColumn;
        private int _timerInterval;
        private int _reattemptsToSendForecastPerFile;

        #endregion

        #region Properties

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public ILog Log
        {
            get { return _log; }
            set { _log = value; }
        }

        #endregion



        public SkironFileWatcherWinService()
        {
            this.ServiceName = MFIServiceName;
            this.EventLog.Log = "Application";

            // These Flags set whether or not to handle that specific  type of event. Set to true if you need it, false otherwise.                        
            this.CanShutdown = true;
            this.CanStop = true;
            this.CanHandlePowerEvent = true; //PowerBroadcastStatus            
            this.CanHandleSessionChangeEvent = false; //SessionChangeReason
            this.CanPauseAndContinue = false;
            this._logFileName = MFILogFileName;
            this._moduleName = MFILogName;
            InitializeComponent();
        }

        


        protected override void OnStart(string[] args)
        {
            try
            {
                configureLog4net();
                Log.Info("--------------------WS Starting");
                Log.Info("Log4net configured");
                configureSMLogger();
            }
            catch (Exception ex)
            {
                Log.Error("Exception OnStart method", ex);
                return;
            }

            try
            {
                Logger.Info("SMLogger configured");
                captureInfoFromAppConfig();
                _process = new SkironLoaderStart();
                _process.Logger = Logger;
                _process.WebApiURL = _webApiURL;
                _process.DirectoryPathToWatch = _directoryPathToWatch;
                _process.DirectoryToMoveProcessedFiles = _directoryToMoveProcessedFiles;
                _process.DatavariableList = _datavariableList;
                _process.UtcGeneratedHour = _utcGeneratedHour;
                _process.WeatherModel = _weatherModel;
                _process.Resolution = _resolution;
                _process.DateTimeColumn = _dateTimeColumn;
                _process.TimerInterval = _timerInterval;
                _process.ReattemptsToSendForecastPerFile = _reattemptsToSendForecastPerFile;
                _process.Start();
                Logger.Info(String.Format("{0}: Process started", DateTime.Now));
            }
            catch (Exception ex)
            {
                Logger.Error("Exception OnStart method", ex);
            }
        }

        private void captureInfoFromAppConfig()
        {
            _webApiURL = ConfigurationManager.AppSettings["WebAPIURL"];
            if (_webApiURL == null || (_webApiURL != null && (_webApiURL == "" || !_webApiURL.StartsWith("http://"))))
            {
                throw new Exception("WebAPIURL not set correctly. Please fix it in app config file");
            }
            _directoryPathToWatch = ConfigurationManager.AppSettings["DirectoryPathToWatch"];
            if (_directoryPathToWatch == null || (_directoryPathToWatch != null && (_directoryPathToWatch == "")))
            {
                throw new Exception("DirectoryPathToWatch not set correctly. Please fix it in app config file");
            }
            _directoryToMoveProcessedFiles = ConfigurationManager.AppSettings["DirectoryToMoveProcessedFiles"];
            if (_directoryToMoveProcessedFiles == null ||
                (_directoryToMoveProcessedFiles != null &&
                 (_directoryToMoveProcessedFiles == "" ||
                  (!_directoryToMoveProcessedFiles.EndsWith("\\") && _directoryToMoveProcessedFiles.EndsWith("/")))))
            {
                throw new Exception("DirectoryToMoveProcessedFiles not set correctly. Please fix it in app config file");
            }
            _datavariableList = ConfigurationManager.AppSettings["DatavariableList"];
            if (_datavariableList == null || (_datavariableList != null && (_datavariableList == "")))
            {
                throw new Exception("DatavariableList not set correctly. Please fix it in app config file");
            }
            _weatherModel = ConfigurationManager.AppSettings["WeatherModel"];
            if (_weatherModel == null || (_weatherModel != null && (_weatherModel == "")))
            {
                throw new Exception("WeatherModel not set correctly. Please fix it in app config file");
            }
            _resolution = ConfigurationManager.AppSettings["Resolution"];
            if (_resolution == null || (_resolution != null && (_resolution == "")))
            {
                throw new Exception("Resolution not set correctly. Please fix it in app config file");
            }
            _dateTimeColumn = int.Parse(ConfigurationManager.AppSettings["DateTimeColumn"]);
            if (_dateTimeColumn == null )
            {
                throw new Exception("DateTimeColumn not set correctly. Please fix it in app config file");
            }

            _utcGeneratedHour = int.Parse(ConfigurationManager.AppSettings["UtcGeneratedHour"]);
            if (_utcGeneratedHour == null)
            {
                throw new Exception("UtcGeneratedHour not set correctly. Please fix it in app config file");
            }

            _timerInterval = int.Parse(ConfigurationManager.AppSettings["TimerIntervalMilliseconds"]);
            if (_timerInterval == null || _timerInterval == 0)
            {
                throw new Exception("TimerIntervalMilliseconds not set correctly. Please fix it in app config file");
            }
            string reattemps = ConfigurationManager.AppSettings["ReattemptsToSendForecastPerFile"];
            bool correctParse = int.TryParse(reattemps, out _reattemptsToSendForecastPerFile);
            if (!correctParse)
            {
                throw new Exception(
                    "ReattemptsToSendForecastPerFile not set correctly. Please fix it in app config file");
            }

        }


        protected override void OnStop()
        {
            Logger.Fatal(this.GetType().ToString() + " Stopping");
        }

        protected override void OnShutdown()
        {
            Logger.Fatal(this.GetType().ToString() + " will stop due to Computer Shutting down");
            Stop();
            base.OnShutdown();
        }

        protected void configureLog4net()
        {
            // this log4net configuration will also be valid for nhibernate log4net loggers

            FileInfo f = new FileInfo(Path.Combine(ConfigUtil.Path, "LogConfig\\" + _logFileName));
            XmlConfigurator.Configure(f);
            Log = LogManager.GetLogger("System");

        }

        private void releaseResources()
        {
            try
            {
                Logger.Info("WS Releasing Resources");
            }
            catch (Exception ex)
            {
                Logger.Error("Exception OnStop method", ex);
            }
        }

        protected void configureSMLogger()
        {

            SimpleLogger wl = new SimpleLogger();
            wl.Log = Log;

            Logger = wl;
        }
    }
}
