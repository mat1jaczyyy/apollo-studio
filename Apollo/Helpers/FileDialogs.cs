using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Apollo.Helpers {
    internal static class FileDialogs {
        static int openCount;
        internal static bool IsOpen => openCount != 0;
        public static async Task<string[]> Open(Window owner, FilePickerOpenOptions options) {
            openCount++;
            try {
                var files = await owner.StorageProvider.OpenFilePickerAsync(options);
                return files.Select(file => file.TryGetLocalPath()).Where(path => path != null).ToArray();
            } finally { openCount--; }
        }

        public static async Task<string> Save(Window owner, FilePickerSaveOptions options) {
            openCount++;
            try {
                options.ShowOverwritePrompt = true;
                var file = await owner.StorageProvider.SaveFilePickerAsync(options);
                return file?.TryGetLocalPath();
            } finally { openCount--; }
        }
    }
}
