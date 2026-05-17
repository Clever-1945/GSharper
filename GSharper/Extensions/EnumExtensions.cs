using GSharper.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GSharper.Extensions
{
    public static class EnumExtensions
    {
        public static T[] GetValues<T>(this T instance) where T: struct
        {
            return Enum.GetNames(typeof(T))
                .Select(x => x.ToEnum<T>())
                .Where(x => x != null)
                .Select(x => x.Value)
                .ToArray();
        }
    }
}
