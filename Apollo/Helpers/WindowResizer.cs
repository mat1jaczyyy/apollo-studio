using System;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Apollo.Helpers {
    public static class WindowResizer {
        public static void Begin(Window window, WindowEdge edge, PointerPressedEventArgs e) {
            if (window.WindowState != WindowState.Normal) return;
            if (!e.GetCurrentPoint(window).Properties.IsLeftButtonPressed) return;

            IPointer pointer = e.Pointer;
            InputElement grip = (e.Source as InputElement) ?? window;
            pointer.Capture(grip);

            double scale = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 1 : window.RenderScaling;
            PixelPoint startPos = window.Position;
            double startW = window.Width;
            double startH = window.Height;
            PixelPoint startCursor = CursorScreen(window, e, scale);

            bool west = edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest;
            bool east = edge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast;
            bool north = edge is WindowEdge.North or WindowEdge.NorthWest or WindowEdge.NorthEast;
            bool south = edge is WindowEdge.South or WindowEdge.SouthWest or WindowEdge.SouthEast;

            void Moved(object sender, PointerEventArgs ev) {
                PixelPoint cursor = CursorScreen(window, ev, scale);
                double ddx = (cursor.X - startCursor.X) / scale;
                double ddy = (cursor.Y - startCursor.Y) / scale;

                double w = startW;
                double h = startH;

                if (east) w = startW + ddx;
                if (west) w = startW - ddx;
                if (south) h = startH + ddy;
                if (north) h = startH - ddy;

                w = Clamp(w, window.MinWidth, window.MaxWidth);
                h = Clamp(h, window.MinHeight, window.MaxHeight);

                window.Width = w;
                window.Height = h;

                if (west || north)
                    window.Position = new PixelPoint(
                        west ? startPos.X + (int)Math.Round((startW - w) * scale) : startPos.X,
                        north ? startPos.Y + (int)Math.Round((startH - h) * scale) : startPos.Y
                    );
            }

            void Ended(object sender, PointerEventArgs ev) {
                pointer.Capture(null);
                Detach();
            }

            void Lost(object sender, PointerCaptureLostEventArgs ev) => Detach();

            void Detach() {
                grip.PointerMoved -= Moved;
                grip.PointerReleased -= Ended;
                grip.PointerCaptureLost -= Lost;
            }

            grip.PointerMoved += Moved;
            grip.PointerReleased += Ended;
            grip.PointerCaptureLost += Lost;
        }

        static PixelPoint CursorScreen(Window window, PointerEventArgs e, double scale) {
            Point p = e.GetPosition(window);
            return new PixelPoint(
                (int)Math.Round(window.Position.X + p.X * scale),
                (int)Math.Round(window.Position.Y + p.Y * scale)
            );
        }

        static double Clamp(double value, double min, double max) {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }
    }
}
