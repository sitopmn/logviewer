using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.Model
{
    /// <summary>
    /// Encapsulates a list of search bookmarks
    /// </summary>
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class BookmarkList : List<SearchContext>
    {
    }
}
