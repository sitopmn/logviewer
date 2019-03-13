using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query
{
    internal static class QueryFunctions
    {
        #region binning

        public static double? bin(double? v, double binSize)
        {
            if (v.HasValue)
            {
                var r = Math.IEEERemainder(v.Value, binSize);
                return v - r;
            }
            else
            {
                return null;
            }
        }

        public static double? bin(double? v, string binSize)
        {
            return bin(v, double.Parse(binSize, CultureInfo.InvariantCulture));
        }

        public static DateTime? bin(DateTime? v, TimeSpan bin)
        {
            if (v.HasValue)
            {
                var ticks = v.Value.Ticks / bin.Ticks;
                return new DateTime(ticks * bin.Ticks);
            }
            else
            {
                return null;
            }
        }

        public static TimeSpan? bin(TimeSpan? v, string binSize)
        {
            return bin(v, duration(binSize));
        }

        public static TimeSpan? bin(TimeSpan? v, TimeSpan bin)
        {
            if (v.HasValue)
            {
                var ticks = v.Value.Ticks / bin.Ticks;
                return new TimeSpan(ticks * bin.Ticks);
            }
            else
            {
                return null;
            }
        }

        public static DateTime? bin(DateTime? v, string binSize)
        {
            return bin(v, duration(binSize));
        }

        public static DateTime? bin(string v, string binSize)
        {
            return bin(time(v), duration(binSize));
        }

        #endregion

        #region conversions

        public static double? number(string v)
        {
            var result = 0.0;
            if (double.TryParse(v, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static TimeSpan duration(string v)
        {
            if (!TimeSpan.TryParse(v, out TimeSpan result))
            {
                return TimeSpan.Zero;
            }
            return result;
        }

        public static DateTime? time(string v, string format)
        {
            if (!DateTime.TryParseExact(v, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return DateTime.MinValue;
            }
            return result;
        }

        public static DateTime? time(string v)
        {
            return time(v, QueryFactory.DateTimeFormat);
        }

        public static DateTime? time(object v)
        {
            return time((string)v);
        }

        #endregion

        #region string functions

        public static bool contains(string v, string needle)
        {
            if (string.IsNullOrEmpty(v))
            {
                return false;
            }
            else
            {
                return v.Contains(needle);
            }
        }

        public static bool startswith(string v, string needle)
        {
            if (string.IsNullOrEmpty(v))
            {
                return false;
            }
            else
            {
                return v.StartsWith(needle);
            }
        }

        public static bool endswith(string v, string needle)
        {
            if (string.IsNullOrEmpty(v))
            {
                return false;
            }
            else
            {
                return v.EndsWith(needle);
            }
        }

        public static string trim(string v)
        {
            if (string.IsNullOrEmpty(v))
            {
                return string.Empty;
            }
            else
            {
                return v.Trim();
            }
        }

        public static int length(string v)
        {
            return v.Length;
        }

        #endregion

        #region mathematical functions

        public static double? abs(double? a) => a.HasValue ? Math.Abs(a.Value) : double.NaN;
        public static double? sqrt(double? a) => a.HasValue ? Math.Sqrt(a.Value) : double.NaN;
        public static double? sin(double? a) => a.HasValue ? Math.Sin(a.Value) : double.NaN;
        public static double? cos(double? a) => a.HasValue ? Math.Cos(a.Value) : double.NaN;
        public static double? tan(double? a) => a.HasValue ? Math.Tan(a.Value) : double.NaN;
        public static double? log(double? a) => a.HasValue ? Math.Log(a.Value) : double.NaN;
        public static double? exp(double? a) => a.HasValue ? Math.Exp(a.Value) : double.NaN;
        public static double? round(double? a) => a.HasValue ? Math.Round(a.Value) : double.NaN;
        public static double? ceil(double? a) => a.HasValue ? Math.Ceiling(a.Value) : double.NaN;
        public static double? floor(double? a) => a.HasValue ? Math.Floor(a.Value) : double.NaN;
        public static double? trunc(double? a) => a.HasValue ? Math.Truncate(a.Value) : double.NaN;

        public static double? min(double? a, double? b)
        {
            if (a.HasValue && b.HasValue)
            {
                return Math.Min(a.Value, b.Value);
            }
            else if (a.HasValue)
            {
                return a.Value;
            }
            else if (b.HasValue)
            {
                return b.Value;
            }
            else
            {
                return double.NaN;
            }
        }

        public static double? max(double? a, double? b)
        {
            if (a.HasValue && b.HasValue)
            {
                return Math.Max(a.Value, b.Value);
            }
            else if (a.HasValue)
            {
                return a.Value;
            }
            else if (b.HasValue)
            {
                return b.Value;
            }
            else
            {
                return double.NaN;
            }
        }

        public static double? pow(double? a, double? b)
        {
            if (a.HasValue && b.HasValue)
            {
                return Math.Pow(a.Value, b.Value);
            }
            else
            {
                return double.NaN;
            }
        }

        #endregion
    }
}
