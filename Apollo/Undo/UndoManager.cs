using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Input;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Undo {
    public class UndoManager {
        public UndoWindow Window;

        object locker = new object();

        public List<UndoEntry> History = new List<UndoEntry>() {
            new UndoEntry("Initial State")
        };

        public delegate void PositionChangedEventHandler(int position);
        public event PositionChangedEventHandler PositionChanged;

        int _position = 0;
        public int Position { 
            get => _position;
            private set {
                _position = value;

                PositionChanged?.Invoke(Position);
                SavedChanged?.Invoke(Saved);

                Window?.HighlightPosition(Position);
            }
        }

        public delegate void SavedPositionChangedEventHandler(int? position);
        public event SavedPositionChangedEventHandler SavedPositionChanged;

        int? _saved = null;
        public int? SavedPosition {
            get => _saved;
        }
        
        public delegate void SavedChangedEventHandler(bool saved);
        public event SavedChangedEventHandler SavedChanged;

        public bool Saved {
            get => _saved == Position;
        }

        public void SavePosition() {
            _saved = Position;

            SavedChanged?.Invoke(Saved);
            SavedPositionChanged?.Invoke(SavedPosition.Value);
        }

        public void Add(UndoEntry entry) {
            for (int i = History.Count - 1; i > Position; i--)
                Remove(i);
            
            History.Add(entry);
            Window?.Contents_Insert(History.Count - 1, History.Last());

            Position = History.Count - 1;

            if (Preferences.UndoLimit) Limit();

            Program.Project.WriteCrashBackup();
        }

        public void Add(string desc, Action undo = null, Action redo = null, Action dispose = null) => Add(new LegacyUndoEntry(desc, undo, redo, dispose));

        class LegacyUndoEntry: UndoEntry {  // TODO delete this
            Action undo, redo, dispose;

            public override void Undo() => undo?.Invoke();
            public override void Redo() => redo?.Invoke();
            public override void Dispose() => dispose?.Invoke();

            public LegacyUndoEntry(string desc, Action undo, Action redo, Action dispose)
            : base($"[Legacy] {desc}") {
                this.undo = undo;
                this.redo = redo;
                this.dispose = dispose;
            }
        }

        public void AddAndExecute(UndoEntry entry) {
            Add(entry);
            entry.Redo();
        }

        public void Select(int index) {
            lock (locker) {
                if (index < Position)
                    for (int i = Position; i > index; i--)
                        History[i].Undo();
                
                else if (index > Position)
                    for (int i = Position + 1; i <= index; i++)
                        History[i].Redo();
                
                Position = index;
            }
        }

        public void Undo() => Select(Math.Max(0, Position - 1));
        public void Redo() => Select(Math.Min(History.Count - 1, Position + 1));

        void Remove(int index) {
            History[index].Dispose();
            History.RemoveAt(index);
            Window?.Contents_Remove(index);
        }

        public void Clear(string description = "Undo History Cleared") {
            _saved = (SavedPosition == Position)? (int?)0 : null;
            SavedPositionChanged?.Invoke(SavedPosition);

            if (Window != null)
                for (int i = History.Count - 1; i >= 0; i--)
                    Window.Contents_Remove(i);

            foreach (UndoEntry entry in History) entry.Dispose();
            History = new List<UndoEntry>() {
                new UndoEntry(description)
            };

            Window?.Contents_Insert(0, History[0]);

            Position = 0;
            SavedPositionChanged?.Invoke(SavedPosition);
        }

        public void Limit() {
            int remove;
            if ((remove = History.Count - 150) > 0) {
                if (Position < History.Count - 150) return;

                if (_saved != null) {
                    _saved += -remove;
                    if (_saved < 0) _saved = null;
                }
                SavedPositionChanged?.Invoke(SavedPosition);

                _position += -remove;

                for (int i = History.Count - 151; i >= 0; i--)
                    Remove(i);

                Position = Position;
                SavedPositionChanged?.Invoke(SavedPosition);
            }
        }

        public bool HandleKey(KeyEventArgs e) {
            if (e.KeyModifiers == App.ControlKey) {
                if (e.Key == Key.Z) Undo();
                else if (e.Key == Key.Y) Redo();
                else return false;

            } else if (e.KeyModifiers == (App.ControlKey | KeyModifiers.Shift)) {
                if (e.Key == Key.Z) Redo();
                else return false;
            
            } else return false;

            return true;
        }

        public void Dispose() {
            Window?.Close();
            Window = null;

            PositionChanged = null;
            SavedChanged = null;
            SavedPositionChanged = null;

            foreach (UndoEntry entry in History) entry.Dispose();
            History = null;
        }
    }
}