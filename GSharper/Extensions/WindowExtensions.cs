using System.Windows;

namespace GSharper.Extensions
{
    public static class WindowExtensions
    {
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
    }
}
