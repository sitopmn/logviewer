using logviewer.Interfaces;
using logviewer.query.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query
{
    public class LogItem : ILogItem, IComparable<LogItem>
    {
        private static readonly FileNameComparer _fileNameComparer = new FileNameComparer();

        public string File { get; private set; }
        public string Member { get; private set; }
        public long Position { get; private set; }
        public int Line { get; private set; }
        public Dictionary<string, object> Fields { get; private set; }

        public LogItem(string message, string file, string member, long position, int line)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            File = file;
            Member = member;
            Position = position;
            Line = line;
            Fields = new Dictionary<string, object>();
            Message = message;
        }

        public string Message
        {
            get => Fields.ContainsKey("message") ? (string)Fields["message"] : string.Empty;
            set => Fields["message"] = value;
        }

        public object GetValue(string field)
        {
            if (Fields.TryGetValue(field, out object v))
            {
                return v;
            }
            else
            {
                return null;
            }
        }

        public int CompareTo(LogItem other)
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
            return $"logviewer.query.LogItem {{{string.Join(", ", Fields.Select(f => $"{f.Key} => {f.Value}"))}}}";
        }

        public override int GetHashCode()
        {
            return Fields.GetHashCode() ^ File.GetHashCode() ^ Line.GetHashCode() ^ Member.GetHashCode() ^ Message.GetHashCode() ^ Position.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var item = obj as LogItem;
            return item != null &&
                item.Fields.SequenceEqual(Fields) &&
                item.File == File &&
                item.Line == Line &&
                item.Member == Member &&
                item.Position == Position;
        }
    }
}
