using System.IO;

namespace sdpl {
    class Logger {
        private static StreamWriter logfile;
        public static void Init() {

            // create log directory if it does not exit
            string logdir = Path.Combine(Plugin.Directory, "logs");
            if (!Directory.Exists(logdir)) {
                Directory.CreateDirectory(logdir);
            }

            // Create log file; overwrite it if it exists
            string logpath = Path.Combine(logdir, "sdpl.log");
            logfile = File.CreateText(logpath);
            logfile.AutoFlush = true;
        }
        public static void Log(string message) {
            logfile.WriteLine(message);
        }
    }
}
