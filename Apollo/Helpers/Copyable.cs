using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StringBuilder = System.Text.StringBuilder;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Selection;
using Apollo.Windows;

namespace Apollo.Helpers {
    public class Copyable {
        public List<ISelect> Contents = new();

        public Type Type => Contents.FirstOrDefault()?.GetType();

        public void Merge(Copyable merging) {
            if (Type != merging.Type && Type != null && merging.Type != null) return;

            foreach (ISelect item in merging.Contents)
                Contents.Add(item);
        }

        public async void StoreToClipboard()
            => await Application.Current.Clipboard.SetTextAsync(ToCompressedBase64(Encoder.Encode(this)));
            
        class Seq {
            public char c;
            public int n;
            
            public Seq(char c, int n) {
                this.c = c;
                this.n = n;
            }
        }

        public static string ToCompressedBase64(byte[] inArray) {
            string input = Convert.ToBase64String(inArray);
            char[] arr = input.ToCharArray();

            char current = '\0';
            List<Seq> data = new();

            foreach (char c in arr) {
                if (c == current) data.Last().n++;
                else data.Add(new Seq(current = c, 1));
            }
            
            StringBuilder a = new StringBuilder();

            foreach (Seq t in data) {
                int log = (int)Math.Log(t.n, 64) + 1;
                
                if (3 + log < t.n) {
                    char[] chars = new char[log];

                    for (int j = 0; j < log; j++) {
                        chars[j] = (char)(t.n % 64 + 48); 
                        t.n = t.n >> 6;
                    }

                    a.Append('{');
                    a.Append(t.c);

                    foreach (char i in chars.Reverse())
                        a.Append(i);

                    a.Append('}');

                } else for (int i = 0; i < t.n; i++) 
                    a.Append(t.c);
            }

            return a.ToString();
        }
        
        public static byte[] FromCompressedBase64(string inString) {
            bool inCompressed = false;
            bool readingChar = true;
            char compressedChar = ' ';
            StringBuilder count = new StringBuilder();
            StringBuilder res = new StringBuilder();
            
            foreach (char c in inString) {
                switch (c) {
                    case '{':
                        inCompressed = true;
                        readingChar = true;
                        break;

                    case '}':
                        int numCount = 0;
                        
                        for (int i = count.Length - 1; i >= 0; i--)
                            numCount |= ((int)count[i] - 48) << (6 * (count.Length - i - 1));
                        
                        for (int i = 0; i < numCount; i++)
                            res.Append(compressedChar);
                        
                        count.Clear();
                        inCompressed = false;
                        readingChar = false;
                        break;

                    default:
                        if (inCompressed) {
                            if (readingChar) {
                                compressedChar = c;
                                readingChar = false;

                            } else count.Append(c);

                        } else res.Append(c);
                        break;
                }
            }
            
            return Convert.FromBase64String(res.ToString());
        }
        
        public async Task StoreToFile(string path, Window sender) {
            try {
                File.WriteAllBytes(path, Encoder.Encode(this));

            } catch (UnauthorizedAccessException) {
                await MessageWindow.CreateWriteError(sender);
            }
        }

        public static async Task<Copyable> DecodeClipboard() {
            string b64 = await Application.Current.Clipboard.GetTextAsync();

            if (b64 == null) return null;

            try {
                using (MemoryStream ms = new MemoryStream(FromCompressedBase64(b64)))
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