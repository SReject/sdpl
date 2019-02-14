using System;
using System.Diagnostics;
using System.Threading;

namespace sdpl {
    public delegate void ProcessExitedEventHandler(object sender, Launch.ProcessExitedArgs args);

    public class Launch {

        // Event prep - emitted when the launched process exits
        public event ProcessExitedEventHandler ProcessExited;
        public class ProcessExitedArgs : EventArgs {
            public bool Errored { get; set; } = false;
            public Exception Error { get; set; }
        }

        // Process and thread tracking
        public Process process { get; private set; }
        public Thread thread;

        // Running and error tracking
        public bool Running { get; private set; } = false;
        public Exception Error { get; private set; }

        public Launch(Manifest manifest) {
            Logger.Log("[Launcher] Initalizing");

            // Build start info
            ProcessStartInfo LaunchInfo = new ProcessStartInfo();
            LaunchInfo.RedirectStandardOutput = true;
            LaunchInfo.RedirectStandardError = true;
            LaunchInfo.FileName = manifest.Path;
            LaunchInfo.Arguments = manifest.Arguments;
            LaunchInfo.CreateNoWindow = manifest.HideWindow;
            LaunchInfo.UseShellExecute = manifest.UseShell;
            LaunchInfo.WorkingDirectory = Plugin.Directory;

            // Setup process
            process = new Process();
            process.StartInfo = LaunchInfo;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new DataReceivedEventHandler(Process_DataReceivedHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(Process_DataReceivedHandler);

            Logger.Log("[Launcher] Initialized");

            // Prep thread the process will run on
            thread = new Thread(Start);
            thread.IsBackground = false;

            // Start process
            Running = true;
            thread.Start();
        }
        private void Process_DataReceivedHandler(object sender, DataReceivedEventArgs evt) {
            Logger.Log($"[Process] {evt.Data}");
        }

        // Called to start the launched process in a secondary thread
        private void Start() {
            try {

                // Attempt to launch the executable
                Logger.Log($"[Launcher] Launching: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                process.Start();

                // Begin reading from process's stdout/err
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for executable to exit
                Logger.Log("[Launcher] Process Started");
                process.WaitForExit();
                Running = false;

                // Prep exit event args
                ProcessExitedArgs exitArgs = new ProcessExitedArgs();
                if (Error != null) {
                    exitArgs.Errored = true;
                    exitArgs.Error = Error;
                }

                // Emit exit event
                ProcessExited?.Invoke(this, exitArgs);

            } catch (Exception ex) {
                Running = false;
                Error = ex;

                Logger.Log($"[Launcher] Process failed to start: {ex.Message}");

                // Prep exit event args
                ProcessExitedArgs exitArgs = new ProcessExitedArgs();
                exitArgs.Errored = true;
                exitArgs.Error = Error;

                // Emit exit event
                ProcessExited?.Invoke(this, exitArgs);
            }
        }
        public void Close() {
            if (Running == true) {
                Console.Out.WriteLine("[Launcher] Exiting process");

                // Attempt to gracefully close the process
                process.CancelErrorRead();
                process.CancelOutputRead();
                process.CloseMainWindow();
                process.Close();
                Running = false;

                // TODO: Make this so if the process exits the sleep is cancelled
                // if process hasn't exited after 1sec, kill it
                Thread.Sleep(1000);
                if (!process.HasExited) {
                    process.Kill();
                }
            }
        }
    }
}
