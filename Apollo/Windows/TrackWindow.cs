using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class TrackWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Track _track;
        Grid Root;
        
        private void UpdateTitle(string path, int index) => this.Get<TextBlock>("Title").Text = (path == "")
            ? $"Track {index + 1}"
            : $"Track {index + 1} - {path}";

        private void UpdateTitle(string path) => UpdateTitle(path, _track.ParentIndex.Value);
        private void UpdateTitle(int index) => UpdateTitle(Program.Project.FilePath, index);
        private void UpdateTitle() => UpdateTitle(Program.Project.FilePath, _track.ParentIndex.Value);

        private void UpdateTopmost(bool value) => Topmost = value;

        private void UpdateContentAlignment(bool value) => Root.ColumnDefinitions[0] = new ColumnDefinition(1, value? GridUnitType.Star : GridUnitType.Auto);

        public Device SelectionStart { get; private set; } = null;
        public Device SelectionEnd { get; private set; } = null;

        public List<Device> Selection {
            get {
                if (SelectionStart != null) {
                    if (SelectionEnd != null) {
                        Device left = (SelectionStart.ParentIndex.Value < SelectionEnd.ParentIndex.Value)? SelectionStart : SelectionEnd;
                        Device right = (SelectionStart.ParentIndex.Value < SelectionEnd.ParentIndex.Value)? SelectionEnd : SelectionStart;

                        return left.Parent.Devices.Skip(left.ParentIndex.Value).Take(right.ParentIndex.Value - left.ParentIndex.Value + 1).ToList();
                    }
                    
                    return new List<Device>() {SelectionStart};
                }

                return new List<Device>();
            }
        }

        public void Select(Device device, bool shift = false) {
            if (SelectionStart != null)
                if (SelectionEnd != null)
                    foreach (Device selected in Selection)
                        selected.Viewer?.Deselect();
                else SelectionStart.Viewer?.Deselect();

            if (shift && SelectionStart != null && SelectionStart.Parent == device.Parent && SelectionStart != device)
                SelectionEnd = device;

            else {
                SelectionStart = device;
                SelectionEnd = null;
            }

            if (SelectionStart != null)
                if (SelectionEnd != null)
                    foreach (Device selected in Selection)
                        selected.Viewer?.Select();
                else SelectionStart.Viewer?.Select();
        }

        public void SelectionAction(string action) {
            if (SelectionStart == null) return;

            if (action == "Cut") Copy(true);
            else if (action == "Copy") Copy();
            else if (action == "Duplicate") Duplicate();
            else if (action == "Paste") Paste();
            else if (action == "Delete") Delete();
            else if (action == "Group") Group();
            else if (action == "Ungroup") Ungroup();
        }

        public TrackWindow(Track track) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _track = track;

            ChainViewer chainViewer = new ChainViewer(_track.Chain);

            Root = chainViewer.Get<Grid>("Layout");
            UpdateContentAlignment(Preferences.CenterTrackContents);
            Preferences.CenterTrackContentsChanged += UpdateContentAlignment;

            this.Get<ScrollViewer>("Contents").Content = chainViewer;
        }

        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            _track.ParentIndexChanged += UpdateTitle;
            UpdateTitle();
        }

        private void Unloaded(object sender, EventArgs e) {
            _track.Window = null;
            
            Program.Project.PathChanged -= UpdateTitle;
            _track.ParentIndexChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            Preferences.CenterTrackContentsChanged -= UpdateContentAlignment;

            Program.WindowClose(this);
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void ResizeWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West);

        private void ResizeEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East);

        public static void Create(Track track, Window owner) {
            if (track.Window == null) {
                track.Window = new TrackWindow(track) {Owner = owner};
                track.Window.Show();
                track.Window.Owner = null;
            } else {
                track.Window.WindowState = WindowState.Normal;
                track.Window.Activate();
            }
        }

        private async void Copy(bool cut = false) {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("clipboard");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("content");
                        writer.WriteValue("device");

                        writer.WritePropertyName("devices");
                        writer.WriteStartArray();

                            foreach (Device selected in Selection) {
                                writer.WriteRawValue(selected.Encode());
                                if (cut) selected.Viewer?.Device_Remove();
                            }

                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            await Application.Current.Clipboard.SetTextAsync(json.ToString());
        }
        
        private async void Paste() {
            string jsonString = await Application.Current.Clipboard.GetTextAsync();

            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "clipboard") return;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            if (data["content"].ToString() != "device") return;

            List<object> devices = JsonConvert.DeserializeObject<List<object>>(data["devices"].ToString());
            Device left = Selection.First();

            foreach (Device device in (from i in devices select Device.Decode(i.ToString())))
                left.Viewer?.Device_Paste(left = device);
        }

        private void Duplicate() {
            Device left = Selection.Last();

            foreach (Device device in Selection)
                left.Viewer?.Device_Paste(left = device.Clone());
        }
        
        private void Delete() {
            foreach (Device selected in Selection)
                selected.Viewer?.Device_Remove();
        }

        private void Group() {
            List<Device> selection = Selection;

            Selection.First().Viewer?.Device_Paste(
                new Group(new List<Chain>() {new Chain((from i in Selection select i.Clone()).ToList())})
            );

            foreach (Device selected in selection)
                selected.Viewer?.Device_Remove();
        }

        private void Ungroup() {
            List<Device> selection = Selection;
            Device left = Selection.First();

            foreach (Device device in ((Group)SelectionStart)[0].Devices)
                left.Viewer?.Device_Paste(left = device.Clone());
            
            foreach (Device selected in selection)
                selected.Viewer?.Device_Remove();
        }
    }
}