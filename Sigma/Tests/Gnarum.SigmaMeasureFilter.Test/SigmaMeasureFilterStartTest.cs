using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gnarum.SigmaMeasureFilter;
using Gnarum.SigmaMeasureFilter.Providers;
using Moq;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.SigmaMeasureFilter.Model.Repositories;
using Gnarum;
using Gnarum.SigmaMeasureFilter.Model.Services;
using Gnarum.SigmaMeasureFilter.Ninject;
using Gnarum.SAP.ConnectionProvider;
using Gnarum.Log;
using Gnarum.SigmaMeasureFilter.Model.Sap;
using Gnarum.SigmaMeasureFilter.Model;
using Gnarum.SigmaMeasureFilter.Ninject;
using Gnarum.SAP;
using System.Diagnostics;

namespace Gnarum.Wise.SigmaMeasureFilter.Test
{
    [TestClass]
    public class SigmaMeasureFilterStartTest
    {
        [TestMethod]
        public void Can_Identify_As_Ambiguous_Winter_Time_Change_Of_Windhoek()
        {
            DateTime date = new DateTime(2013, 1, 1);

            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Namibia Standard Time");

            while (!timeZone.IsAmbiguousTime(date))
            {
                date = date.AddHours(1);
            }

            Assert.AreEqual(new DateTime(2013, 4, 7, 1, 0, 0), date);
        }

        [TestMethod]
        public void Can_Get_Correct_Data_In_Summer_Daylight_Time_Change_In_Rare_TimeZone()
        {

            var timezoneMock = new Mock<TimeZone>();

            timezoneMock.Setup(s => s.StandardName).Returns("Iran Standard Time");

            Plant p = new Plant()
            {
                Id = "PLT1",

                TimeZone = timezoneMock.Object.StandardName,

            };

            var isappMock = new Mock<ISAPProvider>();

            IList<Meter> meterList = new List<Meter>()
            {
                new Meter()
                {
                    IdMeter = "ABAGI01000",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                },
                new Meter()
                {
                    IdMeter = "ABAGI01001",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                }
            };

            isappMock.Setup(s => s.GetMeterListByPlant(p)).Returns(meterList);

            IList<SigmaMeasure> listSigmaMeasure = new List<SigmaMeasure>();

            DateTime date1 = new DateTime(2012, 9, 21);

            for (int i = 0; i < 24; i++)
            {
                SigmaMeasure sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01000",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 50
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01001",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 40                  
                };

                listSigmaMeasure.Add(sm);
            }


            Mock<SigmaMeasureService> sigmaMeasureServMock = new Mock<SigmaMeasureService>(null);

            sigmaMeasureServMock.Setup(s => s.GetByMeterListInDateRange(meterList, date1, date1)).Returns(listSigmaMeasure);

            SigmaMeasureFilterStart smfs = new SigmaMeasureFilterStart();

            smfs.ISAPProv = isappMock.Object;

            smfs.SigmaMeasureService = sigmaMeasureServMock.Object;

            var webApiProvMock = new Mock<IWebAPIProvider>();

            webApiProvMock.Setup(s => s.PutJSONMeasureList(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            smfs.IWepAPIProv = webApiProvMock.Object;

            smfs.Plants = new List<Plant>() { p };

            smfs.SigmaMeasureFilterBeginningDate = date1;

            smfs.SigmaMeasureFilterEndingDate = date1;

            smfs.StartExecution();

            List<SMeasure> correctList = new List<SMeasure>();

            for (int i = 20; i < 24; i++)
            {
                correctList.Add(new SMeasure(p.Id, date1.AddDays(-1), i, 30, 90,1));
            }

            for (int i = 0; i < 20; i++)
            {
                correctList.Add(new SMeasure(p.Id, date1, i, 30, 90,1));
            }

            bool dif = false;

            if (smfs.ListOfSMeasures.Count != correctList.Count)
                dif = true;

            for (int i = 0; i < smfs.ListOfSMeasures.Count && !dif; i++)
            {
                SMeasure sm = smfs.ListOfSMeasures[i];
                if (!correctList.Contains(sm))
                {
                    dif = true;
                }
            }

            Assert.AreEqual(false, dif);
        }

        [TestMethod]
        public void Can_Get_Correct_Data_In_Winter_Time_Change() // When NumPeriod has 25 Periods
        {

            var timezoneMock = new Mock<TimeZone>();

            timezoneMock.Setup(s => s.StandardName).Returns("Iran Standard Time");

            Plant p = new Plant()
            {
                Id = "PLT1",

                TimeZone = timezoneMock.Object.StandardName,

            };

            var isappMock = new Mock<ISAPProvider>();

            IList<Meter> meterList = new List<Meter>()
            {
                new Meter()
                {
                    IdMeter = "ABAGI01000",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                },
                new Meter()
                {
                    IdMeter = "ABAGI01001",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                }
            };

            isappMock.Setup(s => s.GetMeterListByPlant(p)).Returns(meterList);

            IList<SigmaMeasure> listSigmaMeasure = new List<SigmaMeasure>();

            DateTime date1 = new DateTime(2012, 9, 21);

            for (int i = 0; i < 25; i++)
            {
                SigmaMeasure sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01000",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 50
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01001",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 40
                };

                listSigmaMeasure.Add(sm);
            }


            Mock<SigmaMeasureService> sigmaMeasureServMock = new Mock<SigmaMeasureService>(null);

            sigmaMeasureServMock.Setup(s => s.GetByMeterListInDateRange(meterList, date1, date1)).Returns(listSigmaMeasure);

            SigmaMeasureFilterStart smfs = new SigmaMeasureFilterStart();

            smfs.ISAPProv = isappMock.Object;

            smfs.SigmaMeasureService = sigmaMeasureServMock.Object;

            var webApiProvMock = new Mock<IWebAPIProvider>();

            webApiProvMock.Setup(s => s.PutJSONMeasureList(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            smfs.IWepAPIProv = webApiProvMock.Object;


            smfs.Plants = new List<Plant>() { p };

            smfs.SigmaMeasureFilterBeginningDate = date1;

            smfs.SigmaMeasureFilterEndingDate = date1;

            smfs.StartExecution();

            List<SMeasure> correctList = new List<SMeasure>();

            for (int i = 20; i < 24; i++)
            {
                correctList.Add(new SMeasure(p.Id, date1.AddDays(-1), i, 30, 90,1));
            }

            for (int i = 0; i < 21; i++)
            {
                correctList.Add(new SMeasure(p.Id, date1, i, 30, 90,1));
            }

            bool dif = false;

            if (smfs.ListOfSMeasures.Count != correctList.Count)
                dif = true;

            for (int i = 0; i < smfs.ListOfSMeasures.Count && !dif; i++)
            {
                SMeasure sm = smfs.ListOfSMeasures[i];
                if (!correctList.Contains(sm))
                {
                    dif = true;
                }
            }

            Assert.AreEqual(false, dif);
        }    

        [TestMethod]
        public void Can_Get_Corrected_Data_With_Summer_Time_Change() // When NumPeriod has 23 Periods
        {

            var timezoneMock = new Mock<TimeZone>();

            timezoneMock.Setup(s => s.StandardName).Returns("Romance Standard Time");           

            Plant p = new Plant()
            {
                Id = "PLT1",

                TimeZone = timezoneMock.Object.StandardName,

            };

            var isappMock = new Mock<ISAPProvider>();

            IList<Meter> meterList = new List<Meter>()
            {
                new Meter()
                {
                    IdMeter = "ABAGI01000",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                },
                new Meter()
                {
                    IdMeter = "ABAGI01001",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                },
                new Meter()
                {
                    IdMeter = "ABAGI01002",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                }
            };

            isappMock.Setup(s => s.GetMeterListByPlant(p)).Returns(meterList);

            IList<SigmaMeasure> listSigmaMeasure = new List<SigmaMeasure>();

            DateTime date1 = new DateTime(2012, 3, 25);

            for (int i = 0; i < 24; i++)
            {
                SigmaMeasure sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01000",
                    NumPeriod = i + 1,
                    ProductionDate = date1.AddDays(-1),
                    ProductionValue = 50
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01001",
                    NumPeriod = i + 1,
                    ProductionDate = date1.AddDays(-1),
                    ProductionValue = 40
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01002",
                    NumPeriod = i + 1,
                    ProductionDate = date1.AddDays(-1),
                    ProductionValue = 10
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01000",
                    NumPeriod = i + 1,
                    ProductionDate = date1.AddDays(1),
                    ProductionValue = 50
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01001",
                    NumPeriod = i + 1,
                    ProductionDate = date1.AddDays(1),
                    ProductionValue = 40
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01002",
                    NumPeriod = i + 1,
                    ProductionDate = date1.AddDays(1),
                    ProductionValue = 10
                };

                listSigmaMeasure.Add(sm);

            }

            for (int i = 0; i < 23; i++)
            {
                SigmaMeasure sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01000",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 50
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01001",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 40
                };

                listSigmaMeasure.Add(sm);

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01002",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 10
                };

                listSigmaMeasure.Add(sm);
            }


            Mock<SigmaMeasureService> sigmaMeasureServMock = new Mock<SigmaMeasureService>(null);

            sigmaMeasureServMock.Setup(s => s.GetByMeterListInDateRange(meterList, date1.AddDays(-1), date1.AddDays(1))).Returns(listSigmaMeasure);

            SigmaMeasureFilterStart smfs = new SigmaMeasureFilterStart();

            smfs.ISAPProv = isappMock.Object;

            smfs.SigmaMeasureService = sigmaMeasureServMock.Object;

            var webApiProvMock = new Mock<IWebAPIProvider>();

            webApiProvMock.Setup(s => s.PutJSONMeasureList(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            smfs.IWepAPIProv = webApiProvMock.Object;

            smfs.Plants = new List<Plant>() { p };

            smfs.SigmaMeasureFilterBeginningDate = date1.AddDays(-1);

            smfs.SigmaMeasureFilterEndingDate = date1.AddDays(1);

            smfs.StartExecution();

            List<SMeasure> correctList = new List<SMeasure>();

            DateTime d2 = new DateTime(2012, 3, 26, 21, 0, 0,DateTimeKind.Utc);

            for (DateTime di = new DateTime(2012, 3, 23, 23, 0, 0, DateTimeKind.Utc); di <= d2; di = di.AddHours(1))
            {
                correctList.Add(new SMeasure(p.Id,di,di.Hour,di.Minute,100,1));
            }            

            bool dif = false;

            if (smfs.ListOfSMeasures.Count != correctList.Count)
                dif = true;

            for (int i = 0; i < smfs.ListOfSMeasures.Count && !dif; i++)
            {
                SMeasure sm = smfs.ListOfSMeasures[i];
                if (!correctList.Contains(sm))
                {
                    dif = true;
                }
            }

            Assert.AreEqual(false, dif);
        }

        [TestMethod]
        public void Can_Get_Correct_Data_With_Missing_Periods()
        {

            var timezoneMock = new Mock<TimeZone>();

            timezoneMock.Setup(s => s.StandardName).Returns("Iran Standard Time");

            Plant p = new Plant()
            {
                Id = "PLT1",

                TimeZone = timezoneMock.Object.StandardName,

            };

            var isappMock = new Mock<ISAPProvider>();

            IList<Meter> meterList = new List<Meter>()
            {
                new Meter()
                {
                    IdMeter = "ABAGI01000",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                },
                new Meter()
                {
                    IdMeter = "ABAGI01001",
                    LosingRate = 0,
                    Power = 50,
                    IdPlant = p.Id,
                    PowerPercentage = 1
                }
            };

            isappMock.Setup(s => s.GetMeterListByPlant(p)).Returns(meterList);

            IList<SigmaMeasure> listSigmaMeasure = new List<SigmaMeasure>();

            DateTime date1 = new DateTime(2012, 9, 21);

            for (int i = 0; i < 24; i++)
            {
                SigmaMeasure sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01000",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 50
                };

                if (sm.NumPeriod != 3)
                {
                    listSigmaMeasure.Add(sm);
                }

                sm = new SigmaMeasure()
                {
                    Id_Meter = "ABAGI01001",
                    NumPeriod = i + 1,
                    ProductionDate = date1,
                    ProductionValue = 40
                };

                if (sm.NumPeriod != 3)
                {
                    listSigmaMeasure.Add(sm);
                }
            }


            Mock<SigmaMeasureService> sigmaMeasureServMock = new Mock<SigmaMeasureService>(null);

            sigmaMeasureServMock.Setup(s => s.GetByMeterListInDateRange(meterList, date1, date1)).Returns(listSigmaMeasure);

            SigmaMeasureFilterStart smfs = new SigmaMeasureFilterStart();

            smfs.ISAPProv = isappMock.Object;

            smfs.SigmaMeasureService = sigmaMeasureServMock.Object;


            var webApiProvMock = new Mock<IWebAPIProvider>();

            webApiProvMock.Setup(s => s.PutJSONMeasureList(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            smfs.IWepAPIProv = webApiProvMock.Object;


            smfs.Plants = new List<Plant>() { p };

            smfs.SigmaMeasureFilterBeginningDate = date1;

            smfs.SigmaMeasureFilterEndingDate = date1;

            smfs.StartExecution();

            List<SMeasure> correctList = new List<SMeasure>();

            DateTime d2 = new DateTime(2012, 9, 21, 19, 30, 0, DateTimeKind.Utc);
            DateTime notIncludedDate = new DateTime(2012, 9, 20, 22, 30, 0, DateTimeKind.Utc);

            for (DateTime di = new DateTime(2012, 9, 20, 20, 30, 0, DateTimeKind.Utc); di <= d2; di = di.AddHours(1))
            {
                if (di != notIncludedDate)
                    correctList.Add(new SMeasure(p.Id, di, di.Hour, di.Minute, 90,1));
            }

            bool dif = false;

            if (smfs.ListOfSMeasures.Count != correctList.Count)
                dif = true;

            for (int i = 0; i < smfs.ListOfSMeasures.Count && !dif; i++)
            {
                SMeasure sm = smfs.ListOfSMeasures[i];
                if (!correctList.Contains(sm))
                {
                    dif = true;
                }
            }

            Assert.AreEqual(false, dif);
        }

        [TestMethod]
        public void Can_Process_Plants()
        {
            IList<Plant> Plants = new List<Plant>() { new Plant() { Id = "AGUIR01", TimeZone = "Romance Standard Time" }, new Plant() { Id = "SOLVI02", TimeZone = "Romance Standard Time" } };
            string executionResume = "";
            DateTime execInitDatetime = DateTime.Now;
            Stopwatch sw = new Stopwatch();

            SigmaMeasureFilterStart sigmaMeasureFilterStart = new SigmaMeasureFilterStart();
            sigmaMeasureFilterStart.ISAPProv = ProvidersProvider.GetDependency<SAPProvider>();
            IWebAPIProvider IWebAPIProv = ProvidersProvider.GetDependency<IWebAPIProvider>();
            //configureSapConnection();

            string user = "consultor1";
            string password = "beconsultor1";
            string client = "100";
            string host = "192.168.1.234";
            string language = "ES";
            string systemNumber = "00";
            string peakConnectionLimit = "10";
            SapConnectionProvider.InitializeConnection(user, password, client, host, language, systemNumber, peakConnectionLimit);

            sigmaMeasureFilterStart.IWepAPIProv = IWebAPIProv;
            sigmaMeasureFilterStart.SigmaMeasureFilterBeginningDate = new DateTime(2013, 8, 12);
            sigmaMeasureFilterStart.SigmaMeasureFilterEndingDate = new DateTime(2013, 8, 13);
            //sigmaMeasureFilterStart.SigmaMeasureService = getDependency<ISigmaMeasureService>();
            sw.Start();
           
            sigmaMeasureFilterStart.ProcessPlants(Plants, executionResume);
            sw.Stop();
            DateTime execEndDatetime = DateTime.Now;
            //sigmaMeasureFilterStart.ProcessAndSendWiseReport(sigmaMeasureFilterStart._wiseReport.ToString(), execInitDatetime, sw, execEndDatetime);



        }

        //private T getDependency<T>()
        //{
        //    return new StandardKernel(
        //        new SapModule(),
        //        new ProvidersModule(),
        //        new SapRepositoryModule(),
        //        new ModelModule()
        //        ).Get<T>();
        //}
    }
}
