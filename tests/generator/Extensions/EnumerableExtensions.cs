using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nuke.Generator.Shims
{
    public static partial class Extensions
    {
        public static IEnumerable<T> DescendantsAndSelf<T>(
            this T obj,
            Func<T, T> selector,
            Func<T, bool> traverse = null)
        {
            yield return obj;

            foreach (var p in obj.Descendants(selector, traverse))
                yield return p;
        }
        public static IEnumerable<T> Descendants<T>(
            this T obj,
            Func<T, T> selector,
            Func<T, bool> traverse = null)
        {
            if (traverse != null && !traverse(obj))
                yield break;

            var next = selector(obj);
            if (traverse == null && Equals(next, default(T)))
                yield break;

            foreach (var nextOrDescendant in next.DescendantsAndSelf(selector, traverse))
                yield return nextOrDescendant;
        }
        public static DirectoryInfo FindParentDirectory(DirectoryInfo start, Func<DirectoryInfo, bool> predicate)
        {
            return start
                .DescendantsAndSelf(x => x.Parent)
                .Where(x => x != null)
                .FirstOrDefault(predicate);
        }
    }
}