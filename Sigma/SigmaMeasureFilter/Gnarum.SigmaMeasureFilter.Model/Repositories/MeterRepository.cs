using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gnarum.SigmaMeasureFilter.Model.Entities;

namespace Gnarum.SigmaMeasureFilter.Model.Repositories
{
    public interface IMeterRepository
    {
        IList<Meter> GetMeterListByPlant(Plant p);
    }

    public class MeterRepository : IMeterRepository
    {
        public IList<Meter> GetMeterListByPlant(Plant p)
        {
            throw new NotImplementedException();
        }
    }
}
