using System;
using System.Windows;

namespace ScreenProfileSwitcher
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            var window = new MainWindow();
            app.Run(window);
        }
    }
}
