using logviewer.query.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// Structure describing an index item
    /// </summary>
    public struct IndexItem : IComparable<IndexItem>
    {
        private static readonly FileNameComparer _fileNameComparer = new FileNameComparer();

        public readonly string File;
        public readonly string Member;
        public readonly long Position;
        public readonly int Line;

        public IndexItem(string file, string member, long position, int line)
        {
            File = file;
            Member = member;
            Position = position;
            Line = line;
        }

        public int CompareTo(IndexItem other)
        {
            var file = _fileNameComparer.Compare(File, other.File);
            var member = _fileNameComparer.Compare(Member, other.Member);
            var position = Position.CompareTo(other.Position);

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
                return position;
            }
        }

        public override string ToString()
        {
            return $"logviewer.query.IndexItem {{File = {File}, Member = {Member}, Position = {Position}, Line = {Line}}}";
        }

        public override bool Equals(object obj)
        {
            return
                obj is IndexItem &&
                ((IndexItem)obj).File == File &&
                ((IndexItem)obj).Member == Member &&
                ((IndexItem)obj).Position == Position &&
                ((IndexItem)obj).Line == Line;
        }

        public override int GetHashCode()
        {
            return File.GetHashCode() ^ Member.GetHashCode() ^ Position.GetHashCode() ^ Line.GetHashCode();
        }

        public static bool operator ==(IndexItem left, IndexItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexItem left, IndexItem right)
        {
            return !left.Equals(right);
        }
    }
}
