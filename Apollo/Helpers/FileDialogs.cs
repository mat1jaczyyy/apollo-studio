using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Apollo.Helpers {
    internal static class FileDialogs {
        public static async Task<string[]> Open(Window owner, FilePickerOpenOptions options) {
            var files = await owner.StorageProvider.OpenFilePickerAsync(options);
            return files.Select(file => file.TryGetLocalPath()).Where(path => path != null).ToArray();
        }

        public static async Task<string> Save(Window owner, FilePickerSaveOptions options) {
            options.ShowOverwritePrompt = true;
            var file = await owner.StorageProvider.SaveFilePickerAsync(options);
            return file?.TryGetLocalPath();
        }
    }
}
