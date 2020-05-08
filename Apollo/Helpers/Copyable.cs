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

        public void Merge(Copyable merging) {
            if (Type != merging.Type && Type != null && merging.Type != null) return;

            foreach (ISelect item in merging.Contents)
                Contents.Add(item);
        }

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
            
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(b64)))
                try {
                    return await Decoder.Decode<Copyable>(ms);
                } catch (Exception) {
                    return null;
                }
        }

        public static async Task<Copyable> DecodeFile(string[] paths, Window sender, Type ensure) {
            Copyable ret = new Copyable();

            try {
                foreach (string path in paths) {
                    Copyable copyable;

                    using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
                        copyable = await Decoder.Decode<Copyable>(file);

                    if (!ensure.IsAssignableFrom(copyable.Type))
                        throw new InvalidDataException();
                    
                    ret.Merge(copyable);
                }
                
                return ret;

            } catch {
                await MessageWindow.CreateReadError(sender);
                return null;
            }
        }
    }
}