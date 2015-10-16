
using System;
namespace Gnarum.SigmaMeasureFilter.Model.Entities
{
    public class Meter
    {
        public virtual string IdMeter { get; set; }
        public virtual string IdPlant { get; set; }
        public virtual double Power { get; set; }
        public virtual double LosingRate { get; set; }
        public virtual DateTime startDate { get; set; }
        public virtual DateTime endDate { get; set; }
        public virtual double PowerPercentage { get; set; }

        public override bool Equals(object obj)
        {
            var p = obj as Meter;
            return p != null
                   && p.IdPlant == IdPlant
                   && p.startDate == startDate
                   && p.endDate == endDate;
        }

        public override int GetHashCode()
        {
            return IdPlant.GetHashCode()
                   * startDate.GetHashCode()
                   * endDate.GetHashCode();
        }

    }
}
