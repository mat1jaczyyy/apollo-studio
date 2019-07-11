using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Input;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Helpers {
    public class UndoManager {
        public class UndoEntry {
            public string Description;
            public Action Undo;
            public Action Redo;
            Action DisposeAction;

            public UndoEntry(string desc, Action undo = null, Action redo = null, Action dispose = null) {
                Description = desc;
                Undo = undo?? (() => {});
                Redo = redo?? (() => {});
                DisposeAction = dispose?? (() => {});
            }

            public void Dispose() => DisposeAction?.Invoke();
        }

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

        public void Add(string desc, Action undo, Action redo, Action dispose = null) {
            for (int i = History.Count - 1; i > Position; i--)
                Remove(i);
            
            History.Add(new UndoEntry(desc, undo, redo, dispose));
            Window?.Contents_Insert(History.Count - 1, History.Last());

            Position = History.Count - 1;

            if (Preferences.UndoLimit) Limit();
        }

        public void Select(int index) {
            lock (locker) {
                if (index < Position)
                    for (int i = Position; i > index; i--)
                        History[i].Undo.Invoke();
                
                else if (index > Position)
                    for (int i = Position + 1; i <= index; i++)
                        History[i].Redo.Invoke();
                
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
                if (_saved != null) {
                    _saved += -remove;
                    if (_saved < 0) _saved = null;
                }

                if (Position < History.Count - 150) Select(History.Count - 150);
                _position += -remove;

                SavedPositionChanged?.Invoke(SavedPosition);

                for (int i = History.Count - 151; i >= 0; i--)
                    Remove(i);

                Position = Position;
                SavedPositionChanged?.Invoke(SavedPosition);
            }
        }

        public bool HandleKey(KeyEventArgs e) {
            if (e.Modifiers == Program.ControlKey) {
                if (e.Key == Key.Z) Undo();
                else if (e.Key == Key.Y) Redo();
                else return false;

            } else if (e.Modifiers == (Program.ControlKey | InputModifiers.Shift)) {
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