using DLQ.Launcher.Kernel;
using DLQ.Message.Launcher.Providers;
using Ninject;
using System;
using System.Diagnostics;

namespace DLQ.Launcher
{
    internal class AppManagerLoader
    {
        [Inject]
        public IStringTemplateProvider StringTemplateProvider { get; set; }

        public AppManagerLoader()
        {
            InitializeInjector();
        }

        private void InitializeInjector()
        {
            using IKernel kernel = new AppManagerKernelResolver().ResolveKernel();
            kernel.Inject(this);
        }

        public Process Launch(string workingDirectory, string fullFileName, string arguments)
        {
            try
            {
                Process process = new Process();

                process.StartInfo.Verb = "runas";
                process.StartInfo.FileName = fullFileName;
                //process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.Arguments = arguments;
                //process.StartInfo.RedirectStandardError = true;
                //process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = false;

                if (!process.Start())
                {
                    Console.WriteLine($"Unable to start process '{fullFileName}'.");
                }

                //process.BeginOutputReadLine();
                //process.BeginErrorReadLine();

                return process;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while attempting to launch process '{fullFileName}' - {ex.Message}");
                return null;
            }
        }

    }
}
