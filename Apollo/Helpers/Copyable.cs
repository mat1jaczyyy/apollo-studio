using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Interfaces;
using Apollo.Windows;

namespace Apollo.Helpers {
    public class Copyable {
        public List<ISelect> Contents = new List<ISelect>();

        public Type Type => (Contents.Count > 0)? Contents[0].GetType() : null;

        public async void StoreToClipboard()
            => await Application.Current.Clipboard.SetTextAsync(Convert.ToBase64String(Encoder.Encode(this).ToArray()));

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
                await MessageWindow.Create(
                    $"An error occurred while reading the file.\n\n" +
                    "You may not have sufficient privileges to read from the destination folder, or\n" +
                    "the file you're attempting to read is invalid.",
                    null, sender
                );

                return null;
            }
        }
    }
}