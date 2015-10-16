using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Gnarum.Log;
using Gnarum.Notification;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.SigmaMeasureFilter.Model.Services;
using Gnarum.SigmaMeasureFilter.Providers;
using Gnarum.Wise.NotifierSender;
using Gnarum.Wise.NotifierSender.Model;
using Gnarum.SigmaMeasureFilter.Helpers;
using System.Globalization;


namespace Gnarum.SigmaMeasureFilter
{

    public class SigmaMeasureFilterStart : IHasLogger
    {
        #region private Properties
        private ILogger _logger = NullLogger.Instance;
        private StringBuilder _wiseReport = new StringBuilder();
        private bool idPlantWritedInReport;

        #endregion

        #region public Properties
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public DateTime SigmaMeasureFilterBeginningDate { get; set; }
        public DateTime SigmaMeasureFilterEndingDate { get; set; }
        public IList<Plant> Plants { get; set; }
        public ISAPProvider ISAPProv { get; set; }
        public IWebAPIProvider IWepAPIProv { get; set; }
        public ISigmaMeasureService SigmaMeasureService { get; set; }
        public List<SMeasure> ListOfSMeasures { get; set; }
        public string WebAPIURL { get; set; }
        public int ReattemptsToSendJSON { get; set; }
        #endregion

        #region Private Methods

        /// <summary>
        /// Search a SMeasure instance in list with same datetime
        /// </summary>
        /// <param name="list">List of SMeasure objects</param>
        /// <param name="date">Date of SMeasure objects it is searching</param>
        /// <returns>SMeasure object with same date or null</returns>
        private SMeasure SMeasureInListByDate(IList<SMeasure> list, string idPlant, DateTime date)
        {
            string paramDate = string.Format("{0}{1}{2}", date.Year, date.Month.ToString().PadLeft(2, '0'), date.Day.ToString().PadLeft(2, '0'));

            var queryDay = from SMeasure in list
                           where SMeasure.Plant.Equals(idPlant)
                           && SMeasure.UtcDate.Equals(paramDate)
                           && SMeasure.UtcHour == date.Hour
                           && SMeasure.UtcMinute == date.Minute
                           select SMeasure;

            if (queryDay.Count() == 0)
            {
                return null;
            }

            return queryDay.First();
        }

       private void ProcessPlants(IList<Plant> Plants, string executionResume)
        {
           
            if (Plants != null && Plants.Count != 0)
            {
                ListOfSMeasures = new List<SMeasure>();

                List<List<SMeasure>> ListOfListOfSMeasuresToReSend = new List<List<SMeasure>>();

                foreach (var plant in Plants)
                {
                    //if (plant.Id == "YECCP01")
                    //{
                        try
                        {
                            Logger.Info(string.Format("Getting measures of plant {0}", plant.Id));
                            List<PlantPower> plantPowerList = new List<PlantPower>();


                            //Get meter list of this plant
                            IList<Meter> meterList = ISAPProv.GetMeterListByPlant(plant);
                            double totalPower = 0;
                            if (meterList.Count() == 0)
                            {
                                Logger.Warn(string.Format("Found {0} meters of plant {1}", meterList.Count(), plant.Id));
                            }
                            else
                            {
                                Logger.Info(string.Format("Found {0} meters of plant {1}", meterList.Count(), plant.Id));

                                //Get total power as a all meters power sum
                                // and a list of ids of meters

                                plantPowerList = getPlantPowerList(meterList);

                                //Get all sigma measures of all meters of plant
                                Logger.Info(string.Format("***Begining process meterlist to obtain measures of plant: {0}, Time: {1}", plant.Id, DateTime.Now));
                                IList<SigmaMeasure> sigmaMeasureList = SigmaMeasureService.GetByMeterListInDateRange(meterList, SigmaMeasureFilterBeginningDate, SigmaMeasureFilterEndingDate,Logger);
                                //foreach (SigmaMeasure sigmaMeasure in sigmaMeasureList)
                                //{
                                //    Logger.Info(string.Format("Meter: {0}, date: {1}, period: {2}, value:{3}", sigmaMeasure.Id_Meter, sigmaMeasure.ProductionDate, sigmaMeasure.NumPeriod, sigmaMeasure.ProductionValue));
                                //}
                                Logger.Info(string.Format("***End process meterlist to obtain measures of plant: {0}, Time: {1}", plant.Id, DateTime.Now));

                                if (sigmaMeasureList.Count == 0)
                                {
                                    Logger.Warn(string.Format("There are {0} sigma measures of meters in BD related with plant {1} between date {2} and {3}", sigmaMeasureList.Count, plant.Id, SigmaMeasureFilterBeginningDate.ToShortDateString(), SigmaMeasureFilterEndingDate.ToShortDateString()));
                                }
                                else
                                {
                                    Logger.Info(string.Format("There are {0} sigma measures of meters in BD related with plant {1} between date {2} and {3}", sigmaMeasureList.Count, plant.Id, SigmaMeasureFilterBeginningDate.ToShortDateString(), SigmaMeasureFilterEndingDate.ToShortDateString()));

                                    DateTime date = SigmaMeasureFilterBeginningDate;

                                    //ReadOnlyCollection<TimeZoneInfo> collect = TimeZoneInfo.GetSystemTimeZones();
                                    TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(plant.TimeZone);

                                    idPlantWritedInReport = false;
                                    while (date <= SigmaMeasureFilterEndingDate)
                                    {
                                        Logger.Info(string.Format("Processing date {0}", date.ToShortDateString()));
                                        //for each day between SigmaMeasureFilterBeginningDate to SigmaMeasureFilterEndingDate
                                        int periodsOfDate = IdentifyPeriodsByDate(date, timeZone);
                                        Logger.Debug(string.Format("Date {0} has {1} periods", date, periodsOfDate));

                                        totalPower = getTotalPowerByDate(plantPowerList, date);

                                        Logger.Debug(string.Format("Plant {0} has {1} kW for date {2} ", plant.Id, totalPower, date));

                                        ProcessMeters(totalPower, plant, meterList, sigmaMeasureList, date, timeZone, periodsOfDate, executionResume);

                                        date = date.AddDays(1);
                                    } // end of 'while (date <= SigmaMeasureFilterEndingDate)'
                                }
                            }

                            if (ListOfSMeasures.Count == 0)
                            {
                                Logger.Warn(string.Format("0 measures generated of plant {0}", plant.Id));
                                executionResume = string.Format("{0}\n\t0 measures generated of plant {1}", executionResume, plant.Id);
                            }
                            else
                            {
                                ProcessAndSendListOfSMeasures(plant, ListOfSMeasures, ListOfListOfSMeasuresToReSend, totalPower, executionResume);
                            }

                        }
                        catch (Exception e)
                        {
                            Logger.Error(string.Format("Exception obtaining measures of plant {0}", plant.Id), e);
                            executionResume = string.Format("{0}\n\t*Exception obtaining measures of plant {1}", executionResume, plant.Id);
                        }
                        if (!_wiseReport.ToString().Equals("") && !_wiseReport.ToString().EndsWith("\n"))
                        {
                            _wiseReport.Append("\n");
                        }
                    //}
                }
                
                ProcessResendListOfSMeasures(ListOfListOfSMeasuresToReSend, executionResume);


            }
            else
            {
                Logger.Warn("There are no plants with Sigma condition");
                executionResume = string.Format("{0}\n\tThere are no plants with Sigma condition", executionResume);
            }

        }

       private List<PlantPower> getPlantPowerList(IList<Meter> meterList)
       {
           var plantPowerList = new List<PlantPower>();

           Logger.Debug(String.Format("MeterList has {0} distinct elements", meterList.Count));

           List<DateTime> dateList = new List<DateTime>();
           dateList=meterList.Distinct().Select(x=>x.startDate).Distinct().ToList();
           dateList=dateList.Concat(meterList.Distinct().Select(x=>x.endDate).Distinct()).ToList();
           Logger.Debug(String.Format("DateList has {0} distinct elements", dateList.Count));
           
           for (int i=0; i<dateList.Count-1;i++)
           {
               PlantPower plantPower = new PlantPower();
               var totalPower = 0.0;
               foreach (var meter in meterList.OrderBy(x => x.startDate))
               {
                   //Logger.Debug(String.Format("startDate:{0}, EndDate{1}", dateList[i], dateList[i + 1]));
                   if (meter.startDate <= dateList[i] && meter.endDate >= dateList[i+1])
                   {
                       plantPower.IdPlant = meter.IdPlant;
                       plantPower.startDate = dateList[i];
                       plantPower.endDate = dateList[i + 1];
                       totalPower += meter.Power * 1000;
                   }
               }
               plantPower.Power = Math.Round(totalPower, 5);
               Logger.Debug(String.Format("IdPlant: {0},Startdate: {1}, EndDate: {2}, Power: {3}", plantPower.IdPlant, plantPower.startDate,plantPower.endDate,plantPower.Power));
               plantPowerList.Add(plantPower);
           }

           return plantPowerList;

       }

       private double getTotalPowerByDate(List<PlantPower> plantPowerList, DateTime date)
       {
           PlantPower plantpower = plantPowerList.OrderByDescending(x => x.startDate).FirstOrDefault(x => x.startDate <= date && x.endDate > date);
           if (plantpower == null)
           {
               return plantPowerList.OrderByDescending(x => x.startDate).FirstOrDefault(x =>x.endDate > date).Power;
           }
           return plantPowerList.OrderByDescending(x=>x.startDate).FirstOrDefault(x=>x.startDate<=date && x.endDate>date).Power;
       }

       

        //This function identify how many periods has a given date
        private int IdentifyPeriodsByDate(DateTime localDate, TimeZoneInfo timeZone)
        {

            DateTime initDate = localDate.Date;
            DateTime initDateNextDay = initDate.AddDays(1);

            //Inicialmente la variable de retorno tendrá el valor 24 ya que son los periodos normales que tienen casi todos los días
            //(excepto los de cambio de hora)
            int ret = 24;

            bool endFor = false;


            for (DateTime date = initDate; date < initDateNextDay && !endFor; date = date.AddMinutes(5)) // La fecha se incrementa de 5 en 5 min debido a que en ciertas
            // zonas geograficas el cambio de hora puede producirse en horas
            // distintas a horas en punto. (Ejemplo: Producirse el cambio a las
            // 15:30 en una cierta zona, a las 17:45 en otra, etc.)
            {
                //Para evitar que al tratar de convertir en Utc salte una excepción acerca de si su tipo es local y falta atributo sourceTimezone
                date = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);

                //Si date es identificado como ambigüo significa que en esa fecha y hora hay cambio de hora (cuando que quita 1 hora) por lo que ese día 
                //tendrá 25 periodos (1 más por la hora repedida del cambio de hora)
                if (timeZone.IsAmbiguousTime(date))
                {
                    endFor = true;
                    ret = 25;
                }
                else
                {
                    try
                    {
                        //Se realiza esta conversión a UTC buscando si salta la excepción.
                        //Esto es porque el día que hay cambio de hora (cuando se añade 1 hora) la hora a la que se le añade "no existe"
                        //por lo que al intentar convertirla a UTC saltará la excepción y sabremos asi que ese día solo tiene 23 periodos
                        //(1 menos por la hora que "no existe")
                        TimeZoneInfo.ConvertTimeToUtc(date, timeZone);
                    }
                    catch (Exception)
                    {
                        endFor = true;
                        ret = 23;
                    }

                }
            }

            return ret;
        }

        private void ProcessMeters(double totalPower,Plant plant, IList<Meter> meterList, IList<SigmaMeasure> sigmaMeasureList, DateTime date, TimeZoneInfo timeZone, int periodsOfDate, string executionResume)
        {
            foreach (SigmaMeasure sigmameasure in sigmaMeasureList) 
            {
                Logger.Info(string.Format("Sigma measures ProdutionDate '{0}' and numPeriod {1}", sigmameasure.ProductionDate, sigmameasure.NumPeriod));
            }
            
            
            bool[] periodsWithData = new bool[24];
            foreach (var meter in meterList)
            {
                Logger.Info(string.Format("Analyzing sigma measures of meter ID '{0}' and local date {1}", meter.IdMeter, date.ToShortDateString()));

                //Get list of sigma measures of a meter with id 'idMeter' where sigma measure production date (day, month and year) are equal to 'date' var
                var queryDay = from SigmaMeasure in sigmaMeasureList
                               where SigmaMeasure.ProductionDate.Day == date.Day
                               && SigmaMeasure.ProductionDate.Month == date.Month
                               && SigmaMeasure.ProductionDate.Year == date.Year
                               && SigmaMeasure.Id_Meter.Equals(meter.IdMeter)
                               orderby SigmaMeasure.NumPeriod ascending
                               select SigmaMeasure;

                if (queryDay.Count() == 0)
                {
                    Logger.Warn(string.Format("{0} results", queryDay.Count()));
                    executionResume = string.Format("{0}\n\t{1} results", executionResume, queryDay.Count());
                }
                else
                {
                    if (queryDay.Count()==25)
                        periodsWithData = new bool[25];

                    Logger.Info(string.Format("{0} results", queryDay.Count()));
                    Logger.Debug(string.Format("1.Entro"));
                    foreach (SigmaMeasure sm in queryDay)
                    {
                        periodsWithData[sm.NumPeriod] = true;
                    }
                    Logger.Debug(string.Format("2.Entro"));
                    DateTime localDate = date.Date;
                    localDate = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
                    Logger.Debug(string.Format("Entro en initial date '{0}' for converting to UTC with timezone '{1}'", localDate, timeZone));
                    DateTime initialUtcDate = new DateTime();

                    //If localDate is ambiguous date (in daylight saving time when it is converted to UTC could return 2 datetimes,
                    //.Net method 'ConvertTimeToUtc' return second option. This code lines fix it)
                    if (timeZone.IsAmbiguousTime(localDate))
                    {
                        Logger.Warn(string.Format("Ambiguous local time found: {0}.", localDate));
                        executionResume = string.Format("{0}\n\tAmbiguous local time found: {1}.", executionResume, localDate);
                        initialUtcDate = TimeZoneInfo.ConvertTimeToUtc(localDate, timeZone);
                        initialUtcDate.AddHours(-1);
                        Logger.Warn("Ambiguous local time fixed in UTC");
                        executionResume = string.Format("{0}\n\tAmbiguous local time fixed in UTC", executionResume);
                    }
                    else
                    {
                        Logger.Debug(string.Format("Entro en initial date '{0}' for converting to UTC with timezone '{1}'", localDate,timeZone));
                        initialUtcDate = TimeZoneInfo.ConvertTimeToUtc(localDate, timeZone);
                        Logger.Debug(string.Format("Local date '{0}' has been converted to UTC: '{1}'",localDate,initialUtcDate));
                    }

                    int lastNumPeriod = ProcessSigmaMeasure(totalPower, meter, queryDay, initialUtcDate, localDate, executionResume,plant);

                    for (int i = lastNumPeriod + 1; i < periodsOfDate; i++)
                    {
                        Logger.Warn(string.Format("Missing Period Number {0} in meter with ID '{1}' and local date {2}", i, meter.IdMeter, localDate.ToShortDateString()));
                        executionResume = string.Format("{0}\n\tMissing Period Number {1} in meter with ID '{2}' and local date {3}", executionResume, i, meter.IdMeter, localDate.ToShortDateString());
                    }

                }
            }

            String missingPeriods = "";
            for (int i = 0; i < periodsWithData.Count(); i++)
            {
                if (periodsWithData[i] == false)
                {
                    if (missingPeriods.Equals(""))
                    {
                        missingPeriods = string.Format("{0}", i);
                    }
                    else
                    {
                        missingPeriods = string.Format("{0},{1}", missingPeriods, i);
                    }
                }
            }
            if (!missingPeriods.Equals(""))
            {
                if (!idPlantWritedInReport)
                {
                    _wiseReport.AppendFormat("{0};", plant.Id);
                    idPlantWritedInReport = true;
                }
                _wiseReport.AppendFormat("{0};", date.ToString(CultureInfo.GetCultureInfo("es-ES")));
                _wiseReport.AppendFormat("{0};", missingPeriods);
            }
        }

        private int ProcessSigmaMeasure(double totalPower, Meter meter, IOrderedEnumerable<SigmaMeasure> queryDay, DateTime initialUtcDate, DateTime localDate, string executionResume,Plant plant)
        {
            //It saves the minutes offset os a date converted to UTC
            //When a date with hour on the hour is converted to UTC in some time zones it could 
            //be converted in half-past hour or something similar so we need to save this parameter
            int minutesOffset = initialUtcDate.Minute;

            DateTime utcDate = new DateTime();

            int lastNumPeriod = -1;

            //this block generate a new SMeasure or increment value of a created SMeasure if was created with same date and time
            //ProductionValue parameter include all meters power sum per plant and datetime
            foreach (var sigmaMeasure in queryDay)
            {
                
                //Fix errors of same numperiod in two or more different sigmaMeasures
                if (sigmaMeasure.NumPeriod != lastNumPeriod)
                {
                    while (sigmaMeasure.NumPeriod - lastNumPeriod != 1)
                    {
                        Logger.Warn(string.Format("Missing Period Number {0} in meter with ID '{1}' and local date {2}", lastNumPeriod, sigmaMeasure.Id_Meter, localDate.ToShortDateString()));
                        executionResume = string.Format("{0}\n\tMissing Period Number {1} in meter with ID '{2}' and local date {3}", executionResume, lastNumPeriod, sigmaMeasure.Id_Meter, localDate.ToShortDateString());
                        lastNumPeriod++;
                    }
                    lastNumPeriod = sigmaMeasure.NumPeriod;

                    utcDate = initialUtcDate.AddHours(sigmaMeasure.NumPeriod);

                    SMeasure sm = SMeasureInListByDate(ListOfSMeasures, plant.Id, utcDate);

                    if (sm == null)
                    {
                        sm = new SMeasure(plant.Id, utcDate.Date, utcDate.Hour, minutesOffset, sigmaMeasure.ProductionValue * meter.LosingRate * meter.PowerPercentage, ((meter.Power * 1000) / totalPower));
                        ListOfSMeasures.Add(sm);
                    }
                    else
                    {
                        sm.Value += sigmaMeasure.ProductionValue * meter.LosingRate * meter.PowerPercentage;
                        //Preguntar si "meter.PowerPercentage" es necesario para el calculo de porcentaje de medida
                        //Si porcentaje de medida indica la cantidad de medidas que hay NO sería necesario
                        //Si indicara el porcentaje de potencia con respecto al total SI
                        sm.MeasurePercentage += ((meter.Power * 1000) / totalPower);
                    }
                }
                else
                {
                    Logger.Warn(string.Format("Repeated Period Number {0} in meter with ID '{1}' and local date {2}", lastNumPeriod, sigmaMeasure.Id_Meter, localDate.ToShortDateString()));
                    executionResume = string.Format("{0}\n\tRepeated Period Number {1} in meter with ID '{2}' and local date {3}", executionResume, lastNumPeriod, sigmaMeasure.Id_Meter, localDate.ToShortDateString());
                }

            }
            return lastNumPeriod;
        }

        private void ProcessAndSendListOfSMeasures(Plant plant, List<SMeasure> ListOfSMeasures, List<List<SMeasure>> ListOfListOfSMeasuresToReSend, double totalPower, string executionResume)
        {
            //This block round measure percentage and value parameters to 4 decimal
            foreach (var smeasure in ListOfSMeasures)
            {
               
                smeasure.MeasurePercentage = Math.Round(smeasure.MeasurePercentage, 4);
                smeasure.Value = Math.Round(smeasure.Value, 4);

                CultureInfo provider = CultureInfo.InvariantCulture;
                string format = "yyyyMMdd";
                DateTime utcDateTime = DateTime.ParseExact(smeasure.UtcDate, format, provider);
                utcDateTime = utcDateTime.AddHours(smeasure.UtcHour);

                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(plant.TimeZone);
                DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);

                smeasure.ReliabilityType = MeasureValidation.GetMeasureReliabilityType(smeasure.Value, localDateTime.Hour, localDateTime.Date, plant, totalPower);
            }

            Logger.Info(string.Format("{0} measures generated of plant {1}", ListOfSMeasures.Count, plant.Id));

            //These lines convert the list of SMeasure objects in a json string
            string jsonStringOfSMeasures = ConvertSMeasuresToJSON(ListOfSMeasures);

            try
            {
                Logger.Info(string.Format("Sending measures of plant {0}", plant.Id));
                bool jsonSentSuccesfully = IWepAPIProv.PutJSONMeasureList(WebAPIURL, jsonStringOfSMeasures);
                if (jsonSentSuccesfully)
                {
                    Logger.Info("Measures sent succesfully");
                }
                else
                {
                    Logger.Warn(string.Format("Failed measures send process of plant {0}. Saved to resend", plant.Id));
                    executionResume = string.Format("{0}\n\tFailed measures send process of plant {1}. Saved to resend", executionResume, plant.Id);

                    List<SMeasure> copyOfList = ListOfSMeasures.Take(ListOfSMeasures.Count).ToList();

                    ListOfListOfSMeasuresToReSend.Add(copyOfList);

                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Exception sending measures of plant {0}. Saved to resend", plant.Id), e);
                executionResume = string.Format("{0}\n\t*Exception sending measures of plant {1}. Saved to resend", executionResume, plant.Id);

                List<SMeasure> copyOfList = ListOfSMeasures.Take(ListOfSMeasures.Count).ToList();

                ListOfListOfSMeasuresToReSend.Add(copyOfList);
            }
            finally
            {
                ListOfSMeasures.Clear();
            }
        }

        private string ConvertSMeasuresToJSON(List<SMeasure> ListOfSMeasures)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            StringBuilder sb = new StringBuilder("{\"MeasureList\":");
            sb.Append(js.Serialize(ListOfSMeasures));
            sb.Append("}");
            return sb.ToString();
        }

        private void ProcessResendListOfSMeasures(List<List<SMeasure>> ListOfListOfSMeasuresToReSend, string executionResume)
        {
            //Zona de código para el reenvio de JSON perdidos     
            foreach (var listSM in ListOfListOfSMeasuresToReSend)
            {
                string jsonStringOfSMeasures = ConvertSMeasuresToJSON(listSM);

                bool endFor = false;
                for (int i = 0; i < ReattemptsToSendJSON && !endFor; i++)
                {
                    try
                    {
                        bool jsonSentSuccesfully = IWepAPIProv.PutJSONMeasureList(WebAPIURL, jsonStringOfSMeasures);
                        if (jsonSentSuccesfully)
                        {
                            Logger.Info(string.Format("Attempt {0}: Measures of plant {1} sent succesfully", i + 1, listSM[0].Plant));
                            executionResume = string.Format("{0}\n\tAttempt {1}: Measures of plant {2} sent succesfully", executionResume, i + 1, listSM[0].Plant);
                            endFor = true;
                        }
                        else
                        {
                            Logger.Warn(string.Format("Attempt {0}: Failed measures send process of plant {1}", i + 1, listSM[0].Plant));
                            executionResume = string.Format("{0}\n\tAttempt {1}: Failed measures send process of plant {2}", executionResume, i + 1, listSM[0].Plant);

                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(string.Format("Attempt {0}: Exception sending measures of plant {1}", i + 1, listSM[0].Plant), e);
                        executionResume = string.Format("{0}\n\t*Attempt {1}: Exception sending measures of plant {2}", executionResume, i + 1, listSM[0].Plant);
                    }
                }
            }
        }

        private void SendNotification(WiseNotificationPriority wiseNotificationPriority, string subject, DateTime execInitDatetime, DateTime execEndDatetime, string executionResume, Stopwatch sw)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("Comienzo de ejecución: {0}\n", execInitDatetime);
            stringBuilder.AppendFormat("Fin de ejecución: {0}\n", execEndDatetime);
            stringBuilder.AppendFormat("Tiempo total de ejecución: {0}\n", sw.Elapsed);
            if (!executionResume.Equals(""))
            {
                stringBuilder.AppendFormat("Resumen de warning y errores: {0}\n", executionResume);
            }
            stringBuilder.AppendFormat("Número de plantas analizadas: {0}\n", Plants.Count());
            stringBuilder.Append("Fuente: SIGMA\n");
            stringBuilder.AppendFormat("Periodo de busqueda: {0} - {1}\n", SigmaMeasureFilterBeginningDate.ToShortDateString(), SigmaMeasureFilterEndingDate.ToShortDateString());

            WiseMessage wiseMessage = new WiseMessage(null, subject, stringBuilder.ToString(), "Sigma", "Medidas Incorporadas", WiseNotificationType.System, wiseNotificationPriority, null);
            try
            {
                WiseNotifierSender.sendWiseMessageToNotifier(wiseMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(String.Format("Exception sending message to notifier"), ex);
            }
        }

        private void SendReport(string html)
        {

            WiseMessage wiseMessage = new WiseMessage(null, "Wise Report", html, "SigmaReport", "Reporte de Medidas", WiseNotificationType.System, WiseNotificationPriority.Info, null);
            try
            {
                _logger.Info(string.Format("Sending Sigma Report notification with message length {0}", html.Length));
                WiseNotifierSender.sendWiseMessageToNotifier(wiseMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(String.Format("Exception sending message with report to notifier"), ex);
            }
        }

        #endregion


        public void StartExecution()
        {
            Stopwatch sw = new Stopwatch();
            DateTime execInitDatetime = DateTime.Now;

            WiseNotificationPriority wiseNotificationPriority = WiseNotificationPriority.Info;
            string subject = "[Ok]Sigma Service (Incorporación de medida)";
            string executionResume = "";

            sw.Start();

            ProcessPlants(Plants, executionResume);

            if (!executionResume.Equals(""))
            {
                if (executionResume.Contains('*'))
                {
                    wiseNotificationPriority = WiseNotificationPriority.Error;
                    subject = "[Error]Sigma Service (Incorporación de medida)";
                }
                else
                {
                    wiseNotificationPriority = WiseNotificationPriority.Warning;
                    subject = "[Warning]Sigma Service (Incorporación de medida)";
                }
            }

            sw.Stop();

            DateTime execEndDatetime = DateTime.Now;

            Logger.Info(String.Format("Sending message with report"));
            ProcessAndSendWiseReport(_wiseReport.ToString(), execInitDatetime, sw, execEndDatetime);

            SendNotification(wiseNotificationPriority, subject, execInitDatetime,execEndDatetime, executionResume, sw);
        }

        public void ProcessAndSendWiseReport(string info, DateTime execInitDatetime, Stopwatch sw, DateTime execEndDatetime)
        {
            StringBuilder html = new StringBuilder();

            html.AppendLine("<h1>\"Wise Informe\"</h1>");
            html.AppendLine("<hr align=\"center\" size=\"2\" width=\"100%\">");
            html.AppendLine("<hr align=\"center\" size=\"2\" width=\"100%\">");
            html.AppendLine("<h2>\"Módulo Measure Filter\"</h2>");
            html.AppendFormat("<p>\"Comienzo de ejecución: {0}\"</p>\n", execInitDatetime.ToString(CultureInfo.GetCultureInfo("es-ES")));
            html.AppendFormat("<p>\"Fin de ejecución: {0}\"</p>\n", execEndDatetime.ToString(CultureInfo.GetCultureInfo("es-ES")));
            html.AppendFormat("<p>\"Tiempo total de ejecución: {0}\"</p>\n", sw.Elapsed);
            html.AppendLine("<hr align=\"center\" size=\"2\" width=\"100%\">");
            html.AppendFormat("<p>\"Número de Unidades Físicas analizadas: {0}\"</p>\n", Plants.Count);
            html.AppendLine("<p>\"Fuentes de datos de medidas: SIGMA\"</p>");
            html.AppendFormat("<p>\"Primer día de búsqueda de medidas: {0}\"</p>\n", SigmaMeasureFilterBeginningDate.ToString("dd/MM/yyyy",CultureInfo.GetCultureInfo("es-ES")));
            html.AppendFormat("<p>\"Último día de búsqueda de medidas: {0}\"</p>\n", SigmaMeasureFilterEndingDate.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-ES")));
            html.AppendLine("<hr align=\"center\" size=\"2\" width=\"100%\">");

            string[] rows = info.Split('\n');
            bool endFor = false;
            for (int i = 0; i < rows.Count() && !endFor; i++)
            {
                string[] rowParts = rows[i].Split(';');
                if (rowParts.Count() > 1)
                {
                    endFor = true;
                }
            }

            if (!endFor)
            {
                html.AppendFormat("<span>No se han encontrado medidas faltantes.</span>");
            }
            else
            {
                html.AppendFormat("<span style=\"COLOR:red\">Se han encontrado medidas faltantes</span>");
                html.AppendLine("<table border=\"1\" cellpadding=\"0\">\n<tbody>");
                html.AppendLine("<tr>");
                html.AppendLine("<td style=\"PADDING-BOTTOM:0.75pt;PADDING-LEFT:0.75pt;PADDING-RIGHT:0.75pt;PADDING-TOP:0.75pt\">");
                html.AppendLine("<p style=\"TEXT-ALIGN:center\" class=\"MsoNormal\" align=\"center\"><b>Planta<u></u><u></u></b></p></td>");
                html.AppendLine("<td style=\"PADDING-BOTTOM:0.75pt;PADDING-LEFT:0.75pt;PADDING-RIGHT:0.75pt;PADDING-TOP:0.75pt\">");
                html.AppendLine("<p style=\"TEXT-ALIGN:center\" class=\"MsoNormal\" align=\"center\"><b>Día<u></u><u></u></b></p></td>");
                html.AppendLine("<td style=\"PADDING-BOTTOM:0.75pt;PADDING-LEFT:0.75pt;PADDING-RIGHT:0.75pt;PADDING-TOP:0.75pt\">");
                html.AppendLine("<p style=\"TEXT-ALIGN:center\" class=\"MsoNormal\" align=\"center\"><b>Periodos<u></u><u></u></b></p></td>");
                html.AppendLine("</tr>");

                foreach (var row in rows)
                {
                    string[] rowParts = row.Split(';');
                    if (rowParts.Count() > 1)
                    {
                        html.AppendLine("<tr>");
                        html.AppendLine("<td style=\"PADDING-BOTTOM:0.75pt;PADDING-LEFT:0.75pt;PADDING-RIGHT:0.75pt;PADDING-TOP:0.75pt\">");
                        html.AppendFormat("<p class=\"MsoNormal\">{0}<u></u><u></u></p></td>\n",rowParts[0]);

                        for (int i = 1; i < rowParts.Count(); i+=2)
                        {
                            if (!rowParts[i].Equals(""))
                            {
                                if (i != 1)
                                {
                                    html.AppendLine("<tr>");
                                    html.AppendLine("<td style=\"PADDING-BOTTOM:0.75pt;PADDING-LEFT:0.75pt;PADDING-RIGHT:0.75pt;PADDING-TOP:0.75pt\">");
                                    html.AppendLine("<p class=\"MsoNormal\"><u></u><u></u></p></td>");
                                }
                                html.AppendFormat("<td style=\"PADDING-BOTTOM:0.75pt;PADDING-LEFT:0.75pt;PADDING-RIGHT:0.75pt;PADDING-TOP:0.75pt\">\n");
                                html.AppendFormat("<p class=\"MsoNormal\">{0}<u></u><u></u></p></td>\n", rowParts[i]);
                                html.AppendFormat("<td style=\"PADDING-BOTTOM:0.75pt;PADDING-LEFT:0.75pt;PADDING-RIGHT:0.75pt;PADDING-TOP:0.75pt\">\n");
                                html.AppendFormat("<p class=\"MsoNormal\">{0}<u></u><u></u></p></td>\n", rowParts[i + 1]);
                                html.AppendLine("</tr>");
                            }
                        }
                        

                    }
                }

                html.AppendLine("</tbody>\n</table>");

            }
            SendReport(html.ToString());
        }

    }

    public enum MeasureReliabilityType
    {
        Valid = 0,
        NotValid = 1
    }

    /// <summary>
    /// Represent data of plant with a power value sum of his meters per datetime
    /// </summary>
    public class SMeasure
    {
        public string Plant { get; set; }

        public string Source { get; set; }

        public string DataVariable { get; set; }

        public string Resolution { get; set; }

        public String UtcDate { get; set; }

        public int UtcHour { get; set; }

        public int UtcMinute { get; set; }

        public int UtcSecond { get; set; }

        public double Value { get; set; }

        public double MeasurePercentage { get; set; }

        public MeasureReliabilityType ReliabilityType { get; set; }

        public SMeasure(string idPlant, DateTime date, int hour, int minutes, double value, double measurePercentage)
        {
            Plant = idPlant;
            Source = "SIGMA";
            DataVariable = "E";
            Resolution = "1H";
            UtcDate = string.Format("{0}{1}{2}", date.Year, date.Month.ToString().PadLeft(2, '0'), date.Day.ToString().PadLeft(2, '0'));
            UtcHour = hour;
            UtcMinute = minutes;
            UtcSecond = 0;
            Value = value;
            MeasurePercentage = measurePercentage;
        }

        public override bool Equals(object obj)
        {
            SMeasure sm = obj as SMeasure;

            return this.Plant.Equals(sm.Plant)
                && this.UtcDate.Equals(sm.UtcDate)
                && this.UtcHour.Equals(sm.UtcHour)
                && this.UtcMinute.Equals(sm.UtcMinute)
                && this.Value.Equals(sm.Value)
                && this.MeasurePercentage.Equals(sm.MeasurePercentage);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
