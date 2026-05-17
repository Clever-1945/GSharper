using GSharper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace GSharper.Helpers
{
    /// <summary>
    /// Хранилище браузеров.
    /// Браузер инициализируется долго. Если браузер нам не нужен, то мы его не удаляем, а только помечаем что он свободен к использованию
    /// </summary>
    public class WebViewMonacoStore
    {
        private static object lock_store = new object();
        private static HashSet<WebViewScope> listUsed = new HashSet<WebViewScope>();
        private static HashSet<WebViewScope> listFree = new HashSet<WebViewScope>();

        public WebViewScope GetView()
        {
            lock(lock_store)
            {
                if (listFree.Count > 0)
                {
                    var view = listFree.First();
                    listFree.Remove(view);
                    listUsed.Add(view);
                    return view;
                }
                else
                {
                    var view = new Microsoft.Web.WebView2.Wpf.WebView2();
                    var scope = view.CreateScope();
                    listUsed.Add(scope);
                    return scope;
                }
            }
        }

        public void ReleaseView(Microsoft.Web.WebView2.Wpf.WebView2 view)
        {
            lock (lock_store)
            {
                var scope = listUsed.FirstOrDefault(x => x.View == view) ?? listFree.FirstOrDefault(x => x.View == view);
                if (scope != null)
                {
                    listUsed.Remove(scope);
                    listFree.Add(scope);
                }
            }
        }
    }
}
