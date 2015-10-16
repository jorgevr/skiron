using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;
using Gnarum.Log;
using Gnarum.Notification;
using Gnarum.SkironLoader.Model.Entities;
using Gnarum.SkironLoader.Providers;
using Gnarum.Wise.NotifierSender;
using Gnarum.Wise.NotifierSender.Model;

namespace Gnarum.SkironLoader
{
    public class SkironLoaderStart
    {
        #region private Properties

        private Timer _timer;
        private WebAPIProvider _webApiProvider;
        private ILogger _logger = NullLogger.Instance;

        private static string DATE_FORMAT = "yyyyMMdd";
        private const string DATETIME_FORMAT = "yyyyMMddHHmmss";




        private Queue<WeatherForecastWithAttempts> _listWeatherForecastToReSend;

        #endregion

        #region public Properties

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }


        public string WebApiURL { get; set; }
        public string DirectoryPathToWatch { get; set; }
        public string DirectoryToMoveProcessedFiles { get; set; }
        public string DatavariableList { get; set; }
        public int UtcGeneratedHour { get; set; }
        public string WeatherModel { get; set; }
        public string Resolution { get; set; }
        public int DateTimeColumn { get; set; }
        public int TimerInterval { get; set; }
        public int ReattemptsToSendForecastPerFile { get; set; }


        #endregion

        public void Start()
        {
            
            if (!Directory.Exists(DirectoryPathToWatch))
            {
                string error = string.Format("Directory '{0}' not found", DirectoryPathToWatch);
                Logger.Fatal(error);
                SendNotification(error);
                return;
            }
            Logger.Info(string.Format("Directory '{0}' found", DirectoryPathToWatch));

            _webApiProvider = new WebAPIProvider(WebApiURL);
            _listWeatherForecastToReSend = new Queue<WeatherForecastWithAttempts>();

            _timer = new Timer(TimerInterval);
            _timer.Elapsed += new ElapsedEventHandler(checkExistingFiles);
            _timer.Enabled = true;
        }

        private void checkExistingFiles(object source, ElapsedEventArgs e)
        {
            var files = Directory.GetFiles(DirectoryPathToWatch);
            if (!files.Any())
                return;

            _timer.Enabled = false;
            Logger.Info(String.Format("Checking current files"));
            foreach (var file in Directory.GetFiles(DirectoryPathToWatch))
            {
                Logger.Info(String.Format("Processing file {0}",file));
                ApiPlant plant = getPlant(file);
                if (plant != null)
                {
                    try
                    {
                        processFile(file);
                    }
                    catch (Exception ex)
                    {
                        String error = String.Format("Error trying to process file '{0}'.", file);
                        Logger.Error(error, ex);
                        SendNotification(error);
                    }
                }
                else
                {
                    _logger.Warn(String.Format("No found plant {0}", plant));

                }
            }
            Logger.Info(String.Format("Done checking current files"));
            _timer.Enabled = true;
        }

        private void processFile(string fullPathToFile)
        {
            waitUntilFileIsReady(fullPathToFile);

            //Se obtiene del fichero el ID de la planta
            var name = Path.GetFileName(fullPathToFile);

            ApiWeatherModel weatherModel = getWeatherModel(WeatherModel);
            Logger.Info(String.Format("WeatherModel {0}", weatherModel.Id));
            if (weatherModel.Id == "")
            {
                throw new IOException("App config not contain WeatherModel name (or it is in wrong place)");
            }
            else
            {
                //Se busca para esa planta si entre sus measureSource se encuentra algún MeasureSource Client
                List<Datavariable> datavariableList = getDatavariableList(DatavariableList);
                foreach (Datavariable datavariable in datavariableList)
                {
                    Logger.Warn(string.Format("DataVariable '{0}' , '{1}' ,'{2}', {3} ", datavariable.Id,
                        datavariable.Position, datavariable.Operator, datavariable.Factor));
                }


                if (!datavariableList.Any())
                {
                    Logger.Warn(string.Format(
                        "Not found any datavariable in datavariable list of weatherModel with ID '{0}'", weatherModel.Id));
                }
                else
                {
                    //Se obtiene del fichero una lista de Forecast
                    List<WeatherForecast> fileWeatherForecastList = getWeatherForecastListFromFile(fullPathToFile,
                        datavariableList, weatherModel);
                    if (fileWeatherForecastList == null)
                    {
                        Logger.Warn(string.Format("WeatherForecast List generated from file '{0}' is NULL", name));
                    }
                    else if (fileWeatherForecastList.Count == 0)
                    {
                        Logger.Warn(string.Format("0 weatherForecast generated from file '{0}'", name));
                    }
                    else
                    {
                        Logger.Info(String.Format("{0} weatherForecast generated from file {1} ",
                            fileWeatherForecastList.Count, name));
                        bool sendOK = sendWeatherForecastList(fileWeatherForecastList, name);

                        if (sendOK)
                        {
                            moveFileInAnotherFolderWithOK(fullPathToFile);
                        }
                    }
                }
            }


            //Se realiza un reintento por cada lista de forecast que falló al enviarse. Si sobrepasa el umbral de reintentos se descarta.
            retrySendWeatherForecast();
        }

        private bool sendWeatherForecastList(List<WeatherForecast> fileWeatherForecastList, string fileName)
        {
            bool sendOK=false;
            List<WeatherForecast> sendList = new List<WeatherForecast>();
            foreach (WeatherForecast weatherForecast in fileWeatherForecastList)
            {
                sendList.Add(weatherForecast);

                if (sendList.Count >= 1000)
                {
                    Logger.Debug(String.Format("Sent WeatherForecast between {0} to {1}", sendList.First().UtcDate, sendList.Last().UtcDate));
                    sendOK=sendWeatherForecastList(sendList);
                    Logger.Debug(String.Format("Response {0}", sendOK));
                    sendList.Clear();
                }
            }
            sendOK=sendWeatherForecastList(sendList);
            sendList.Clear();

           
            if (sendOK)
            {
                Logger.Info(string.Format("Generated weatherForecast of file '{0}' sent succesfully", fileName));
                return true;
            }
            else
            {
                Logger.Warn(
                    string.Format("Failed send of generated weatherForecast of file '{0}'. Saving forecast to retry",
                        fileName));
                //Si no se pudo enviar se encola la lista en una cola de reintentos
                _listWeatherForecastToReSend.Enqueue(new WeatherForecastWithAttempts()
                {
                    WeatherForecastList = fileWeatherForecastList,
                    Attemp = 0,
                    fileName = fileName
                });
                return false;
            }
        }

        private bool sendWeatherForecastList(List<WeatherForecast> fileWeatherForecastList) 
        {
            //Conversión a JSON
            JavaScriptSerializer js = new JavaScriptSerializer();
            StringBuilder sb = new StringBuilder("{\"WeatherForecastList\":");
            sb.Append(js.Serialize(fileWeatherForecastList));
            sb.Append("}");
            string jsonStringOfWeatherForecast = sb.ToString();
            //_logger.Debug(String.Format("PutWeatherForecast: {0}",jsonStringOfWeatherForecast));

            //Se realiza un PUT del JSON generado en la Web API
            return _webApiProvider.PutWeatherForecastList(jsonStringOfWeatherForecast);
            
        }


        private List<WeatherForecast> getWeatherForecastListFromFile(string fullPathToFile,
            List<Datavariable> datavariableList, ApiWeatherModel weatherModel)
        {
            ApiPlant plant = getPlant(fullPathToFile);
            DateTime utcweatherForecatGenerated = DateTime.UtcNow;
            List<WeatherForecast> weatherForecastListReturned = new List<WeatherForecast>();
            string line;
            int cont = 0;
            bool first = true;
            StreamReader file = new System.IO.StreamReader(fullPathToFile);
            while ((line = file.ReadLine()) != null)
            {

                if (cont > 0)
                {
                    if (first)
                    {
                        Logger.Info(String.Format("DateTime {0}", line.Split(' ')[DateTimeColumn]));
                        utcweatherForecatGenerated = DateTime.ParseExact(line.Split(' ')[DateTimeColumn], "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);
                        //Logger.Info(String.Format("Fist DateTime {0}", line.Split(' ')[DateTimeColumn]));
                       // utcweatherForecatGenerated = DateTime.ParseExact(line.Split(' ')[DateTimeColumn], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        utcweatherForecatGenerated = utcweatherForecatGenerated.Date.AddHours(UtcGeneratedHour);
                        first = false;
                    }
                    //Si añaden la columna de UTCGeneratedDateTime
                    //utcweatherForecatGenerated = DateTime.ParseExact(line.Split('\t')[DateTimeColumn - 1], "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    weatherForecastListReturned.AddRange(extractWeatherForecastFromLine(line, datavariableList, plant,
                        weatherModel, utcweatherForecatGenerated));
                }
                cont++;
            }
            file.Close();
            return weatherForecastListReturned;
        }


        private IEnumerable<WeatherForecast> extractWeatherForecastFromLine(string line,
            List<Datavariable> datavariableList,ApiPlant plant, ApiWeatherModel weatherModel, DateTime utcGeneratedDateTime)
        {
            List<WeatherForecast> weatherForecastList = new List<WeatherForecast>();
            //string[] lineSplitted = line.Split(' ');
            string[] lineSplitted = line.Split(' ');
            Logger.Info(String.Format("DateTime {0}", line.Split(' ')[DateTimeColumn]));
            DateTime utcdaDateTime = DateTime.ParseExact(lineSplitted[DateTimeColumn], "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);
            //DateTime utcdaDateTime = DateTime.ParseExact(lineSplitted[DateTimeColumn], "yyyy/MM/dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            Logger.Debug(String.Format("Line{0}", line));
            Logger.Debug(String.Format("Hours {0}, Minute {1}", lineSplitted[DateTimeColumn + 1].Split(':')[0], lineSplitted[DateTimeColumn + 1].Split(':')[1]));
            Logger.Info(String.Format("Hora {0}", int.Parse(lineSplitted[DateTimeColumn + 1].Split(':')[0])));
            Logger.Info(String.Format("Minuto {0}", int.Parse(lineSplitted[DateTimeColumn + 1].Split(':')[1])));

            utcdaDateTime = utcdaDateTime
                .AddHours(int.Parse(lineSplitted[DateTimeColumn + 1].Split(':')[0]))
                .AddMinutes(int.Parse(lineSplitted[DateTimeColumn + 1].Split(':')[1]));

            foreach (Datavariable datavariable in datavariableList)
            {
                Double value = getValue(lineSplitted, datavariable);
                weatherForecastList.Add(createWeatherForecast(plant, weatherModel, utcdaDateTime,
                    utcGeneratedDateTime, Resolution, datavariable.Id, value));
            }
            return weatherForecastList;
        }

        private double getValue(string[] lineSplitted, Datavariable datavariable)
        {

            NumberStyles style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            //Logger.Debug(String.Format("Value {0}", lineSplitted[datavariable.Position]));
            double value = double.Parse(lineSplitted[datavariable.Position],style,culture);
            switch (datavariable.Operator)
            {
                case "+":
                    value = value + datavariable.Factor;
                    break;
                case "-":
                    value = value - datavariable.Factor;
                    break;
                case "*":
                    value = value*datavariable.Factor;
                    break;
                case "/":
                    value = value/datavariable.Factor;
                    break;
                default:
                    value = value*datavariable.Factor;
                    break;
            }
            return value;
        }

        private ApiPlant getPlant(string file)
        {
            string plantId = new FileInfo(file).Name.Split('_')[2].Split('.')[0];
            return _webApiProvider.GetPlant(plantId);
        }

        private ApiWeatherModel getWeatherModel(string weatherModel)
        {
            return _webApiProvider.GetApiWeatherModel(weatherModel);
        }

        private
        List<Datavariable> getDatavariableList(string DatavariableList)
        {
            List<Datavariable> datavariableList = new List<Datavariable>();
            List<String> stringDatavariableList = DatavariableList.Split(';').ToList();
            foreach (String datavariable in stringDatavariableList)
            {
                Logger.Warn(string.Format("datavariable '{0}' ", datavariable.Split('|')[0]));
                ApiDatavariable apiDatavariable = _webApiProvider.GetDatavariable(datavariable.Split('|')[0]);

                if (apiDatavariable == null)
                {
                    throw new Exception(
                        string.Format(
                            "Unknown error: Web API response of Datavariable with ID '{0}' returned NULL",
                            datavariable.Split('|')[0]));
                }
                else
                {
                    datavariableList.Add(new Datavariable()
                    {
                        Id = datavariable.Split('|')[0],
                        Position = int.Parse(datavariable.Split('|')[1]),
                        Operator = datavariable.Split('|')[2],
                        Factor = Double.Parse(datavariable.Split('|')[3])
                    });
                }
            }
            return datavariableList;
        }


        private WeatherForecast createWeatherForecast(ApiPlant plant, ApiWeatherModel weatherModel,
            DateTime weatherForecastDateTime, DateTime utcGeneratedDateTime, string resolution, string datavariable,
            double value)
        {
            return new WeatherForecast()
            {
                Plant = plant.Id,
                DataVariable = datavariable,
                WeatherSource = weatherModel.Id,
                Resolution = resolution,
                UtcGeneratedDate = utcGeneratedDateTime.Date.ToString(DATE_FORMAT),
                UtcGeneratedHour = utcGeneratedDateTime.Hour,
                UtcDate = weatherForecastDateTime.Date.ToString(DATE_FORMAT),
                UtcHour = weatherForecastDateTime.Hour,
                UtcMinute = weatherForecastDateTime.Minute,
                UtcSecond = weatherForecastDateTime.Second,
                UtcInsertDate = DateTime.UtcNow.ToString(DATETIME_FORMAT),
                Value = value,

            };
        }

        private void waitUntilFileIsReady(string fullPathToFile)
        {
            while (!isReady(fullPathToFile))
                //Mientras el fichero aún se este pasando al directorio monitorizado realiza una espera
            {
                System.Threading.Thread.Sleep(100);
            }
            Logger.Info(string.Format("File '{0}' is open ready.", fullPathToFile));
        }

        private void moveFileInAnotherFolderWithOK(string fullPathToFile)
        {
            var name = Path.GetFileName(fullPathToFile);
            try
            {
                var movedFile = string.Format("{0}{1}", DirectoryToMoveProcessedFiles, string.Format("{0}.OK", name));
                _logger.Debug(string.Format("Moving file to {0}", movedFile));
                var version = 0;
                while (File.Exists(movedFile))
                {
                    _logger.Warn(string.Format("The file {0} exists. Creating new version", movedFile));
                    version++;
                    movedFile = string.Format("{0}{1}", DirectoryToMoveProcessedFiles,
                        string.Format("{0}.OK.{1}", name, version));
                }
                File.Move(fullPathToFile, movedFile);
            }
            catch (Exception ex)
            {
                String error = String.Format("Error trying to move file '{0}' to directory '{1}' Exception: {2}", name,
                    DirectoryToMoveProcessedFiles, ex.Message);
                Logger.Error(error);
                SendNotification(error);
            }

        }

        private void SendNotification(string error)
        {

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("Fecha y hora de error: {0}\n", DateTime.Now);
            stringBuilder.AppendFormat("Resumen error: {0}\n", error);
            WiseMessage wiseMessage = new WiseMessage(WeatherModel, "Metoffice WeatherForecast Loader Error",
                stringBuilder.ToString(), "Metoffice WeatherForecast Loader", "Error", WiseNotificationType.System,
                WiseNotificationPriority.Error, null);
            try
            {
                if (WiseNotifierSender.sendWiseMessageToNotifier(wiseMessage))
                {
                    Logger.Info("Notification of error message sent succesfully");
                }
                else
                {
                    Logger.Warn("Can not send error notification message");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(String.Format("Exception sending message to notifier"), ex);
            }
        }

        private void retrySendWeatherForecast()
        {
            int counter = _listWeatherForecastToReSend.Count;
            while (counter > 0)
            {
                counter--;
                WeatherForecastWithAttempts fileWeatherForecastListAttemp = _listWeatherForecastToReSend.Dequeue();
                List<WeatherForecast> listWeatherForecast = fileWeatherForecastListAttemp.WeatherForecastList;

                JavaScriptSerializer js = new JavaScriptSerializer();
                StringBuilder sb = new StringBuilder("{\"MeasureList\":");
                sb.Append(js.Serialize(listWeatherForecast));
                sb.Append("}");
                string jsonStringOfWeatherForecast = sb.ToString();

                Logger.Info(String.Format("Attempt {1}: Sending weatherForecast generated from file {1}",
                    fileWeatherForecastListAttemp.Attemp, fileWeatherForecastListAttemp.fileName));
                bool send = _webApiProvider.PutWeatherForecastList(jsonStringOfWeatherForecast);

                if (send)
                {
                    Logger.Info(string.Format("Generated weatherForecast of file '{0}' sent succesfully in attempt {1}",
                        fileWeatherForecastListAttemp.fileName, fileWeatherForecastListAttemp.Attemp));
                }
                else
                {
                    fileWeatherForecastListAttemp.Attemp++;

                    Logger.Warn(string.Format("Generated weatherForecast of file '{0}' send failed in attempt {1}",
                        fileWeatherForecastListAttemp.fileName, fileWeatherForecastListAttemp.Attemp));

                    if (fileWeatherForecastListAttemp.Attemp < ReattemptsToSendForecastPerFile)
                    {
                        _listWeatherForecastToReSend.Enqueue(fileWeatherForecastListAttemp);
                    }
                    else
                    {
                        Logger.Warn(
                            string.Format(
                                "Max send attemps reached of generated weatherForecast of file '{0}'. Removing...",
                                fileWeatherForecastListAttemp.fileName));
                    }
                }
            }
        }

        private bool isReady(String file)
        {
            try
            {
                using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class WeatherForecastWithAttempts
    {
        public List<WeatherForecast> WeatherForecastList;
        public int Attemp;
        public string fileName;
    }

}

