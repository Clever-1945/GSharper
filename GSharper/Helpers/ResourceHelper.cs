using GSharper.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GSharper.Helpers
{
    public static class ResourceHelper
    {
        private static ConcurrentDictionary<string, ImageSource> _sources = new ConcurrentDictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Получить ресурс по пути ресурса
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public static ImageSource GetSource(string resourcePath)
        {
            return _sources.GetOrAdd((resourcePath ?? ""), (path) => 
            {
                if (String.IsNullOrWhiteSpace(path))
                    return null;
                
                using (var stream = typeof(EnumExtensions).Assembly.GetManifestResourceStream(path))
                {
                    if (stream == null) return null;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
            });
        }
    }
}
