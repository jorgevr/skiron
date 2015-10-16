using System;

namespace Gnarum.SigmaMeasureFilter.Model.Entities
{
    public class SigmaMeasure
    {
        public virtual string Id_Meter { get; set; }
        public virtual DateTime ProductionDate { get; set; }
        public virtual double ProductionValue { get; set; }
        public virtual int NumPeriod { get; set; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
