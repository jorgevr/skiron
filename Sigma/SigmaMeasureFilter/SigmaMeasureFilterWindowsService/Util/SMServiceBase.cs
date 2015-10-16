using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using Gnarum.Log;
using Gnarum.NHibernate.UoW;
using Gnarum.Quartz.Dispatcher;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.SigmaMeasureFilter.Model.Util;
using log4net;
using log4net.Config;
using NHibernate.Engine;
using Quartz;
using Gnarum.SAP.ConnectionProvider;


namespace SigmaMeasureFilterWindowsService.Util
{
    partial class SMServiceBase<T> : ServiceBase, IHasLogger where T : IJob
    {
        #region private properties
        protected ILogger _logger = NullLogger.Instance;
        protected System.ComponentModel.IContainer components = null;
        protected IQuartzDispatcher<T> _dispatcher = null;
        protected ILog _log = NullLog.Instance;
        protected string _logFileName = null;
        protected string _moduleName = null;
        protected int _serviceStartingDelay = 12000;
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

        #region Constructor
        public SMServiceBase(string moduleName, string serviceName, string logFileName)
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = serviceName;
            this.EventLog.Log = "Application";

            // These Flags set whether or not to handle that specific  type of event. Set to true if you need it, false otherwise.                        
            this.CanShutdown = true;
            this.CanStop = true;
            this.CanHandlePowerEvent = true; //PowerBroadcastStatus            
            this.CanHandleSessionChangeEvent = false; //SessionChangeReason
            this.CanPauseAndContinue = false;
            this._logFileName = logFileName;
            this._moduleName = moduleName;
        }
        #endregion

        #region Overrided
        protected override void OnStart(string[] args)
        {
            try
            {
                string serviceStartingDelaySecondsString = ConfigurationManager.AppSettings["ServiceStartingDelaySeconds"];
                int serviceStartingDelaySeconds;
                if (int.TryParse(serviceStartingDelaySecondsString, out serviceStartingDelaySeconds))
                {
                    _serviceStartingDelay = serviceStartingDelaySeconds*1000;
                }
                System.Threading.Thread.Sleep(_serviceStartingDelay);
                configureLog4net();
                Log.Info("--------------------WS Starting");
                Log.Info("Service Delay: " + _serviceStartingDelay);
                Log.Info("Log4net configured");
                configureRepository();
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
                _dispatcher = getQuartzDispatcher();
                _dispatcher.Dispatch();
                Logger.Info(String.Format("Dispatcher started"));
            }
            catch (Exception ex)
            {
                Logger.Error("Exception OnStart method", ex);
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

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            Log.Info("--------------------WS Disposing");
            releaseResources();

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region virtual methods

        protected virtual void configureSMLogger()//SmtpNotifier smtp)
        {
            throw new ArgumentNullException("ConfigureWiseLogger is not overrided");
        }

        protected virtual IQuartzDispatcher<T> getQuartzDispatcher()
        {

            throw new ArgumentNullException("ConfigureQuartzDispatcher is not overrided");
        }

        #endregion

        protected void configureLog4net()
        {
            // this log4net configuration will also be valid for nhibernate log4net loggers

            FileInfo f = new FileInfo(Path.Combine(ConfigUtil.Path, "LogConfig\\" + _logFileName));
            XmlConfigurator.Configure(f);
            Log = LogManager.GetLogger("System");

        }

        /// <summary>
        /// Configure nhibernate logger and nhibernate with the UnitOfWork pattern
        /// </summary>
        protected void configureNHibernate()
        {
            NHibernate.Cfg.Configuration c = new NHibernate.Cfg.Configuration();
            c.Configure(typeof(Plant).Assembly, "Gnarum.SigmaMeasureFilter.Model.hibernate.cfg.xml");
            c.SetProperty("connection.connection_string", ConfigUtil.ReadDBConnectionString());
            ISessionFactoryImplementor sessionFactory = (ISessionFactoryImplementor)c.BuildSessionFactory();
            UnitOfWorkHandler.InitializeResources(sessionFactory);
        }

        private void configureRepository()
        {
            string repositoryType = ConfigurationManager.AppSettings["RepositoryType"];

            if (repositoryType == "SAP")
                configureSapConnection();
            else if (repositoryType == "NHIB")
                configureNHibernate();
            else
                throw new ArgumentException("Repository Type is not recognized");
        }

        private void configureSapConnection()
        {
            string user = ConfigurationManager.AppSettings["SAPUser"];
            string password = ConfigurationManager.AppSettings["SAPPassword"];
            string client = ConfigurationManager.AppSettings["SAPClient"];
            string host = ConfigurationManager.AppSettings["SAPHost"];
            string language = ConfigurationManager.AppSettings["SAPLanguage"];
            string systemNumber = ConfigurationManager.AppSettings["SAPSysteMNumber"];
            string peakConnectionLimit = ConfigurationManager.AppSettings["SAPPeakConnectionLimit"];

            SapConnectionProvider.InitializeConnection(user, password, client, host, language, systemNumber, peakConnectionLimit);
        }

        private void releaseResources()
        {
            try
            {
                Logger.Info("WS Releasing Resources");

                if (_dispatcher != null)
                {
                    _dispatcher.Dispose();
                    _dispatcher = null;
                }

                UnitOfWorkHandler.ReleaseResources();
            }
            catch (Exception ex)
            {
                Logger.Error("Exception OnStop method", ex);
            }
        }

    }
}

