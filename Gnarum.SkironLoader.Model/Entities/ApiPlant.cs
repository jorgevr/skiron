using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gnarum.SkironLoader.Model.Entities
{
    public class ApiPlant
    {
        public string Id { get; set; }
        public string Technology { get; set; }
        public string CountryCode { get; set; }
        public string RegionCode { get; set; }
        public string TimeZone { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Power { get; set; }
    }
}
