using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace logviewer.query.Index
{
    /// <summary>
    /// Comparer for filenames
    /// </summary>
    internal class FileNameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var invert = false;

            if (string.IsNullOrEmpty(x) && !string.IsNullOrEmpty(y))
            {
                return -1;
            }
            else if (!string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y))
            {
                return 1;
            }
            else if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y))
            {
                return 0;
            }

            var isArchiveX = Path.GetExtension(x) == ".zip";
            var isArchiveY = Path.GetExtension(y) == ".zip";
            if (isArchiveX && !isArchiveY)
            {
                return -1;
            }
            else if (!isArchiveX && isArchiveY)
            {
                return 1;
            }
            else if (isArchiveX && isArchiveY)
            {
                invert = false;
            }
            else if (!isArchiveX && !isArchiveY)
            {
                invert = true;
            }
            
            var ax = ExtractNumbers(x).ToArray();
            var ay = ExtractNumbers(y).ToArray();
            if (ax.Length > 0 && ax.Length == ay.Length)
            {
                for (var i = 0; i < ax.Length; i++)
                {
                    var result = ax[i].CompareTo(ay[i]);
                    if (result != 0)
                    {
                        return result * (invert ? -1 : 1);
                    }
                }

                return 0;
            }
            else if (ax.Length == 0 && ay.Length == 0)
            {
                return string.Compare(x, y) * (invert ? -1 : 1);
            }
            else
            {
                return ax.Length.CompareTo(ay.Length) * (invert ? -1 : 1);
            }
        }

        private IEnumerable<int> ExtractNumbers(string s)
        {
            var number = 0;
            var counter = 0;
            foreach (var c in s)
            {
                if (char.IsDigit(c))
                {
                    number = number * 10 + (c - '0');
                    counter += 1;
                }
                else if (counter > 0)
                {
                    yield return number;
                    number = 0;
                    counter = 0;
                }
            }

            if (counter > 0)
            {
                yield return number;
            }
        }
    }
}
