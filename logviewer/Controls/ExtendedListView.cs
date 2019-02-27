using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace logviewer.Controls
{
    /// <summary>
    /// An extension of the <see cref="ListView"/> to take care of problems using virtualized backing lists
    /// </summary>
    public class ExtendedListView : ListView
    {
        /// <summary>
        /// Clears the selection when the backing list changes to avoid complete iteration
        /// </summary>
        /// <param name="e">The argument of the event</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            UnselectAll();
            base.OnItemsChanged(e);
        }

        /// <summary>
        /// Clears the selection when the backing list changes to avoid complete iteration
        /// </summary>
        /// <param name="oldValue">The old items source</param>
        /// <param name="newValue">The new items source</param>
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            UnselectAll();
            base.OnItemsSourceChanged(oldValue, newValue);
        }
    }
}
