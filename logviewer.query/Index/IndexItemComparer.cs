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
    internal class IndexItemComparer : IComparer<IndexItem>, IEqualityComparer<IndexItem>
    {
        private static FileNameComparer _fileNameComparer = new FileNameComparer();

        public int Compare(IndexItem x, IndexItem y)
        {
            var file = _fileNameComparer.Compare(x.File, y.File);
            var member = _fileNameComparer.Compare(x.Member, y.Member);
            if (file != 0)
            {
                return file;
            }
            else if (member != 0)
            {
                return member;
            }
            else
            {
                return x.Position.CompareTo(y.Position);
            }
        }

        public bool Equals(IndexItem x, IndexItem y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(IndexItem obj)
        {
            return obj.GetHashCode();
        }
    }
}
