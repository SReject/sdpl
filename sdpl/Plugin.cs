using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace sdpl {


    class Plugin {

        // Reads from windows' message queue for the purpose of receiving a close event
        #region Unmanged
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax, PeekMessageOption wRemoveMsg);
        private enum PeekMessageOption {
            PM_NOREMOVE = 0,
            PM_REMOVE
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Point {
            long x;
            long y;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MSG {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point pt;
            public uint lPrivate;
        }
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_QUIT = 0x0012;

        // Reads ctrl+break, ctrl+c, etc keys for purposes of receiving a close event
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        public enum CtrlTypes {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        #endregion Unamanged

        // Manifest structuring
        #region manifest
        public class Manifest {
            public bool HideWindow { get; set; } = true;
            public string Path { get; set; } = "";
            public string Arguments { get; set; } = "";
        }
        public class SDManifest {
            public Manifest SDPL { get; set; }
        }
        #endregion Manifest

        // Logs to plugin/logs/sdpl.log
        #region Logger
        private static StreamWriter LogFile;
        public static void Log(string message) {
            if (LogFile == null) {

                // create log directory if it does not exit
                string logdir = Path.Combine(Environment.CurrentDirectory, "logs");
                if (!Directory.Exists(logdir)) {
                    Directory.CreateDirectory(logdir);
                }

                // Create log file; overwrite it if it exists
                string logpath = Path.Combine(logdir, "sdpl.log");
                LogFile = File.CreateText(logpath);
                LogFile.AutoFlush = true;
            }
            LogFile.WriteLine(message);
        }
        #endregion Logger

        // Launched process state tracking
        private static Process LaunchedProcess;
        private static bool LaunchedProcessRunning = false;
        private static bool LaunchedProcessExited = false;
        
        // Main entry point
        static void Main(string[] args) {

            // retrieve plugin directory
            string PluginDirectory = Environment.CurrentDirectory;

            // Initialize the logger
            Log($"[Main] Starting in: {PluginDirectory}");

            // Listen for various exit events
            SetConsoleCtrlHandler(new HandlerRoutine(HandleConsoleCtrl), true);
            Console.CancelKeyPress += AppExitHandler;

            #region Load Manifest
            // Attempt to read manifest
            Log("[Manifest] Loading");

            // Deduce path to manifest.json
            string manifestPath = Path.Combine(PluginDirectory, "manifest.json");

            // variable declares
            string manifestText;
            SDManifest sdmanifest;

            // Read the contents of manifest.json
            try {
                Log($"[Manifest] Attempting to read: {manifestPath}");
                manifestText = File.ReadAllText(manifestPath);

            } catch (Exception ex) {
                Log($"[Manifest] Failed to load: {ex.Message}");
                return;
            }

            // Deserialize the contents of manifest.json
            try {
                Log("[Manifest] Parsing manifest");
                sdmanifest = JsonConvert.DeserializeObject<SDManifest>(manifestText);

            } catch (Exception ex) {
                Log($"[Manifest] Could not process manifest: {ex.Message}");
                return;
            }

            // Extract manifest.json's SDPL entry
            if (sdmanifest.SDPL == null) {
                Log("[Manifest] SDPL property missing or invalid");
                return;
            }

            // concat arguments listed in manifest with args passed into main
            if (args.Length > 0) {
                if (sdmanifest.SDPL.Arguments != "") {
                    sdmanifest.SDPL.Arguments += " ";
                }
                sdmanifest.SDPL.Arguments += string.Join(" ", args).Replace("\"", "\"\"\"");
            }

            Manifest manifest = sdmanifest.SDPL;
            Log("[Manifest] Ready");
            #endregion Load Manifest

            #region Launch executable
            Log($"[Main] Initailizing process");

            // Prepare process start info
            ProcessStartInfo LaunchInfo = new ProcessStartInfo {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = manifest.Path,
                Arguments = manifest.Arguments,
                CreateNoWindow = manifest.HideWindow,
                UseShellExecute = false,
                WorkingDirectory = PluginDirectory
            };

            // Setup process
            LaunchedProcess = new Process {
                StartInfo = LaunchInfo,
                EnableRaisingEvents = true
            };
            LaunchedProcess.OutputDataReceived += new DataReceivedEventHandler(Process_DataReceivedHandler);
            LaunchedProcess.ErrorDataReceived += new DataReceivedEventHandler(Process_DataReceivedHandler);
            LaunchedProcess.Exited += new EventHandler(Process_Exited);

            try {

                // Launch executable
                LaunchedProcess.Start();
                
                // Listen for data wrote to executable's stdout and stderr
                LaunchedProcess.BeginOutputReadLine();
                LaunchedProcess.BeginErrorReadLine();

                // update launched process state
                LaunchedProcessRunning = true;

                Log($"[Main] Process Started");

            // Executable failed to launch
            } catch (Exception ex) {
                Log($"[Main] Failed to launch executable {ex.Message}");
                return;
            }
            #endregion Launch executable

            #region main loop
            MSG message = new MSG();
            while (LaunchedProcessRunning && !LaunchedProcessExited) {

                bool ExitReceived = false;

                // Read messages from Windows message queue if there are any
                while (PeekMessage(out message, IntPtr.Zero, 0, 0, PeekMessageOption.PM_REMOVE)) {

                    // Quit message received
                    if (message.message == WM_QUIT || message.message == WM_CLOSE) {
                        Log("[Main] Close message received");
                        ExitReceived = true;
                        break;
                    }
                }

                if (ExitReceived) {
                    break;
                }

                // let other threads run without maxing cpu
                Thread.Sleep(5);
            }
            #endregion Main Loop

            LaunchedProcessExit();
        }

        // Handle data received from the launched process
        private static void Process_DataReceivedHandler(object sender, DataReceivedEventArgs evt) {
            Log($"[Process] {evt.Data}");
        }

        // Handle exit event from launched process
        private static void Process_Exited(object sender, EventArgs evt) {
            LaunchedProcessExited = true;
            LaunchedProcessRunning = false;
            Log("[Process] Exited");
        }

        // Attempts to exit the launched process
        private static void LaunchedProcessExit() {
            if (LaunchedProcessRunning && !LaunchedProcessExited) {
                Log("[Main] Exiting launched process");

                LaunchedProcessRunning = false;

                // Attempt to close the main window and wait 750ms
                LaunchedProcess.CloseMainWindow();
                Thread.Sleep(750);

                // Process failed to close
                if (LaunchedProcessExited != true) {
                    Log("[Main] Process failed to exit; using kill");
                    LaunchedProcess.Close();

                } else {
                    Log("[Main] Process Exited");
                }
            }
        }

        // Plugin is meant to close
        private static void AppExitHandler(object sender, EventArgs evt) {
            Log("[Main] Exiting: Console Cancel Event Received");
            LaunchedProcessExit();
        }
        private static bool HandleConsoleCtrl(CtrlTypes sig) {
            Log("[Main] Exiting: Console Ctrl Event Received");
            LaunchedProcessExit();
            return true;
        }
    }
}
