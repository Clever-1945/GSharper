using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Extensions
{
    public static class ArrayExtensions
    {
        public static int FindIndex<T>(this T[] array, Func<T, bool> verifiers)
        {
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (verifiers(array[i]))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
