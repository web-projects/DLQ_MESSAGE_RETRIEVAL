using DLQ.Common.Configuration;
using DLQ.Common.Configuration.ChannelConfig;
using DLQ.MessageProvider.Providers;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeadletterQueue
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

        static private AppConfig configuration;

        static async Task Main(string[] args)
        {
            try
            {
                SetupEnvironment();

                ServiceBus serviceBus = configuration.Channels.Servers.First().ServiceBus;

                // FilterRule name
                string filterRuleName = await DLQMessageProcessor.CreateFilterRule(serviceBus);

                // Write messages to DLQ
                await DLQMessageProcessor.WriteDLQMessages(serviceBus).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            Console.WriteLine("\r\nAll messages sent --- expect them in DLQ within 60 seconds");

            Console.WriteLine("Press <ENTER> to end.");
            Console.ReadLine();

            SaveWindowPosition();
        }

        private static void SetupEnvironment()
        {
            // Get appsettings.json config - AddEnvironmentVariables()
            // requires packages:
            //                    Microsoft.Extensions.Configuration
            //                    Microsoft.Extensions.Configuration.Abstractions
            //                    Microsoft.Extensions.Configuration.Binder
            //                    Microsoft.Extensions.Configuration.EnvironmentVariables
            //                    Microsoft.Extensions.Configuration.Json
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build()
                .Get<AppConfig>();

            // Always refresh the FilterName for each generating instance
            configuration.Channels.Servers.First().ServiceBus.LastFilterNameUsed = Guid.NewGuid().ToString();
            AppSettingsUpdate();

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
        }

        public static void SetWindowPosition()
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

        public static IntPtr Handle
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
                Debug.WriteLine($"{newJson}");

                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                File.WriteAllText(appSettingsPath, newJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in saving settings: {ex}");
            }
        }
    }
}
