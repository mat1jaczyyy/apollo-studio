using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Selection;
using Apollo.Windows;

namespace Apollo.Helpers {
    public class Copyable {
        public List<ISelect> Contents = new List<ISelect>();

        public Type Type => Contents.FirstOrDefault()?.GetType();

        public async void StoreToClipboard()
            => await Application.Current.Clipboard.SetTextAsync(Convert.ToBase64String(Encoder.Encode(this).ToArray()));
        
        public async Task StoreToFile(string path, Window sender) {
            try {
                File.WriteAllBytes(path, Encoder.Encode(this).ToArray());

            } catch (UnauthorizedAccessException) {
                await MessageWindow.CreateWriteError(sender);
            }
        }

        public static async Task<Copyable> DecodeClipboard() {
            string b64 = await Application.Current.Clipboard.GetTextAsync();

            if (b64 == null) return null;
            
            try {
                return await Decoder.Decode(new MemoryStream(Convert.FromBase64String(b64)), typeof(Copyable));
            } catch (Exception) {
                return null;
            }
        }

        public static async Task<Copyable> DecodeFile(string path, Window sender) {
            try {
                using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
                    return await Decoder.Decode(file, typeof(Copyable));

            } catch {
                await MessageWindow.CreateReadError(sender);
                return null;
            }
        }
    }
}