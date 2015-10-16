using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Gnarum.SigmaMeasureFilter.Helpers
{
    public static class DateTimeUtil
    {
        public static DateTime WinterTimeChangeDay(DateTime predDay)
        {
            //buscamos el último domingo de octubre
            DateTime lastOctoberDay = DateTime.Parse("31/10/" + predDay.Year, new CultureInfo("es-ES", true));
            return getLastSunday(lastOctoberDay).Date;
        }

        public static DateTime SummerTimeChangeDay(DateTime predDay)
        {
            //buscamos el último domingo de marzo
            DateTime lastMarchDay = DateTime.Parse("31/03/" + predDay.Year, new CultureInfo("es-ES", true));
            return getLastSunday(lastMarchDay).Date;
        }


        private static DateTime getLastSunday(DateTime lastDayMont)
        {
            DateTime lastSunday = lastDayMont.Date;
            //buscamos el último domingo del mes
            while (lastSunday.DayOfWeek != DayOfWeek.Sunday)
            {
                lastSunday = lastSunday.AddDays(-1);
            }

            return lastSunday.Date;

        }

        public static int NumberOfHoursBetween(int fromHour, int toHour)
        {
            int result = 0;

            /*
            if (fromHour == toHour)
                throw new ArgumentException(String.Format("Both values are equal {0}", fromHour));
            */

            if (fromHour <= toHour)
                result = toHour - fromHour;
            else
                result = (toHour + 24) - fromHour;

            return result;
        }

        public static DateTime getMonthFirstDay(DateTime lastDayMonth)
        {
            DateTime day = lastDayMonth.AddDays(-27);
            while (day.Month == lastDayMonth.Month)
            {
                day = day.AddDays(-1);
            }
            return day.AddDays(1);
        }

        /// <summary>
        /// Calculates sunset / sunrise time.
        /// 
        /// Implementation of algorithm found in Almanac for Computers, 1990
        /// published by Nautical Almanac Office
        /// 
        /// Implemented by Huysentruit Wouter, Fastload-Media.be
        /// </summary>
        public class SunTime
        {
            #region Declarations

            public enum ZenithValue : long
            {
                /// <summary>
                /// Official zenith (90 degrees and 50' = 90.833 degrees)
                /// </summary>
                Official = 90833,

                /// <summary>
                /// Civil zenith (96)
                /// </summary>
                Civil = 96000,

                /// <summary>
                /// Nautical zenith (102)
                /// </summary>
                Nautical = 102000,

                /// <summary>
                /// Astronomical zenith (108)
                /// </summary>
                Astronomical = 108000
            }

            internal enum Direction
            {
                Sunrise,
                Sunset
            }

            private ZenithValue zenith = ZenithValue.Official;
            private double longitude;
            private double latitude;
            private double utcOffset;
            private DateTime date;
            private DaylightTime daylightChanges;
            private int sunriseTime;
            private int sunsetTime;

            #endregion

            #region Construction

            /// <summary>
            /// Create a new SunTime object with default settings.
            /// </summary>
            public SunTime()
            {
                this.latitude = 0.0;
                this.longitude = 0.0;
                this.utcOffset = 1.0;
                this.date = DateTime.Now;
                this.daylightChanges = TimeZone.CurrentTimeZone.GetDaylightChanges(date.Year);
                Update();
            }

            /// <summary>
            /// Create a new SunTime object for the current date.
            /// </summary>
            /// <param name="latitude">Global position latitude in degrees. Latitude is positive for North and negative for South.</param>
            /// <param name="longitude">Global position longitude in degrees. Longitude is positive for East and negative for West.</param>
            /// <param name="utcOffset">The local UTC offset (f.e. +1 for Brussel, Kopenhagen, Madrid, Paris).</param>
            /// <param name="daylightChanges">The daylight saving settings to use.</param>
            public SunTime(double latitude, double longitude)
            {
                this.latitude = latitude;
                this.longitude = longitude;
                this.date = DateTime.Now;
                Update();
            }

            /// <summary>
            /// Create a new SunTime object for the given date.
            /// </summary>
            /// <param name="latitude">Global position latitude in degrees. Latitude is positive for North and negative for South.</param>
            /// <param name="longitude">Global position longitude in degrees. Longitude is positive for East and negative for West.</param>
            /// <param name="utcOffset">The local UTC offset (f.e. +1 for Brussel, Kopenhagen, Madrid, Paris).</param>
            /// <param name="daylightChanges">The daylight saving settings to use.</param>
            /// <param name="date">The date to calculate the set- and risetime for.</param>
            public SunTime(double latitude, double longitude, double utcOffset, DateTime date)
            {
                this.latitude = latitude;
                this.longitude = longitude;
                this.date = date;
                this.utcOffset = utcOffset;
                Update();
            }

            #endregion

            #region Private methods

            private static double Deg2Rad(double angle)
            {
                return Math.PI * angle / 180.0;
            }

            private static double Rad2Deg(double angle)
            {
                return 180.0 * angle / Math.PI;
            }

            private static double FixValue(double value, double min, double max)
            {
                while (value < min)
                    value += (max - min);

                while (value >= max)
                    value -= (max - min);

                return value;
            }

            private int Calculate(Direction direction)
            {
                /* doy (N) */
                int N = date.DayOfYear;

                /* appr. time (t) */
                double lngHour = longitude / 15.0;

                double t;

                if (direction == Direction.Sunrise)
                    t = N + ((6.0 - lngHour) / 24.0);
                else
                    t = N + ((18.0 - lngHour) / 24.0);

                /* mean anomaly (M) */
                double M = (0.9856 * t) - 3.289;

                /* true longitude (L) */
                double L = M + (1.916 * Math.Sin(Deg2Rad(M))) + (0.020 * Math.Sin(Deg2Rad(2 * M))) + 282.634;
                L = FixValue(L, 0, 360);

                /* right asc (RA) */
                double RA = Rad2Deg(Math.Atan(0.91764 * Math.Tan(Deg2Rad(L))));
                RA = FixValue(RA, 0, 360);

                /* adjust quadrant of RA */
                double Lquadrant = (Math.Floor(L / 90.0)) * 90.0;
                double RAquadrant = (Math.Floor(RA / 90.0)) * 90.0;
                RA = RA + (Lquadrant - RAquadrant);

                RA = RA / 15.0;

                /* sin cos DEC (sinDec / cosDec) */
                double sinDec = 0.39782 * Math.Sin(Deg2Rad(L));
                double cosDec = Math.Cos(Math.Asin(sinDec));

                /* local hour angle (cosH) */
                double cosH = (Math.Cos(Deg2Rad((double)zenith / 1000.0f)) - (sinDec * Math.Sin(Deg2Rad(latitude)))) / (cosDec * Math.Cos(Deg2Rad(latitude)));

                /* local hour (H) */
                double H;

                if (direction == Direction.Sunrise)
                    H = 360.0 - Rad2Deg(Math.Acos(cosH));
                else
                    H = Rad2Deg(Math.Acos(cosH));

                H = H / 15.0;

                /* time (T) */
                double T = H + RA - (0.06571 * t) - 6.622;

                /* universal time (T) */
                double UT = T - lngHour;

                UT += utcOffset;  // local UTC offset

                if (daylightChanges != null)
                    if ((date > daylightChanges.Start) && (date < daylightChanges.End))
                        UT += daylightChanges.Delta.TotalHours;

                UT = FixValue(UT, 0, 24);

                return (int)Math.Round(UT * 3600);  // Convert to seconds
            }

            private void Update()
            {
                sunriseTime = Calculate(Direction.Sunrise);
                sunsetTime = Calculate(Direction.Sunset);
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Combine a degrees/minutes/seconds value to an angle in degrees.
            /// </summary>
            /// <param name="degrees">The degrees part of the value.</param>
            /// <param name="minutes">The minutes part of the value.</param>
            /// <param name="seconds">The seconds part of the value.</param>
            /// <returns>The combined angle in degrees.</returns>
            public static double DegreesToAngle(double degrees, double minutes, double seconds)
            {
                if (degrees < 0)
                    return degrees - (minutes / 60.0) - (seconds / 3600.0);
                else
                    return degrees + (minutes / 60.0) + (seconds / 3600.0);
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets or sets the global position longitude in degrees.
            /// Longitude is positive for East and negative for West.
            /// </summary>
            public double Longitude
            {
                get { return longitude; }
                set
                {
                    longitude = value;
                    Update();
                }
            }

            /// <summary>
            /// Gets or sets the global position latitude in degrees.
            /// Latitude is positive for North and negative for South.
            /// </summary>
            public double Latitude
            {
                get { return latitude; }
                set
                {
                    latitude = value;
                    Update();
                }
            }

            /// <summary>
            /// Gets or sets the date where the RiseTime and SetTime apply to.
            /// </summary>
            public DateTime Date
            {
                get { return date; }
                set
                {
                    date = value;
                    Update();
                }
            }

            /// <summary>
            /// Gets or sets the local UTC offset in hours.
            /// F.e.: +1 for Brussel, Kopenhagen, Madrid, Paris.
            /// See Windows Time settings for a list of offsets.
            /// </summary>
            public double UtcOffset
            {
                get { return utcOffset; }
                set
                {
                    utcOffset = value;
                    Update();
                }
            }

            /// <summary>
            /// The time (in seconds starting from midnight) the sun will rise on the given location at the given date.
            /// </summary>
            public int RiseTimeSec
            {
                get { return sunriseTime; }
            }

            /// <summary>
            /// The time (in seconds starting from midnight) the sun will set on the given location at the given date.
            /// </summary>
            public int SetTimeSec
            {
                get { return sunsetTime; }
            }

            /// <summary>
            /// The time the sun will rise on the given location at the given date.
            /// </summary>
            public DateTime RiseTime
            {
                get { return date.Date.AddSeconds(sunriseTime); }
            }

            /// <summary>
            /// The time the sun will set on the given location at the given date.
            /// </summary>
            public DateTime SetTime
            {
                get { return date.Date.AddSeconds(sunsetTime); }
            }

            /// <summary>
            /// Gets or sets the zenith used in the sunrise / sunset time calculation.
            /// </summary>
            public ZenithValue Zenith
            {
                get { return zenith; }
                set
                {
                    zenith = value;
                    Update();
                }
            }

            /// <summary>
            /// Gets or sets the daylight saving range to use in sunrise / sunset time calculation.
            /// </summary>
            public DaylightTime DaylightChanges
            {
                get { return daylightChanges; }
                set
                {
                    daylightChanges = value;
                    Update();
                }
            }

            #endregion
        }


        public static Int32 GetNumberOfHoursInALocalDay(DateTime localDate, string timeZoneId)
        {
            return Convert.ToInt32((ConvertTimeToUtc(localDate.AddDays(1), timeZoneId) - ConvertTimeToUtc(localDate, timeZoneId)).TotalHours);
        }

        public static int GetNumberOfHoursInALocalDay(DateTime localDate, string timeZoneId, int firstLocalHour, int lastLocalHour)
        {
            return Convert.ToInt32((ConvertTimeToUtc(localDate.AddHours(lastLocalHour), timeZoneId) - ConvertTimeToUtc(localDate.AddHours(firstLocalHour), timeZoneId)).TotalHours) + 1;
        }

        public static DateTime ConvertTimeToUtc(DateTime localDate, string timeZoneId)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }

        /// <summary>
        /// Gets the numbers of hours of difference between one UtcDateTime and the UtcDateTime that results of transforming its Local Date (without time) to UtcDateTime again
        /// E.g. 17:00 Utc, for Spain TimeZone will return 18. Because 17(d) - 23(d-1) [17 (utcDateTime) => 18 (localDateTime) => 00 (localDate) => 23 (UtcDateTime)] = 18 hours of difference
        /// This is util for getting the position that occupy one local hour in one local day (for transforming hour into market periods)
        /// without using DaySavingLight Dates
        /// </summary>
        /// <param name="physicalUnit"></param>
        /// <param name="utcDateTime"></param>
        /// <returns></returns>
        public static Int32 GetNumberOfHours(TimeZoneInfo localZone, DateTime utcDateTime)
        {
            return Convert.ToInt32((utcDateTime - getUtcDateTimeFromLocalDate(localZone, utcDateTime)).TotalHours);
        }

        /// <summary>
        /// Get the local Date (without TimeSpan) of one Utc Date Time and Time Zone Info
        /// </summary>
        /// <param name="utcDateTime"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        private static DateTime getLocalDateFromUtcDateTime(DateTime utcDateTime, TimeZoneInfo zone)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, zone).Date;
        }

        /// <summary>
        /// Get the UtcDateTime from the Local Date (without Time) that results of transforming one UtcDateTime to Local Date
        /// </summary>
        /// <param name="physicalUnit"></param>
        /// <param name="utcDateTime"></param>
        /// <returns></returns>
        private static DateTime getUtcDateTimeFromLocalDate(TimeZoneInfo localzone, DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(getLocalDateFromUtcDateTime(utcDateTime, localzone), localzone);
        }
    }
}

