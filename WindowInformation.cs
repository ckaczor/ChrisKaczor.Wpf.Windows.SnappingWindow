using System.Drawing;

namespace ChrisKaczor.Wpf.Windows;

public class WindowInformation
{
    public nint Handle { get; }
    public Rectangle Location { get; }

    public WindowInformation(nint handle)
    {
        Handle = handle;

        var windowPlacement = new PInvoke.WindowPlacement();
        PInvoke.GetWindowPlacement(Handle, ref windowPlacement);

        var normalPosition = windowPlacement.NormalPosition;

        Location = new Rectangle(normalPosition.X, normalPosition.Y, normalPosition.Width, normalPosition.Height);
    }

    public WindowInformation(nint handle, Rectangle location)
    {
        Handle = handle;
        Location = location;
    }
}