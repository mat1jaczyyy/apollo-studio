using System;
using System.Linq;

namespace api.Outbreak {
    public class Lightweight {
        // Path to MIDI file
        private String _path = "";

        // Access path to MIDI file
        public String Path {
            get {
                return _path;
            }
            set {
                // TODO: Check if MIDI file exists and is valid, open it...
                _path = value;
            }
        }

        // Compute name of the MIDI file
        public String FileName {
            get {
                return _path.Split('/').Last();
            }
        }

        // Create device without file loaded
        public Lightweight() {}

        // Create device with file loaded
        public Lightweight(string path) {
            this.Path = path;
        }
    }
}