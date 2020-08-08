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
            => await Application.Current.Clipboard.SetTextAsync(ToCompressedBase64(Encoder.Encode(this)));
            
        public static string ToCompressedBase64(byte[] inArray) {
            string input = Convert.ToBase64String(inArray);
            char[] arr = input.ToCharArray();
	  
        	char current = '\0';
        	(char, int)[] data = new (char, int)[]{};
        	  
        	foreach (char c in arr) {
        		if (c == current) {
        			int count = data.Count();
        			data[count - 1].Item2++;
        		} else {
        			data = data.Append((c, 1)).ToArray();
        			current = c;
        		}
        	}
        	  
    	    return data.Aggregate("", (a, t) => {
        		(char c, int count) = t;
        		int log = (int)Math.Floor(Math.Log(count, 64)) + 1;
                
        		if (3 + log < count) {
        			char[] chars = new char[log];
        			for (int j = 0; j < log; j++) {
        				chars[j] = (char)(count % 64 + 48); 
        				count = count >> 6;
        			}
        			chars = chars.Reverse().ToArray();
        			return a + $"{{{c}{string.Join("", chars)}}}";
        		} else return a + string.Concat(Enumerable.Repeat(c, count));
        	});
        }
        
        public static byte[] FromCompressedBase64(string inString) {
            bool inCompressed = false;
            bool readingChar = true;
            char compressedChar = ' ';
            string count = "";
            string res = "";
            
            foreach(char c in inString) {
                switch (c) {
                    case '{':
                        inCompressed = true;
                        readingChar = true;
                        break;
                    case '}':
                        char[] countPlaces = count.Reverse().ToArray();
                        int numCount = 0;
                        
                        for(int i = 0; i < countPlaces.Count(); i++) {
                            numCount += ((int)countPlaces[i] - 48) * (int)Math.Pow(64, i);
                        }
                        
                        res += string.Join("", Enumerable.Repeat(compressedChar, numCount));
                        
                        inCompressed = false;
                        readingChar = false;
                        break;
                    default:
                        if (inCompressed) {
                            if (readingChar) {
                                compressedChar = c;
                                readingChar = false;
                            }
                            else count += c;
                        }
                        else res += c;
                        break;
                }
            }
            
            return Convert.FromBase64String(res);
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