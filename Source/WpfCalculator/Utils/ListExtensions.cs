using System.Collections.Generic;

namespace WpfCalculator
{
    public static class ListExtensions
    {
        public static void AddNonNull<T>(this List<T> list, T item)
        {
            if (item != null)
                list.Add(item);
        }
    }
}
