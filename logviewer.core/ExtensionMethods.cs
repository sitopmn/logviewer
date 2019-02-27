using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace logviewer.core
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the visual child with the given type of the given parent element
        /// </summary>
        /// <typeparam name="T">Type of the child</typeparam>
        /// <param name="parent">Parent element to search</param>
        /// <returns>Child or null</returns>
        public static T GetVisualChild<T>(this DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        /// <summary>
        /// Gets the visual child with the given type of the given parent element
        /// </summary>
        /// <typeparam name="T">Type of the child</typeparam>
        /// <param name="parent">Parent element to search</param>
        /// <param name="predicate">Predicate the child must satisfy</param>
        /// <returns>Child or null</returns>
        public static T GetVisualChild<T>(this DependencyObject parent, Func<T, bool> predicate) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null && predicate(child))
                {
                    break;
                }
            }
            return child;
        }

        /// <summary>
        /// Adds a range of items to a <see cref="HashSet{T}"/>
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="set">Set to add to</param>
        /// <param name="elements">Items to add</param>
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> elements)
        {
            foreach (var e in elements)
            {
                set.Add(e);
            }
        }

        /// <summary>
        /// Adds a range of items to a <see cref="IList{T}"/>
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="list">List to add to</param>
        /// <param name="elements">Items to add</param>
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> elements)
        {
            foreach (var e in elements)
            {
                list.Add(e);
            }
        }

        public static T FirstOr<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate, T defaultValue)
        {
            foreach (var e in enumerable.Where(predicate))
            {
                return e;
            }

            return defaultValue;
        }

        public static T FirstOr<T>(this IEnumerable<T> enumerable, T defaultValue)
        {
            try
            {
                return enumerable.First();
            }
            catch (InvalidOperationException)
            {
                return defaultValue;
            }
        }

        public static IEnumerable<T> Skip<T>(this IList<T> list, int count)
        {
            for (var i = count; i < list.Count; i++)
            {
                yield return list[i];
            }
        }

        public static T FindParent<T>(this DependencyObject start) where T : class
        {
            while (start != null)
            {
                if (start.GetType() == typeof(T))
                {
                    return start as T;
                }

                start = VisualTreeHelper.GetParent(start);
            }

            return default(T);
        }

        public static Type GetType<TAttr>(this Assembly assembly, Func<TAttr, bool> predicate) where TAttr : Attribute
        {
            return assembly.GetTypes().Select(t => new { t, a = t.GetCustomAttribute<TAttr>() }).Where(a => a.a != null && predicate(a.a)).Select(a => a.t).FirstOrDefault();
        }

        public static TList Extend<TList, TItem>(this TList list, TItem item) where TList : ICollection<TItem>
        {
            if (item != null)
            {
                list.Add(item);
            }

            return list;
        }

        public static int[] ArraySum(this IEnumerable<IReadOnlyList<int>> list)
        {
            var result = new List<int>();
            foreach (var e in list)
            {
                for (var i = 0; i < e.Count; i++)
                {
                    while (result.Count <= i) result.Add(0);
                    result[i] += e[i];
                }
            }

            return result.ToArray();
        }
    }
}
