using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Gnarum.SkironLoader.Model.Entities;
using System.Collections.Generic;
using System.Globalization;

namespace GetLine
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        [DeploymentItem(@"../../TestData/Wind_78m_2015_PANTE01_44.5N28.3E.txt", "TestData")]
        public void TestMethod1()
        {

            string filePath = @"TestData\Wind_78m_2015_PANTE01_44.5N28.3E.txt";

            string plantId = new FileInfo(filePath).Name.Split('_')[2].Split('.')[0];

            DateTime utcweatherForecatGenerated = DateTime.UtcNow;
            List<WeatherForecast> weatherForecastListReturned = new List<WeatherForecast>();
            string line;
            int cont = 0;
            bool first = true;
            StreamReader file = new System.IO.StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {

                if (cont > 0)
                {
                    first = extractWeatherForecastFromLine(string line);
                }

                cont++;
            }
            file.Close();
        }

        private bool extractWeatherForecastFromLine(string line)
        {
            List<WeatherForecast> weatherForecastList = new List<WeatherForecast>();
            //string[] lineSplitted = line.Split(' ');
            string[] lineSplitted = line.Split(' ');
            //Logger.Info(String.Format("DateTime {0}", line.Split(' ')[DateTimeColumn]));
            DateTime utcdaDateTime = DateTime.ParseExact(lineSplitted[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            //DateTime utcdaDateTime = DateTime.ParseExact(lineSplitted[DateTimeColumn], "yyyy/MM/dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            /*   Logger.Debug(String.Format("Line{0}", line));
            Logger.Debug(String.Format("Hours {0}, Minute {1}", lineSplitted[DateTimeColumn + 1].Split(':')[0], lineSplitted[DateTimeColumn + 1].Split(':')[1]));
            Logger.Info(String.Format("Hora {0}", int.Parse(lineSplitted[DateTimeColumn + 1].Split(':')[0])));
            Logger.Info(String.Format("Minuto {0}", int.Parse(lineSplitted[DateTimeColumn + 1].Split(':')[1])));*/

            utcdaDateTime = utcdaDateTime
                .AddHours(int.Parse(lineSplitted[0 + 1].Split(':')[0]))
                .AddMinutes(int.Parse(lineSplitted[0 + 1].Split(':')[1]));

            string DatavList = "PRES|2|*|100;T|3|+|273.15;WD|4|*|1;WM|5|*|1;RHUM|6|*|1;AD|7|*|1"; 
            List<Datavariable> datavariableList = new List<Datavariable>();
            List<String> stringDatavariableList = new List<String>();
            string[] test = DatavList.Split(';');
            stringDatavariableList.AddRange(test);

            foreach (String datavariable in stringDatavariableList)
            {
                //Logger.Warn(string.Format("datavariable '{0}' ", datavariable.Split('|')[0]));
                //ApiDatavariable apiDatavariable = _webApiProvider.GetDatavariable(datavariable.Split('|')[0]);

                /*if (apiDatavariable == null)
                {
                    throw new Exception(
                        string.Format(
                            "Unknown error: Web API response of Datavariable with ID '{0}' returned NULL",
                            datavariable.Split('|')[0]));
                }
                else
                {*/
                    datavariableList.Add(new Datavariable()
                    {
                        Id = datavariable.Split('|')[0],
                        Position = int.Parse(datavariable.Split('|')[1]),
                        Operator = datavariable.Split('|')[2],
                        Factor = Double.Parse(datavariable.Split('|')[3])
                    });
                // }
            }

            foreach (Datavariable datavariable in datavariableList)
            {
                Double value = getValue(lineSplitted, datavariable);
                // weatherForecastList.Add(createWeatherForecast(plant, weatherModel, utcdaDateTime,
                //     utcGeneratedDateTime, Resolution, datavariable.Id, value));
            }

        return true;
            
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

    }
}
