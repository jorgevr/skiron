using System;
using System.Collections.Generic;
using System.Linq;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using NHibernate.Criterion;

namespace Gnarum.SigmaMeasureFilter.Model.Queries
{
    public static class SigmaMeasureQueryOver
    {

        internal static QueryOver<SigmaMeasure> ByMeterListBetweenDates(IList<Meter> meterList, DateTime initDate, DateTime endDate)
        {
            var idMeterList = from meter in meterList
                          select meter.IdMeter;

            DateTime initd = initDate.Date;

            DateTime endd = endDate.Date.AddDays(1);

            return QueryOver.Of<SigmaMeasure>()
                .Where(x =>
                    x.Id_Meter.IsIn(idMeterList.ToArray())
                    && (x.ProductionDate >= initd)
                    && (x.ProductionDate < endd))
                .OrderBy(x => x.ProductionDate).Asc.ThenBy(x => x.NumPeriod).Asc;
        }
    }
}
