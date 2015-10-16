using System;
using System.Collections.Generic;
using Gnarum.Log;
using Gnarum.NHibernate.UoW;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.SigmaMeasureFilter.Model.Queries;

namespace Gnarum.SigmaMeasureFilter.Model.Repositories
{
    public interface ISigmaMeasureRepository
    {
        IList<SigmaMeasure> GetByMeterListInDateRange(IList<Meter> meterList, DateTime initDate, DateTime endDate, ILogger logger);
    }

    public class SigmaMeasureRepository : ISigmaMeasureRepository
    {

        public IList<SigmaMeasure> GetByMeterListInDateRange(IList<Meter> meterList, DateTime initDate, DateTime endDate, ILogger logger)
        {
            using (UnitOfWorkHandler.Start())
            {
                return UoWRepository.List<SigmaMeasure>(SigmaMeasureQueryOver.ByMeterListBetweenDates(meterList, initDate, endDate));
            }
        }
    }
}
