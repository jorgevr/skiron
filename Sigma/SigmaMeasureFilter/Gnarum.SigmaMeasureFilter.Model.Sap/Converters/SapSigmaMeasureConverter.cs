using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.SAP.Model;
using Gnarum.Log;

namespace Gnarum.SigmaMeasureFilter.Model.Sap.Converters
{
    public interface ISapSigmaMeasureConverter
    {
        IList<SigmaMeasure> ExtractFromSapResult(IList<SapReadResult> sapResultList, ILogger logger);
    }

    class SapSigmaMeasureConverter : ISapSigmaMeasureConverter
    {
        #region consts properties

        const int IDFACILITY_POSITION = 0;
        const int PRODUCTIONDATE_POSITION = 1;
        const int NUMPERIOD_POSITION = 2;
        const int PRODUCTIONVALUE_POSITION = 3;
        //const int FLAG_POSITION = 4;
        

        const string IDFACILITY_FIELD_NAME = "ID_FACILITY";
        const string PRODUCTIONDATE_FIELD_NAME = "PRODUCTIONDATE";
        const string NUMPERIOD_FIELD_NAME = "NUMPERIOD";
        const string PRODUCTIONVALUE_FIELD_NAME = "PRODUCTIONVALUE"; 
        //const string FLAGVALUE_FIELD_NAME = "FLAGVALUE"; 

        #endregion

        #region ISapSigmaMeasureConverter methods

        public IList<SigmaMeasure> ExtractFromSapResult(IList<SapReadResult> sapResultList, ILogger logger)
        {
            IList<SigmaMeasure> result = new List<SigmaMeasure>();
            foreach (SapReadResult sapResult in sapResultList)
            {
                result.Add(ExtractFromSapResult(sapResult, logger));
            }
            return result;
        }

        public SigmaMeasure ExtractFromSapResult(SapReadResult sapResult, ILogger logger)
        {
            return new SigmaMeasure()
            {
                Id_Meter = getIdMeter(sapResult),
                ProductionDate = getProductionDate(sapResult),
                ProductionValue = getProductionValue(sapResult),
                NumPeriod = getNumPeriod(sapResult)
            };
        }

        #endregion

        #region private methods

        private int getNumPeriod(SapReadResult sapResult)
        {
            /*
             * AÑADIDA RESTA DE UNA HORA AL CAMPO NUMPERIOD QUE 
             * SE HA ELIMINADO DE LA CONSULTA SQL PARA MEJORAR  EL 
             * RENDIMIENTO Y LA VELOCIDAD DE LA EJECUCIÓN
             */
            int num = sapResult.GetInt(NUMPERIOD_POSITION) - 1;
            if (num < 0)
                num = 23;
            return num;
        }

        private double getProductionValue(SapReadResult sapResult)
        {
            return sapResult.GetDouble(PRODUCTIONVALUE_POSITION);
        }

        private DateTime getProductionDate(SapReadResult sapResult)
        {
            /*
             * AÑADIDA RESTA DE UNA HORA AL CAMPO PRODUCTIONDATE QUE 
             * SE HA ELIMINADO DE LA CONSULTA SQL PARA MEJORAR  EL 
             * RENDIMIENTO Y LA VELOCIDAD DE LA EJECUCIÓN
             */
            int num = sapResult.GetInt(NUMPERIOD_POSITION) - 1;
            if (num < 0)
                num = 23;
            
            //if (num == 23 && sapResult.GetString(FLAG_POSITION) == "V)
            if (num == 23)
            {
                return sapResult.GetSpecialDate(PRODUCTIONDATE_POSITION).AddHours(-1);
            }
            else 
            {
                return sapResult.GetSpecialDate(PRODUCTIONDATE_POSITION);
            }
            return sapResult.GetSpecialDate(PRODUCTIONDATE_POSITION).AddHours(-1);
        }

        private string getIdMeter(SapReadResult sapResult)
        {
            return sapResult.GetString(IDFACILITY_POSITION);
        }

        #endregion
    }
}
