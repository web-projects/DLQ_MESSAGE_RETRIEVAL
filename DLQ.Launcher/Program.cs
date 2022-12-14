using DLQ.Common.Configuration;
using DLQ.Common.Configuration.LauncherConfig;
using DLQ.Common.LoggerManager;
using DLQ.Launcher;
using DLQ.Launcher.Core;
using DLQ.Message.Launcher.Providers;
using Microsoft.Extensions.Configuration;
using Ninject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static DLQ.Message.Launcher.Providers.Constants;

namespace DLQ.Message.Launcher
{
    static class Program
    {
        #region --- WIN_API ---
        const int SWP_NOZORDER = 0x4;
        const int SWP_NOACTIVATE = 0x10;

        const uint SW_RESTORE = 9;

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x, y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT
        {
            public uint Length;
            public uint Flags;
            public uint ShowCmd;
            public POINT MinPosition;
            public POINT MaxPosition;
            public RECT NormalPosition;
            public static WINDOWPLACEMENT Default
            {
                get
                {
                    var instance = new WINDOWPLACEMENT();
                    instance.Length = (uint)Marshal.SizeOf(instance);
                    return instance;
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, int flags);

        [DllImport("user32")]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        #endregion --- WIN_API ---

        static private string loggerFullPathLocation;
        static private int loggerActiveLevels;

        static private AppConfig configuration;

        static private AppManagerLoader appManagerLoader;

        static private IStringTemplateProvider StringTemplateProvider { get; }

        static async Task Main(string[] args)
        {
            try
            {
                SetupEnvironment(args);

                InitializeProviders();

                await LaunchApplications();

                Console.WriteLine("\r\nPress <ESC> to EXIT.");

                while (true)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey();

                    if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            SaveWindowPosition();
        }

        private static void InitializeProviders()
        {
            // instance of application manager loader
            appManagerLoader = new AppManagerLoader();
        }

        private static async Task LaunchApplications()
        {
            IDictionary<string, string> pairs = new Dictionary<string, string>
            {
                [SolutionDirectoryToken] = GlobalArguments.SolutionDirectory,
                [SolutionConfigurationToken] = GlobalArguments.SolutionConfiguration,
                [CurrentDirToken] = Directory.GetCurrentDirectory()
            };

            MarqueeStringTemplateProvider marqueeStringTemplateProvider = new MarqueeStringTemplateProvider();

            Console.WriteLine("========================================================");
            Console.WriteLine("Launching applications ...\r\n");

            configuration.Launcher.Apps.OrderBy(p => p.PriorityLevel);

            foreach (Apps app in configuration.Launcher.Apps)
            {
                if (app.LaunchDelaySec > 0)
                {
                    await Task.Delay(app.LaunchDelaySec * 1000);
                }

                string resolvedPath = appManagerLoader.StringTemplateProvider.PerformReduction(
                    new ReadOnlyDictionary<string, string>(pairs),
                    app.Path,
                    new TemplateSettings()
                    {
                        EndTokenSymbol = "{",
                        StartTokenSymbol = "}",
                        Naive = true
                    });

                string workingDirectory = Path.GetDirectoryName(resolvedPath);
                //string args = $"--{CommonConstants.EnvironmentNameConfigKey} {Controller.ConfigProvider.GetConfiguration().GetValue<string>(CommonConstants.EnvironmentNameConfigKey)} " +
                //    $"--{CommonConstants.LicenseKeyConfigKey} {Controller.ConnectorConfiguration.LicenseKey} " +
                //    $"--{CommonConstants.DisableDiagnosticsConfigKey} {Controller.ConfigProvider.GetConfiguration().GetValue<bool>(CommonConstants.DisableDiagnosticsConfigKey)} " +
                //    $"{appConfiguration.Arguments}";
                string args = $"--logLocation {loggerFullPathLocation} --logLevels {loggerActiveLevels}";

                Process process = appManagerLoader.Launch(workingDirectory, resolvedPath, args);

                if (process == null || process.Id <= 0)
                {
                    Console.WriteLine($"Unable to launch '{app.Name}' as a child process.");
                    return;
                }

                Console.WriteLine($"APPLICATION ACTIVE: {app.Name}");
            }
        }

        #region --- Application Setup ---
        private static void SetupEnvironment(string[] args)
        {
            // Get appsettings.json config - AddEnvironmentVariables()
            // requires packages:
            //                    Microsoft.Extensions.Configuration
            //                    Microsoft.Extensions.Configuration.Abstractions
            //                    Microsoft.Extensions.Configuration.Binder
            //                    Microsoft.Extensions.Configuration.EnvironmentVariables
            //                    Microsoft.Extensions.Configuration.Json
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddEnvironmentVariables()
                .Build()
                .Get<AppConfig>();

            // Show Window
            if (Handle == IntPtr.Zero)
            {
                AllocConsole();
            }
            else
            {
                ShowWindow(Handle, SW_SHOW);
            }

            SetWindowPosition();

            Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            AppManagerArgumentHelper.SetApplicationArgumentsIfNecessary(args);

            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            SetLogging();
        }

        private static void SetLogging()
        {
            try
            {
                string[] logLevels = configuration.LoggingManager.Logging.Levels.Split("|");

                if (logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = Directory.GetCurrentDirectory();
                    loggerFullPathLocation = path + "\\logs\\" + logname;

                    loggerActiveLevels = 0;
                    foreach (string item in logLevels)
                    {
                        foreach (LOGLEVELS level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            loggerActiveLevels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(loggerFullPathLocation, loggerActiveLevels);

                    Logger.info($"{Assembly.GetEntryAssembly().GetName().Name} ({Assembly.GetEntryAssembly().GetName().Version}) - LOGGING INITIALIZED.");
                }
            }
            catch (Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", e.Message);
            }
        }

        private static void SetWindowPosition()
        {
            // x : left side of window
            // y : top position of window
            // cx: width of window
            // cy: height of window

            SetWindowPos(Handle, IntPtr.Zero,
                configuration.Application.WindowsPosition.Left, configuration.Application.WindowsPosition.Top,
                configuration.Application.WindowsPosition.Right - configuration.Application.WindowsPosition.Left,
                configuration.Application.WindowsPosition.Bottom - configuration.Application.WindowsPosition.Top,
                SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private static void SaveWindowPosition()
        {
            // Get this console window's hWnd (window handle).
            IntPtr hWnd = GetConsoleWindow();

            // Get information about this window's current placement.
            WINDOWPLACEMENT wp = WINDOWPLACEMENT.Default;

            GetWindowPlacement(hWnd, ref wp);

            configuration.Application.WindowsPosition.Top = wp.NormalPosition.Top;
            configuration.Application.WindowsPosition.Bottom = wp.NormalPosition.Bottom;
            configuration.Application.WindowsPosition.Left = wp.NormalPosition.Left;
            configuration.Application.WindowsPosition.Right = wp.NormalPosition.Right;

            AppSettingsUpdate();
        }

        private static IntPtr Handle
        {
            get
            {
                //Initialize();
                return GetConsoleWindow();
            }
        }

        private static void AppSettingsUpdate()
        {
            try
            {
                var jsonWriteOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                jsonWriteOptions.Converters.Add(new JsonStringEnumConverter());

                string newJson = JsonSerializer.Serialize(configuration, jsonWriteOptions);
                //Debug.WriteLine($"{newJson}");

                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                File.WriteAllText(appSettingsPath, newJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in saving settings: {ex}");
            }
        }
        #endregion --- Application Setup ---
    }
}
