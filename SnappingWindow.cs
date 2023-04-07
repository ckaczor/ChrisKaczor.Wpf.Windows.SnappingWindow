using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using JetBrains.Annotations;
using WpfScreenHelper;
using Rectangle = System.Drawing.Rectangle;

namespace ChrisKaczor.Wpf.Windows
{
    [PublicAPI]
    public class SnappingWindow : Window
    {
        #region Member variables

        private HwndSource? _hWndSource;
        private bool _inSizeMove;
        private PInvoke.WindowPosition _lastWindowPosition;
        private List<WindowInformation>? _otherWindows;

        #endregion

        private class WindowBorderRectangles
        {
            public WindowBorderRectangles(Rectangle windowPosition, int width, bool inside)
            {
                var offset = inside ? width : 0;
                var actualWidth = inside ? width * 2 : width;

                Top = new Rectangle(windowPosition.Left, windowPosition.Top - offset, windowPosition.Width, actualWidth);
                Bottom = new Rectangle(windowPosition.Left, windowPosition.Bottom - offset, windowPosition.Width, actualWidth);
                Left = new Rectangle(windowPosition.Left - offset, windowPosition.Top, actualWidth, windowPosition.Height);
                Right = new Rectangle(windowPosition.Right - offset, windowPosition.Top, actualWidth, windowPosition.Height);
            }

            public WindowBorderRectangles(PInvoke.WindowPosition windowPosition, int width, bool inside)
            {
                var offset = inside ? width : 0;
                var actualWidth = inside ? width * 2 : width;

                Top = new Rectangle(windowPosition.Left, windowPosition.Top - offset, windowPosition.Width, actualWidth);
                Bottom = new Rectangle(windowPosition.Left, windowPosition.Bottom - offset, windowPosition.Width, actualWidth);
                Left = new Rectangle(windowPosition.Left - offset, windowPosition.Top, actualWidth, windowPosition.Height);
                Right = new Rectangle(windowPosition.Right - offset, windowPosition.Top, actualWidth, windowPosition.Height);
            }

            public Rectangle Top { get; }
            public Rectangle Bottom { get; }
            public Rectangle Left { get; }
            public Rectangle Right { get; }
        }

        private class ResizeSide
        {
            public ResizeSide(PInvoke.WindowPosition position1, PInvoke.WindowPosition position2)
            {
                if (position1.IsSameSize(position2))
                    return;

                IsTop = position1.Top != position2.Top;
                IsBottom = position1.Bottom != position2.Bottom;
                IsLeft = position1.Left != position2.Left;
                IsRight = position1.Right != position2.Right;
            }

            public bool IsTop { get; }
            public bool IsBottom { get; }
            public bool IsLeft { get; }
            public bool IsRight { get; }
        }

        #region Enumerations

        private enum SnapMode
        {
            Move,
            Resize
        }

        #endregion

        #region Properties

        protected virtual int SnapDistance => 20;

        protected virtual List<WindowInformation>? OtherWindows => null;

        #endregion

        #region Window overrides

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Store the window handle
            _hWndSource = PresentationSource.FromVisual(this) as HwndSource;

            // Add a hook
            _hWndSource?.AddHook(WndProc);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Unhook the window procedure
            _hWndSource?.RemoveHook(WndProc);
            _hWndSource?.Dispose();
        }

        #endregion

        #region Window procedure

        protected virtual nint WndProc(nint hWnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            switch (msg)
            {
                case (int) PInvoke.WindowMessage.WindowPositionChanging:
                    return OnWindowPositionChanging(lParam, ref handled);

                case (int) PInvoke.WindowMessage.EnterSizeMove:

                    // Initialize the last window position
                    _lastWindowPosition = new PInvoke.WindowPosition
                    {
                        Left = (int) Left,
                        Width = (int) Width,
                        Height = (int) Height,
                        Top = (int) Top
                    };

                    // Store the current other windows
                    _otherWindows = OtherWindows;

                    _inSizeMove = true;

                    break;

                case (int) PInvoke.WindowMessage.ExitSizeMove:
                    _inSizeMove = false;

                    break;
            }

            return nint.Zero;
        }

        #endregion

        #region Snapping

        private nint OnWindowPositionChanging(nint lParam, ref bool handled)
        {
            if (!_inSizeMove)
                return nint.Zero;

            var snapDistance = SnapDistance;

            // Initialize whether we've updated the position
            var updated = false;

            // Convert the lParam into the current structure
            var windowPosition = (PInvoke.WindowPosition) Marshal.PtrToStructure(lParam, typeof(PInvoke.WindowPosition))!;

            // If the window flags indicate no movement then do nothing
            if ((windowPosition.Flags & PInvoke.WindowPositionFlags.NoMove) != 0)
                return nint.Zero;

            // If nothing changed then do nothing
            if (_lastWindowPosition.IsSameLocationAndSize(windowPosition))
                return nint.Zero;

            // Figure out if the window is being moved or resized
            var snapMode = _lastWindowPosition.IsSameSize(windowPosition) ? SnapMode.Move : SnapMode.Resize;

            // Figure out what side is resizing
            var resizeSide = new ResizeSide(windowPosition, _lastWindowPosition);

            // Get the screen the cursor is currently on
            var screen = Screen.FromPoint(MouseHelper.MousePosition);

            // Create a rectangle based on the current working area of the screen
            var snapToBorder = screen.WorkingArea;

            // Deflate the rectangle based on the snap distance
            snapToBorder.Inflate(-snapDistance, -snapDistance);

            if (snapMode == SnapMode.Resize)
            {
                // See if we need to snap on the left
                if (windowPosition.Left < snapToBorder.Left)
                {
                    windowPosition.Width += windowPosition.Left - (int) screen.WorkingArea.Left;
                    windowPosition.Left = (int) screen.WorkingArea.Left;

                    updated = true;
                }

                // See if we need to snap on the right
                if (windowPosition.Right > snapToBorder.Right)
                {
                    windowPosition.Width += (int) screen.WorkingArea.Right - windowPosition.Right;

                    updated = true;
                }

                // See if we need to snap to the top
                if (windowPosition.Top < snapToBorder.Top)
                {
                    windowPosition.Height += windowPosition.Top - (int) screen.WorkingArea.Top;
                    windowPosition.Top = (int) screen.WorkingArea.Top;

                    updated = true;
                }

                // See if we need to snap to the bottom
                if (windowPosition.Bottom > snapToBorder.Bottom)
                {
                    windowPosition.Height += (int) screen.WorkingArea.Bottom - windowPosition.Bottom;

                    updated = true;
                }
            }
            else
            {
                // See if we need to snap on the left
                if (windowPosition.Left < snapToBorder.Left)
                {
                    windowPosition.Left = (int) screen.WorkingArea.Left;
                    updated = true;
                }

                // See if we need to snap on the top
                if (windowPosition.Top < snapToBorder.Top)
                {
                    windowPosition.Top = (int) screen.WorkingArea.Top;
                    updated = true;
                }

                // See if we need to snap on the right
                if (windowPosition.Right > snapToBorder.Right)
                {
                    windowPosition.Left = (int) screen.WorkingArea.Right - windowPosition.Width;
                    updated = true;
                }

                // See if we need to snap on the bottom
                if (windowPosition.Bottom > snapToBorder.Bottom)
                {
                    windowPosition.Top = (int) screen.WorkingArea.Bottom - windowPosition.Height;
                    updated = true;
                }
            }

            var otherWindows = _otherWindows;

            if (otherWindows is { Count: > 0 })
            {
                // Get the snap source rectangles for the window being changed
                var sourceRectangles = new WindowBorderRectangles(windowPosition, 1, false);

                // Loop over all other windows looking to see if we should stick
                foreach (var otherWindow in otherWindows)
                {
                    // Get a rectangle with the bounds of the other window
                    var otherWindowRect = otherWindow.Location;

                    // Get the snap target rectangles for the current window
                    var targetRectangles = new WindowBorderRectangles(otherWindow.Location, snapDistance, true);

                    if (snapMode == SnapMode.Move)
                    {
                        if (sourceRectangles.Left.IntersectsWith(targetRectangles.Right))
                        {
                            windowPosition.Left = otherWindowRect.Right;
                            updated = true;
                        }

                        if (sourceRectangles.Right.IntersectsWith(targetRectangles.Left))
                        {
                            windowPosition.Left = otherWindowRect.Left - windowPosition.Width;
                            updated = true;
                        }

                        if (sourceRectangles.Top.IntersectsWith(targetRectangles.Bottom))
                        {
                            windowPosition.Top = otherWindowRect.Bottom;
                            updated = true;
                        }

                        if (sourceRectangles.Bottom.IntersectsWith(targetRectangles.Top))
                        {
                            windowPosition.Top = otherWindowRect.Top - windowPosition.Height;
                            updated = true;
                        }

                        if (windowPosition.Top == otherWindowRect.Bottom || windowPosition.Bottom == otherWindowRect.Top)
                        {
                            if (Math.Abs(windowPosition.Left - otherWindowRect.Left) <= snapDistance)
                            {
                                windowPosition.Left = otherWindowRect.Left;

                                updated = true;
                            }

                            if (Math.Abs(windowPosition.Right - otherWindowRect.Right) <= snapDistance)
                            {
                                windowPosition.Left = otherWindowRect.Right - windowPosition.Width;

                                updated = true;
                            }
                        }

                        if (windowPosition.Left == otherWindowRect.Right || windowPosition.Right == otherWindowRect.Left)
                        {
                            if (Math.Abs(otherWindowRect.Bottom - windowPosition.Bottom) <= snapDistance)
                            {
                                windowPosition.Top = otherWindowRect.Bottom - windowPosition.Height;

                                updated = true;
                            }

                            if (Math.Abs(otherWindowRect.Top - windowPosition.Top) <= snapDistance)
                            {
                                windowPosition.Top = otherWindowRect.Top;

                                updated = true;
                            }
                        }
                    }
                    else
                    {
                        if (resizeSide.IsLeft)
                        {
                            // Check the current window left against the other window right
                            if (sourceRectangles.Left.IntersectsWith(targetRectangles.Right))
                            {
                                var sign = windowPosition.Left < otherWindowRect.Right ? -1 : 1;

                                windowPosition.Width += Math.Abs(otherWindowRect.Right - windowPosition.Left) * sign;
                                windowPosition.Left = otherWindowRect.Right;

                                updated = true;
                            }
                            else if (windowPosition.Top == otherWindowRect.Bottom || windowPosition.Bottom == otherWindowRect.Top)
                            {
                                if (Math.Abs(windowPosition.Left - otherWindowRect.Left) <= snapDistance)
                                {
                                    var sign = windowPosition.Left < otherWindowRect.Left ? -1 : 1;

                                    windowPosition.Width += Math.Abs(otherWindowRect.Left - windowPosition.Left) * sign;
                                    windowPosition.Left = otherWindowRect.Left;

                                    updated = true;
                                }
                            }
                        }
                        else if (resizeSide.IsRight)
                        {
                            // Check the current window right against the other window left
                            if (sourceRectangles.Right.IntersectsWith(targetRectangles.Left))
                            {
                                var sign = windowPosition.Right < otherWindowRect.Left ? 1 : -1;

                                windowPosition.Width += Math.Abs(otherWindowRect.Left - windowPosition.Right) * sign;

                                updated = true;
                            }
                            else if (windowPosition.Top == otherWindowRect.Bottom || windowPosition.Bottom == otherWindowRect.Top)
                            {
                                if (Math.Abs(windowPosition.Right - otherWindowRect.Right) <= snapDistance)
                                {
                                    var sign = windowPosition.Right < otherWindowRect.Right ? 1 : -1;

                                    windowPosition.Width += Math.Abs(otherWindowRect.Right - windowPosition.Right) * sign;

                                    updated = true;
                                }
                            }
                        }

                        if (resizeSide.IsBottom)
                        {
                            // Check the current window bottom against the other window top
                            if (sourceRectangles.Bottom.IntersectsWith(targetRectangles.Top))
                            {
                                var sign = windowPosition.Bottom < otherWindowRect.Top ? 1 : -1;

                                windowPosition.Height += Math.Abs(otherWindowRect.Top - windowPosition.Bottom) * sign;

                                updated = true;
                            }
                            else if (windowPosition.Left == otherWindowRect.Right || windowPosition.Right == otherWindowRect.Left)
                            {
                                if (Math.Abs(otherWindowRect.Bottom - windowPosition.Bottom) <= snapDistance)
                                {
                                    var sign = windowPosition.Bottom < otherWindowRect.Bottom ? 1 : -1;

                                    windowPosition.Height += Math.Abs(otherWindowRect.Bottom - windowPosition.Bottom) * sign;

                                    updated = true;
                                }
                            }
                        }
                        else if (resizeSide.IsTop)
                        {
                            // Check the current window top against the other window bottom
                            if (sourceRectangles.Top.IntersectsWith(targetRectangles.Bottom))
                            {
                                var sign = windowPosition.Top < otherWindowRect.Bottom ? 1 : -1;

                                windowPosition.Height -= Math.Abs(otherWindowRect.Bottom - windowPosition.Top) * sign;
                                windowPosition.Top = otherWindowRect.Bottom;

                                updated = true;
                            }
                            else if (windowPosition.Left == otherWindowRect.Right || windowPosition.Right == otherWindowRect.Left)
                            {
                                if (Math.Abs(otherWindowRect.Top - windowPosition.Top) <= snapDistance)
                                {
                                    var sign = windowPosition.Top < otherWindowRect.Top ? -1 : 1;

                                    windowPosition.Height += Math.Abs(otherWindowRect.Top - windowPosition.Top) * sign;
                                    windowPosition.Top = otherWindowRect.Top;

                                    updated = true;
                                }
                            }
                        }
                    }
                }
            }

            // Update the last window position
            _lastWindowPosition = windowPosition;

            if (!updated) return nint.Zero;

            Marshal.StructureToPtr(windowPosition, lParam, true);
            handled = true;

            return nint.Zero;
        }

        #endregion
    }
}
