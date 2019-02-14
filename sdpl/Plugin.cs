using System;

namespace sdpl {
    class Plugin {
        public static string Directory = System.IO.Directory.GetCurrentDirectory();
        public static Launch launcher;

        private static bool LauncherRunning = false;

        static void Main(string[] args) {

            // Initialize the logger
            Logger.Init();
            Logger.Log($"[Main] Starting in: {Directory}");

            // listen for exit events
            AppDomain.CurrentDomain.ProcessExit += AppExitHandler;
            Console.CancelKeyPress += AppExitHandler;

            // Attemt to load manifest.json
            Manifest manifest;
            try {
                manifest = Manifest.Load(args);

            } catch (Exception ex) {
                Logger.Log(ex.Message);
                return;
            }

            // attempt to start the launcher
            try {
                launcher = new Launch(manifest);
                LauncherRunning = true;
                launcher.ProcessExited += Launch_ProcessExited;

            } catch (Exception ex) {
                Logger.Log($"[Launcher] {ex.Message}");
            }
        }

        // Plugin received a close 'message'
        private static void AppExitHandler(object sender, EventArgs evt) {
            Logger.Log("[Main] Exiting");

            // launched process still running; attempt to exit
            if (LauncherRunning) {
                Logger.Log("[Process] Exiting");
                LauncherRunning = false;
                launcher.Close();
            }
        }

        // launched process exited
        private static void Launch_ProcessExited(object sender, Launch.ProcessExitedArgs e) {
            LauncherRunning = false;
            Logger.Log("[Process] Exited");
        }
    }
}