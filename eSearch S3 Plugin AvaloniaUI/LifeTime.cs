using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch_S3_Plugin_AvaloniaUI
{
    public class LifeTime
    {
        private CancellationTokenSource _cancellationTokenSource;

        public static LifeTime NewLifeTime()
        {
            if (Application.Current == null)
            {
                Debug.WriteLine("Plugin Application.Current is null. Create it now.");
                var builder = AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .SetupWithoutStarting();
            }
            var lifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (lifetime != null)
            {
                lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
            return new LifeTime()
            {
                _cancellationTokenSource = new CancellationTokenSource()
            };
        }

        public void EndLifeTime()
        {
            //_cancellationTokenSource.Cancel();
        }


        //public AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();


        //// Application entry point. Avalonia is completely initialized.
        //public void AppMain(Application app, string[] args)
        //{
        //    // Start the main loop
        //    app.Run(_cancellationTokenSource.Token);
        //}
    }
}
