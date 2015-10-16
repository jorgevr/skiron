using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gnarum.SigmaMeasureFilter.Model.Repositories;
using Gnarum.SAP.RFCs;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.SAP.Model;
using Gnarum.SigmaMeasureFilter.Model.Sap.Converters;
using Gnarum.SigmaMeasureFilter.Model.Sap.OptionsGenerators;
using Gnarum.Log;

namespace Gnarum.SigmaMeasureFilter.Model.Sap.Repositories
{
    class SapSigmaMeasureRepository : ISigmaMeasureRepository
    {
        #region const properties
        private ILogger _logger = NullLogger.Instance;
        const string SAP_TABLE_NAME = "ZW3SIGMAMEASURES";

        #endregion

        #region private properties

        ISapRfcRead _reader;
        ISapRfcInsert _inserter;
        ISapRfcUpdate _updater;
        ISapSigmaMeasureConverter _converter;
        ISapSigmaMeasureOptionsGenerator _optionsGenerator;

        #endregion

        #region constructor

        public SapSigmaMeasureRepository(ISapRfcRead reader
            , ISapRfcInsert inserter
            , ISapRfcUpdate updater
            , ISapSigmaMeasureConverter converter
            , ISapSigmaMeasureOptionsGenerator optionsGenerator)
        {
            _updater = updater;
            _inserter = inserter;
            _reader = reader;
            _converter = converter;
            _optionsGenerator = optionsGenerator;
        }

        #endregion

        #region ISigmaMeasureRepository properties

        public IList<SigmaMeasure> GetByMeterListInDateRange(IList<Meter> meterList, DateTime initDate, DateTime endDate, ILogger logger)
        {
            SapRfcOptions options = _optionsGenerator.ByMeterListInDateRange(meterList, initDate, endDate);
            return getList(options,logger);
            //IList<SigmaMeasure> listSMeasures = getList(options);
            //IEnumerable<SigmaMeasure> list = listSMeasures.OrderBy(x => x.ProductionDate).ThenBy(x => x.NumPeriod);
            //return null;
        }

        #endregion

        #region private methods

        private IList<SigmaMeasure> getList(SapRfcOptions options, ILogger logger)
        {
            IList<SapReadResult> sapResultList = _reader.GetList(SAP_TABLE_NAME, options);
            return processSapResultList(sapResultList, logger);
        }

        private IList<SigmaMeasure> processSapResultList(IList<SapReadResult> sapResultList, ILogger logger)
        {
            if (sapResultList == null || sapResultList.Count == 0)
                return new List<SigmaMeasure>();
            return _converter.ExtractFromSapResult(sapResultList, logger);
        }

        #endregion
    }
}
