using System;
using System.Linq;
using System.Collections.Generic;
using Gnarum.Log;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.SigmaMeasureFilter.Model.Repositories;

namespace Gnarum.SigmaMeasureFilter.Model.Services
{
    public interface ISigmaMeasureService : IHasLogger
    {
        IList<SigmaMeasure> GetByMeterListInDateRange(IList<Meter> meterList, DateTime initDate, DateTime endDate, ILogger logger);
    }

    public class SigmaMeasureService : ISigmaMeasureService
    {
        #region private properties

        private ILogger _logger = NullLogger.Instance;
        private readonly ISigmaMeasureRepository _repository;

        #endregion

        #region ISigmaMeasureService properties

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        #endregion

        #region constructor

        public SigmaMeasureService(ISigmaMeasureRepository sigmaMeasureRepository)
        {
            _repository = sigmaMeasureRepository;
        }

        #endregion

        #region  ISigmaMeasureService methods

        public virtual IList<SigmaMeasure> GetByMeterListInDateRange(IList<Meter> meterList, DateTime initDate, DateTime endDate,ILogger logger )
        {
            return _repository.GetByMeterListInDateRange(meterList, initDate, endDate, logger).OrderBy(x=>x.Id_Meter).ThenBy(x => x.ProductionDate).ThenBy(x => x.NumPeriod).ToList();
        }

        #endregion
    }
}

