using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Wordwatch.Data.Ingestor.Application.Helpers;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Implementation;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor
{
    static class Program
    {
        private static readonly ILog _logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Add handler to handle the exception raised by main threads
            System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalExceptionHandler);

            System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.SystemAware);
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var builder = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<ApplicationSettings>(hostContext.Configuration.GetSection(nameof(ApplicationSettings)));

                services.AddSingleton<IMigrationActionService, MigrationActionService>();

                services.AddSingleton<SystemInitializerService>();
                services.AddSingleton<InsertTableRowsService>();

                //services.AddTransient<SourceDbContext>();
                //services.AddTransient<TargetDbContext>();

                services.AddSingleton<MainForm>();
            })
            .ConfigureLogging(logBuilder =>
            {
                logBuilder.SetMinimumLevel(LogLevel.Trace);
                logBuilder.AddLog4Net("log4net.config");
                log4net.Config.XmlConfigurator.Configure();
            });

            var host = builder.Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var mainForm = services.GetRequiredService<MainForm>();

                System.Windows.Forms.Application.Run(mainForm);
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs args)
        {
            Exception ex = (Exception)args.Exception;
            _logger.Error(ex);

            MessageBox.Show(ex.Message, $"{ex.TargetSite} - thread exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = (Exception)args.ExceptionObject;
            _logger.Error(ex);

            MessageBox.Show(ex.Message, $"{ex.TargetSite} - gloabl exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
