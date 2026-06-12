using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace GSharper.Extensions
{
    public static class WindowExtensions
    {
        // public const string SearchOverlayLayerName = "GSharperSelection";

        // [Export(typeof(AdornmentLayerDefinition))]
        // [Name(SearchOverlayLayerName)]
        // [Order(After = "Caret")] // Задаем строкой, чтобы избежать ошибок с PredefinedAdornmentLayerNames
        // public static AdornmentLayerDefinition CustomLayerDefinition;


        public static void ShowInCenter(this Window window, int? percentSize = null)
        {
            window.Owner = window.Owner ?? Application.Current.MainWindow;
            window.Loaded += (object sender, RoutedEventArgs e) =>
            {
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                double screenWidth = SystemParameters.PrimaryScreenWidth;

                if (percentSize != null)
                {
                    window.Width = (window.Owner.Width / 100) * 80;
                    window.Height = (window.Owner.Height / 100) * 80;

                    window.Top = (screenHeight - window.Height) / 2;
                    window.Left = (screenWidth - window.Width) / 2;
                }
                else
                {
                    window.Top = screenHeight * 0.25;
                    window.Left = (screenWidth - window.Width) / 2;
                }
            };

            window.ShowDialog();
        }

        // public static void ShowInCenter(this Window window)
        // {
        //     window.Loaded += (object sender, RoutedEventArgs e) =>
        //     {
        //         double screenHeight = SystemParameters.PrimaryScreenHeight;
        //         window.Top = screenHeight * 0.25;
        //         double screenWidth = SystemParameters.PrimaryScreenWidth;
        //         window.Left = (screenWidth - window.Width) / 2;
        //     };


        //     // 1. Получаем текущий активный редактор кода
        //     var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
        //     var editorAdapterFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
        //     var textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

        //     textManager.GetActiveView(1, null, out IVsTextView activeView);
        //     IWpfTextView wpfTextView = editorAdapterFactory.GetWpfTextView(activeView);

        //     if (wpfTextView == null) 
        //         return;

        //     var list = wpfTextView.VisualElement.GetParents().ToArray();
        //     var listGrid = list.Select(x => x as Grid).Where(x => x != null).ToList();
        //     listGrid.Remove(listGrid.Last());
        //     listGrid.Remove(listGrid.Last());
        //     // listGrid.Remove(listGrid.Last());
        //     var mainGrid = listGrid.Last();

        //     // listGrid.Last().Background = new SolidColorBrush(Color.FromArgb(128, 255, 128, 128));


        //     // 2. Получаем стандартный слой оформления студии, который висит поверх текста
        //     // Подойдет встроенный слой "Selection" или любой другой стандартный
        //     IAdornmentLayer layer = wpfTextView.GetAdornmentLayer(SearchOverlayLayerName);


        //     // 3. Создаем наш полноэкранный Grid-оверлей
        //     var overlay = new Grid();
        //     overlay.Background = new SolidColorBrush(Color.FromArgb(128, 128, 128, 255));

        //     mainGrid.Children.Add(overlay);
        //     Panel.SetZIndex(overlay, 9999);


        //     overlay.PreviewMouseDown += (s, e) =>
        //     {
        //         if (e.OriginalSource == overlay)
        //         {
        //             overlay.Focus();
        //         }
        //         e.Handled = true;
        //     };

        //     overlay.Focus();
        //     var uiShell = (Microsoft.VisualStudio.Shell.Interop.IVsUIShell)Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell));
        //     uiShell.EnableModeless(0);

        //     return;

        //     // Растягиваем его под текущие размеры видимого окна редактора
        //     overlay.Width = wpfTextView.VisualElement.RenderSize.Width;
        //     overlay.Height = wpfTextView.VisualElement.RenderSize.Height;

        //     // 4. Добавляем его на экран
        //     layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, overlay, null);

        //     // VisualTreeHelper.GetParent(
        //     // window.Show();
        // }


        // public static IEnumerable<DependencyObject> GetParents(this DependencyObject instance)
        // {
        //     var parent = instance;
        //     while (parent != null)
        //     {
        //         yield return parent;
        //         parent = VisualTreeHelper.GetParent(parent);
        //     }
        // }

        // public static T BlockKeys<T>(this T window) where T : Window
        // {
        //     window.Loaded += (object sender, RoutedEventArgs e) => 
        //     {
        //         // var source = HwndSource.FromHwnd(new WindowInteropHelper(sender as T).Handle);
        //         // if (source != null)
        //         // {
        //         //     // Добавляем фильтр, который будет душить все сообщения
        //         //     source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        //         //     {
        //         //         const int WM_KEYDOWN = 0x0100;
        //         //         const int WM_KEYUP = 0x0101;
        //         //         const int WM_SYSKEYDOWN = 0x0104;
        //         //         const int WM_SYSKEYUP = 0x0105;

        //         //         const int VK_BACK = 0x08;
        //         //         const int VK_DELETE = 0x2E;

        //         //         // Если это любое нажатие или отпускание клавиши (включая Alt-комбинации)
        //         //         if (msg == WM_KEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP)
        //         //         {
        //         //             // Направляем сообщение только внутренним элементам WPF нашего окна
        //         //             var message = new System.Windows.Interop.MSG
        //         //             {
        //         //                 hwnd = hwnd,
        //         //                 message = msg,
        //         //                 wParam = wParam,
        //         //                 lParam = lParam,
        //         //                 time = Convert.ToInt32(Environment.TickCount)
        //         //             };

        //         //             // Передаем сообщение в WPF (без всяких звездочек и амперсандов)
        //         //             // System.Windows.Interop.ComponentDispatcher.RaiseThreadMessage(ref message);

        //         //             // System.Windows.Interop.ComponentDispatcher.RaiseThreadMessage(
        //         //             //     ref *(System.Windows.Interop.MSG*)¤tMessage(hwnd, msg, wParam, lParam));

        //         //             // ЖЕСТКАЯ БЛОКИРОВКА: говорим Windows и Visual Studio, что событие обработано.
        //         //             // Дальше этого окна событие гарантированно не уйдет.
        //         //             handled = true;
        //         //         }

        //         //         return IntPtr.Zero;
        //         //     });
        //         // }

        //         Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        //         var uiShell = (Microsoft.VisualStudio.Shell.Interop.IVsUIShell)Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell));

        //         if (uiShell != null)
        //         {
        //             // Переводим оболочку в режим ожидания (это отключит все глобальные хуки редактора)
        //             uiShell.EnableModeless(0);
        //         }
        //     };

        //     window.Deactivated += (s, ev) => 
        //     {
        //         Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        //         var uiShell = (Microsoft.VisualStudio.Shell.Interop.IVsUIShell)Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell));

        //         if (uiShell != null)
        //         {
        //             uiShell.EnableModeless(1);
        //         }
        //     };

        //     return window;
        // }
    }
}
