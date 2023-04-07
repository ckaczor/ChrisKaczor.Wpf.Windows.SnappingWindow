using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace ChrisKaczor.Wpf.Windows;

internal partial class PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

        public static implicit operator System.Drawing.Point(Point p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator Point(System.Drawing.Point p)
        {
            return new Point(p.X, p.Y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left, Top, Right, Bottom;

        public Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public Rect(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

        public int X
        {
            get => Left;
            set { Right -= (Left - value); Left = value; }
        }

        public int Y
        {
            get => Top;
            set { Bottom -= (Top - value); Top = value; }
        }

        public int Height
        {
            get => Bottom - Top;
            set => Bottom = value + Top;
        }

        public int Width
        {
            get => Right - Left;
            set => Right = value + Left;
        }

        public static implicit operator System.Drawing.Rectangle(Rect r)
        {
            return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static implicit operator Rect(System.Drawing.Rectangle r)
        {
            return new Rect(r);
        }

        public static bool operator ==(Rect r1, Rect r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(Rect r1, Rect r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(Rect r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                Rect rect => Equals(rect),
                System.Drawing.Rectangle rectangle => Equals(new Rect(rectangle)),
                _ => false
            };
        }

        public override int GetHashCode()
        {
            return ((System.Drawing.Rectangle) this).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }
    }

    public struct WindowPlacement
    {
        [UsedImplicitly]
        public int Length;

        [UsedImplicitly]
        public int Flags;

        [UsedImplicitly]
        public uint ShowCommand;

        [UsedImplicitly]
        public Point MinPosition;

        [UsedImplicitly]
        public Point MaxPosition;

        [UsedImplicitly]
        public Rect NormalPosition;
    }

    [Flags]
    public enum WindowPositionFlags
    {
        NoMove = 0x0002
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPosition
    {
        public nint Handle;
        public nint HandleInsertAfter;
        public int Left;
        public int Top;
        public int Width;
        public int Height;
        public WindowPositionFlags Flags;

        public int Right => Left + Width;
        public int Bottom => Top + Height;

        public bool IsSameLocationAndSize(WindowPosition compare)
        {
            return compare.Left == Left && compare.Top == Top && compare.Width == Width && compare.Height == Height;
        }

        public bool IsSameSize(WindowPosition compare)
        {
            return compare.Width == Width && compare.Height == Height;
        }
    }

    public enum WindowMessage
    {
        WindowPositionChanging = 0x0046,
        EnterSizeMove = 0x0231,
        ExitSizeMove = 0x0232
    }


    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowPlacement(nint hWnd, ref WindowPlacement lpWindowPlacement);
}