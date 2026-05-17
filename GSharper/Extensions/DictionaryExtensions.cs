using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Extensions
{
    public static class DictionaryExtensions
    {
        public static TV GetValueOrDefault<TV, TK>(this Dictionary<TK, TV> dictionary, TK key, TV defaultValue = default(TV))
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}
