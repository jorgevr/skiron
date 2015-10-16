using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gnarum.SAP.Model;
using Gnarum.SigmaMeasureFilter.Model.Entities;

namespace Gnarum.SigmaMeasureFilter.Model.Sap.OptionsGenerators
{
    public interface ISapSigmaMeasureOptionsGenerator
    {
        SapRfcOptions ByMeterListInDateRange(IList<Meter> meterList, DateTime initDate, DateTime endDate);
    }

    class SapSigmaMeasureOptionsGenerator : ISapSigmaMeasureOptionsGenerator
    {
        #region consts properties

        const string IDFACILITY_FIELD_NAME = "ID_FACILITY";
        const string PRODUCTIONDATE_FIELD_NAME = "PRODUCTIONDATE";
        const string PRODUCTIONTIME_FIELD_NAME = "PRODUCTIONTIME";
        const string PRODUCTIONVALUE_FIELD_NAME = "PRODUCTIONVALUE";
        const string NUMPERIOD_FIELD_NAME = "NUMPERIOD";

        #endregion

        #region ISapSigmaMeasureOptionsGenerator methods

        public SapRfcOptions ByMeterListInDateRange(IList<Meter> meterList, DateTime initDate, DateTime endDate)
        {
            IList<String> idMeterList = new List<String>();
            foreach (var meter in meterList)
            {
                
                    idMeterList.Add(meter.IdMeter);
            }

            DateTime initd = initDate.Date;

            DateTime endd = endDate.Date.AddDays(1);

            SapRfcOptions result = new SapRfcOptions();
            result.AddIn(IDFACILITY_FIELD_NAME, idMeterList);
            result.AddSpecialDateGreaterEquals(PRODUCTIONDATE_FIELD_NAME, initd);
            result.AddSpecialDateLesser(PRODUCTIONDATE_FIELD_NAME, endd);
            return result;
        }

        #endregion
    }
}
