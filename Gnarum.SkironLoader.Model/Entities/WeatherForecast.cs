using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gnarum.SkironLoader.Model.Entities
{
    public class WeatherForecast
    {
        public string Plant { get; set; }
        public string DataVariable { get; set; }
        public string WeatherSource { get; set; }
        public string Resolution { get; set; }
        public string UtcGeneratedDate { get; set; }
        public int UtcGeneratedHour { get; set; }
        public string UtcDate { get; set; }
        public int UtcHour { get; set; }
        public int UtcMinute { get; set; }
        public int UtcSecond { get; set; }
        public string UtcInsertDate { get; set; }
        public double Value { get; set; }
    }
}
