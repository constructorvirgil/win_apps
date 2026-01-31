using System.Configuration;
using System.Data;
using System.Windows;

namespace VirtualHidSimulator.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            TryEnablePerMonitorDpiAwareness();
            base.OnStartup(e);
        }

        private static void TryEnablePerMonitorDpiAwareness()
        {
            try
            {
                SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            catch
            {
                // best effort
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);
    }

}
