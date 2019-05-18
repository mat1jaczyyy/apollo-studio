using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Input;

using Apollo.Core;

namespace Apollo.Helpers {
    public class UndoManager {
        public class UndoEntry {
            public readonly string Description;
            public readonly Action Undo;
            public readonly Action Redo;

            public UndoEntry(string desc, Action undo = null, Action redo = null) {
                Description = desc;
                Undo = undo?? (() => {});
                Redo = redo?? (() => {});
            }
        }

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
                PositionChanged?.Invoke(_position);
            }
        }

        public void Add(string desc, Action undo, Action redo) {
            for (int i = History.Count - 1; i > Position; i--)
                History.RemoveAt(i);
            
            History.Add(new UndoEntry(desc, undo, redo));
            Position = History.Count - 1;
        }

        public void Select(int index) {
            if (index < Position)
                for (int i = Position; i > index; i--)
                    History[i].Undo.Invoke();
            
            else if (index > Position)
                for (int i = Position + 1; i <= index; i++)
                    History[i].Redo.Invoke();
            
            Position = index;
        }

        public void Undo() => Select(Math.Max(0, Position - 1));
        public void Redo() => Select(Math.Min(History.Count - 1, Position + 1));

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
    }
}