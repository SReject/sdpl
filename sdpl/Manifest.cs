using Newtonsoft.Json;
using System;
using System.IO;

namespace sdpl {
    public class SDManifest {
        public Manifest SDPL { get; set; }
    }
    public class Manifest {
        public bool HideWindow { get; set; } = true;
        public string Path { get; set; } = "";
        public string Arguments { get; set; } = "";

        public static Manifest Load(string[] args) {
            Logger.Log("[Manifest] Loading");

            // Deduce path to manifest.json
            string manifestPath = System.IO.Path.Combine(Plugin.Directory, "manifest.json");

            // variable declares
            string manifestText;
            SDManifest sdmanifest;

            // Read the contents of manifest.json
            try {

                Logger.Log($"[Manifest] Attempting to read: {manifestPath}");
                manifestText = File.ReadAllText(manifestPath);

            } catch (Exception ex) {
                throw new Exception($"[Manifest] Failed to load: {ex.Message}");
            }

            // Deserialize the contents of manifest.json
            try {
                Logger.Log("[Manifest] Parsing manifest");
                sdmanifest = JsonConvert.DeserializeObject<SDManifest>(manifestText);

            } catch (Exception ex) {
                throw new Exception($"[Manifest] Could not process manifest: {ex.Message}");
            }

            // Extract manifest.json's SDPL entry
            if (sdmanifest.SDPL == null) {
                throw new Exception("[Manifest] SDPL property missing or invalid");
            }
            Manifest manifest = sdmanifest.SDPL;

            // concat arguments listed in manifest with args passed into main
            if (args.Length > 0) {
                if (manifest.Arguments != "") {
                    manifest.Arguments += " ";
                }
                manifest.Arguments += String.Join(" ", args).Replace("\"", "\"\"\"");
            }

            // return the manifest
            Logger.Log("[Manifest] Ready");
            return manifest;
        }
    }
}
