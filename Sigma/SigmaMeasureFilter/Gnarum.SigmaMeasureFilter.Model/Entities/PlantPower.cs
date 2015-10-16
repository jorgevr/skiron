using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gnarum.SigmaMeasureFilter.Model.Entities
{
    public class PlantPower
    {
        public virtual string IdPlant { get; set; }
        public virtual double Power { get; set; }
        public virtual DateTime startDate { get; set; }
        public virtual DateTime endDate { get; set; }
    }
}
