using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using System.Globalization;

namespace Gnarum.SigmaMeasureFilter.Helpers
{
    public static class MeasureValidation
    {
        public static MeasureReliabilityType GetMeasureReliabilityType(double MeasureValue, int hour, DateTime localDate, Plant plant, double plantTotalPower)
        {

            // si la medida es +- 0.01
            if ((MeasureValue != 0) && (MeasureValue > -0.01) && (MeasureValue < 0.01))
            {
                return MeasureReliabilityType.NotValid;
            }

            //si la planta es solar y tiene producciones en la noche o producciones a cero de día
            else if (plant.Technology == "FV")
            {

                DateTimeUtil.SunTime sunTime = GetSunTime(localDate.AddHours(hour), plant);
                //1 hora antes de salir
                if ((localDate.AddHours(hour).Hour == (sunTime.RiseTime.Hour - 1)) && (MeasureValue >= 0 && MeasureValue <= (plantTotalPower * 0.25)))
                {
                    return MeasureReliabilityType.Valid;
                }//a la hora de salir
                else if ((localDate.AddHours(hour).Hour == sunTime.RiseTime.Hour) && (MeasureValue >= 0 && MeasureValue <= (plantTotalPower * 0.5)))
                {
                    return MeasureReliabilityType.Valid;
                }//1 hora después de salir
                else if ((localDate.AddHours(hour).Hour == sunTime.RiseTime.Hour + 1) && (MeasureValue >= 0 && MeasureValue <= plantTotalPower))
                {
                    return MeasureReliabilityType.Valid;
                }//1 hora antes de ponerse
                else if ((localDate.AddHours(hour).Hour == (sunTime.SetTime.Hour - 1)) && (MeasureValue >= 0 && MeasureValue <= plantTotalPower))
                {
                    return MeasureReliabilityType.Valid;
                }//a la hora de ponerse
                else if ((localDate.AddHours(hour).Hour == sunTime.SetTime.Hour) && (MeasureValue >= 0 && MeasureValue <= (plantTotalPower * 0.5)))
                {
                    return MeasureReliabilityType.Valid;
                }//1 hora después de ponerse
                else if ((localDate.AddHours(hour).Hour == sunTime.SetTime.Hour + 1) && (MeasureValue >= 0 && MeasureValue <= (plantTotalPower * 0.25)))
                {
                    return MeasureReliabilityType.Valid;
                }

                else if ((MustHaveNullProductionValue(localDate.AddHours(hour), plant)) && (MeasureValue > 0))
                //if ((MustHaveNullProductionValue(hour)) && (MeasureValue > 0))
                {
                    return MeasureReliabilityType.NotValid;
                }
                else if ((MustNotHaveNullProductionValue(localDate.AddHours(hour), plant)) && (MeasureValue == 0))
                //else if ((MustNotHaveNullProductionValue(hour)) && (MeasureValue == 0))
                {
                    return MeasureReliabilityType.NotValid;
                }
                else if (MeasureValue > plantTotalPower)
                {
                    return MeasureReliabilityType.NotValid;
                }
            }
            //si la medida de producción es mayor que la potencia de la instalación en plantas eólicas, dejamos superarlo un 10%
            else if (plant.Technology == "EO")
            {
                if (MeasureValue > (plantTotalPower + (plantTotalPower * 0.1)))
                {
                    return MeasureReliabilityType.NotValid;
                }
                else
                {
                    return MeasureReliabilityType.Valid;
                }

            }
            //si la medida de producción es mayor que la potencia de la instalación para plantas no eólicas ni solares
            else if (MeasureValue > plantTotalPower)
            {
                return MeasureReliabilityType.NotValid;
            }
            return MeasureReliabilityType.Valid;
        }

        /// <summary>
        /// Comprueba si para la UF y fecha y hora local determinada debe tener valor = 0
        /// la medida. Ésto se calcula comprobando la hora de puesta y salida del sol
        /// usando la zona horaria y localización de la planta
        /// </summary>
        /// <param name="localHour"></param>
        /// <param name="plant"></param>
        /// <returns></returns>
        private static bool MustHaveNullProductionValue(DateTime localDateTime, Plant plant)
        {
            DateTime date = localDateTime;
            double lat = plant.Latitude;
            double lng = plant.Longitude;

            double utcOffset = TimeZoneInfo.FindSystemTimeZoneById(plant.TimeZone).GetUtcOffset(date).TotalHours;

            DateTimeUtil.SunTime sunTime = new DateTimeUtil.SunTime(lat, lng,utcOffset, date);

            // Añadimos 1 hora tanto a la puesta como a la salida del sol porque puede haber hasta 1 hora de margen con luz
            return !(localDateTime.Hour < (sunTime.SetTime.Hour + 1) && localDateTime.Hour > (sunTime.RiseTime.Hour - 1));
            //MAL: return (localDateTime.Hour > sunTime.SetTime.Hour +1 && localDateTime.Hour < sunTime.RiseTime.Hour -1);
        }

        private static bool MustNotHaveNullProductionValue(DateTime localDateTime, Plant plant)
        {
            DateTime date = localDateTime;
            double lat = plant.Latitude;
            double lng = plant.Longitude;

            double utcOffset = TimeZoneInfo.FindSystemTimeZoneById(plant.TimeZone).GetUtcOffset(date).TotalHours;

            DateTimeUtil.SunTime sunTime = new DateTimeUtil.SunTime(lat, lng,utcOffset, date);

            // Mayor que el amanecer y menor que el atardecer, se corresponde con las horas de luz
            return (localDateTime.Hour < sunTime.SetTime.Hour && localDateTime.Hour > sunTime.RiseTime.Hour);
        }

        private static bool TwilightHours(DateTime localDateTime, Plant plant)
        {
            DateTime date = localDateTime;
            double lat = plant.Latitude;
            double lng = plant.Longitude;

            double utcOffset = TimeZoneInfo.FindSystemTimeZoneById(plant.TimeZone).GetUtcOffset(date).TotalHours;

            DateTimeUtil.SunTime sunTime = new DateTimeUtil.SunTime(lat, lng,utcOffset, date);

            // la hora de atardecer y la hora posterios, la hora de amanecer y la hora anterior se corresponden con el crepúsculo
            return (localDateTime.Hour == sunTime.SetTime.Hour + 1 || localDateTime.Hour == sunTime.RiseTime.Hour - 1 || localDateTime.Hour == sunTime.SetTime.Hour || localDateTime.Hour == sunTime.RiseTime.Hour);
        }

        private static DateTimeUtil.SunTime GetSunTime(DateTime localDateTime, Plant plant)
        {
            DateTime date = localDateTime;
            double lat = plant.Latitude;
            double lng = plant.Longitude;

            double utcOffset = TimeZoneInfo.FindSystemTimeZoneById(plant.TimeZone).GetUtcOffset(date).TotalHours;

            DateTimeUtil.SunTime sunTime = new DateTimeUtil.SunTime(lat, lng,utcOffset, date);

            return sunTime;
        }


        /*
        private static bool MustNotHaveNullProductionValue(int hour)
        {
            double utcOffset = TimeZoneInfo.FindSystemTimeZoneById(plant.TimeZone).BaseUtcOffset.TotalHours;
            double localHour = hour + utcOffset;
            return (hour >= 12 && hour <= 15);
        }*/




    }
}