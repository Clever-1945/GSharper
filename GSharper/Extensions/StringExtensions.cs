using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Extensions
{
    public static class StringExtensions
    {
        public static T? ToEnum<T>(this string text) where T: struct
        {
            if (Enum.TryParse<T>(text, out var value))
            {
                return value;
            }
            return null;
        }

        public static int? ToInt(this string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return null;

            if (int.TryParse(text, out var value))
            {
                return value;
            }

            return null;
        }
    }
}
