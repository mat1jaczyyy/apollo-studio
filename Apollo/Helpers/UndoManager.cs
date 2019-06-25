using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Input;

using Apollo.Windows;

namespace Apollo.Helpers {
    public class UndoManager {
        public class UndoEntry {
            public string Description;
            public Action Undo;
            public Action Redo;

            public UndoEntry(string desc, Action undo = null, Action redo = null) {
                Description = desc;
                Undo = undo?? (() => {});
                Redo = redo?? (() => {});
            }
        }

        public UndoWindow Window;

        private object locker = new object();

        public List<UndoEntry> History = new List<UndoEntry>() {
            new UndoEntry("Initial State")
        };

        public delegate void PositionChangedEventHandler(int position);
        public event PositionChangedEventHandler PositionChanged;

        private int _position = 0;
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

        private int? _saved = null;
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

        public void Add(string desc, Action undo, Action redo) {
            for (int i = History.Count - 1; i > Position; i--) {
                History.RemoveAt(i);
                Window?.Contents_Remove(i);
            }
            
            History.Add(new UndoEntry(desc, undo, redo));
            Window?.Contents_Insert(History.Count - 1, History.Last());

            Position = History.Count - 1;
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

        public void Clear() {
            lock (locker) {
                _saved = (SavedPosition == Position)? (int?)0 : null;

                Position = 0;
                SavedPositionChanged?.Invoke(SavedPosition);

                if (Window != null)
                    for (int i = History.Count - 1; i >= 0; i--)
                        Window.Contents_Remove(i);

                History = new List<UndoEntry>() {
                    new UndoEntry("Undo History Cleared")
                };

                Window?.Contents_Insert(0, History[0]);

                Position = 0;
                SavedPositionChanged?.Invoke(SavedPosition);
            }
        }

        public bool HandleKey(KeyEventArgs e) {
            if (e.Modifiers == InputModifiers.Control) {
                if (e.Key == Key.Z) Undo();
                else if (e.Key == Key.Y) Redo();
                else return false;

            } else if (e.Modifiers == (InputModifiers.Control | InputModifiers.Shift)) {
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

            History = null;
        }
    }
}