using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaDragDrop = Avalonia.Input.DragDrop;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Selection;
using Apollo.Structures;

namespace Apollo.DragDrop {
    public class DragDropManager {
        public static bool Move(List<ISelect> source, ISelectParent target, int position, bool copy = false) {
            if (!(source[0] is Track) && !copy && Track.PathContains((ISelect)target, source)) return false;

            if (!copy && ((source[0] is Frame && source[0].IParent != target && source[0].IParent.Count == source.Count) ||
                ((position == -1)
                    ? target.Count > 0 && source[0] == target.IChildren[0]
                    : source.Contains(target.IChildren[position]) || (source[0].IParent == target && source[0].IParentIndex == position + 1)
                )
            )) return false;

            ISelect point = (position == -1)? null : target.IChildren[position];

            for (int i = 0; i < source.Count; i++) {
                if (!copy) source[i].IParent.Remove(source[i].IParentIndex.Value, false);

                source[i] = copy? source[i].IClone() : source[i];

                if (source[i] is Pattern pattern)
                    pattern.Window?.Close();

                target.IInsert((point?.IParentIndex.Value?? -1) + i + 1, source[i]);
            }

            SelectionManager selection = ISelect.GetSelection(source.Last(), target);
            selection?.Select(source[0]);
            selection?.Select(source.Last(), true);
            
            return true;
        }

        IDroppable Host;
        HashSet<IControl> Subscribed = new HashSet<IControl>();

        public delegate bool DropHandler(IControl source, ISelectParent parent, ISelect child, int after, string format, DragEventArgs e);
        Dictionary<string, DropHandler> DropHandlers = new Dictionary<string, DropHandler>();

        static bool DefaultDrop(IControl source, ISelectParent parent, ISelect child, int after, string format, DragEventArgs e) {
            List<ISelect> moving = (List<ISelect>)e.Data.Get(format);
            ISelectParent source_parent = moving[0].IParent;
            int before = moving[0].IParentIndex.Value - 1;

            bool copy = e.KeyModifiers.HasFlag(App.ControlKey);
            bool result;

            if (result = Move(moving, parent, after, copy)) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == parent && after < before)
                    before_pos += count;

                if (format == "Track") {   // TODO This is a bit bad but undo-redo rewrite will make it nice
                    Program.Project.Undo.Add($"{format} {(copy? "Copied" : "Moved")}", copy
                        ? new Action(() => {
                            for (int i = after + count; i > after; i--)
                                Program.Project.Remove(i);

                        }) : new Action(() => {
                            List<ISelect> umoving = (from i in Enumerable.Range(after_pos + 1, count) select (ISelect)Program.Project[i]).ToList();

                            Move(umoving, Program.Project, before_pos);

                    }), () => {
                        List<ISelect> rmoving = (from i in Enumerable.Range(before + 1, count) select (ISelect)Program.Project[i]).ToList();

                        Move(rmoving, Program.Project, after, copy);
                    });

                } else {
                    List<int> sourcepath = Track.GetPath((ISelect)source_parent);
                    List<int> targetpath = Track.GetPath((ISelect)parent);
                    
                    Program.Project.Undo.Add($"{format} {(copy? "Copied" : "Moved")}", copy
                        ? new Action(() => {
                            ISelectParent targetparent = (ISelectParent)Track.TraversePath<ISelect>(targetpath);

                            for (int i = after + count; i > after; i--)
                                targetparent.Remove(i);

                        }) : new Action(() => {
                            ISelectParent sourceparent = (ISelectParent)Track.TraversePath<ISelect>(sourcepath);
                            ISelectParent targetparent = (ISelectParent)Track.TraversePath<ISelect>(targetpath);

                            List<ISelect> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetparent.IChildren[i]).ToList();

                            Move(umoving, sourceparent, before_pos);

                    }), () => {
                        ISelectParent sourceparent = (ISelectParent)Track.TraversePath<ISelect>(sourcepath);
                        ISelectParent targetparent = (ISelectParent)Track.TraversePath<ISelect>(targetpath);

                        List<ISelect> rmoving = (from i in Enumerable.Range(before + 1, count) select sourceparent.IChildren[i]).ToList();

                        Move(rmoving, targetparent, after, copy);
                    });
                }
            }    

            return result;
        }

        static bool FileDrop(IControl source, ISelectParent parent, ISelect child, int after, string format, DragEventArgs e) {
            string path = e.Data.GetFileNames().FirstOrDefault();

            if (path != null) parent.IViewer?.Import(after, path);

            return true;
        }

        public DragDropManager(IDroppable control) {
            Host = control;

            foreach (KeyValuePair<string, DropHandler> entry in Host.DropHandlers)
                DropHandlers.Add(
                    entry.Key,
                    entry.Value?? ((entry.Key == DataFormats.FileNames)
                        ? new DropHandler(FileDrop)
                        : DefaultDrop
                    )
                );

            Subscribe(Host);
        }

        public void Subscribe(IControl control) {
            Subscribed.Add(control);
            
            control.AddHandler(AvaloniaDragDrop.DragOverEvent, DragOver);
            control.AddHandler(AvaloniaDragDrop.DropEvent, Drop);
        }

        public async void Drag(SelectionManager selection, PointerPressedEventArgs e) {
            if (!(Host is IDraggable drag)) return;

            if (!drag.Selected) drag.Select(e);

            DataObject dragData = new DataObject();
            dragData.Set(drag.DragFormat, selection.Selection);

            App.Dragging = true;
            DragDropEffects result = await AvaloniaDragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
            App.Dragging = false;

            if (result == DragDropEffects.None) {
                if (drag.Selected) drag.Select(e);
                drag.DragFailed(e);
            }
        }

        void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            
            if (!e.Data.GetDataFormats().Any(DropHandlers.Keys.Contains))
                e.DragEffects = DragDropEffects.None; 
        }

        void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            IControl source = (IControl)e.Source;
            while (!Host.DropAreas.Contains(source.Name)) {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            int after = (Host.Item?.IParentIndex - Convert.ToInt32(Host.DropLeft(source, e)))?? 
                ((source.Name == "DropZoneAfter")? Host.ItemParent.Count - 1 : -1);

            bool result = false;

            foreach (string format in DropHandlers.Keys.Where(i => e.Data.Contains(i)))
                if (result = DropHandlers[format].Invoke(source, Host.ItemParent, Host.Item, after, format, e))
                    break;
            
            if (!result) e.DragEffects = DragDropEffects.None;
        }

        public void Dispose() {
            foreach (IControl control in Subscribed) {
                control.RemoveHandler(AvaloniaDragDrop.DragOverEvent, DragOver);
                control.RemoveHandler(AvaloniaDragDrop.DropEvent, Drop);
            }

            Subscribed = null;
            Host = null;
        }
    }
}