using System.Drawing;

namespace CadDirect2DHitTestFixedDemo.Cad;

public sealed class CadView
{
    public CadPoint ViewCenter { get; set; }
    public double Zoom { get; set; } = 1.0;
    public Size ClientSize { get; set; }

    public double ScreenCenterX => ClientSize.Width * 0.5;
    public double ScreenCenterY => ClientSize.Height * 0.5;

    public CadPoint ScreenToWorld(Point screenPoint)
    {
        return new CadPoint(
            (screenPoint.X - ScreenCenterX) / Zoom + ViewCenter.X,
            (screenPoint.Y - ScreenCenterY) / Zoom + ViewCenter.Y);
    }

    public CadPoint ScreenToWorld(double screenX, double screenY)
    {
        return new CadPoint(
            (screenX - ScreenCenterX) / Zoom + ViewCenter.X,
            (screenY - ScreenCenterY) / Zoom + ViewCenter.Y);
    }

    public PointF WorldToScreen(CadPoint worldPoint)
    {
        double sx = (worldPoint.X - ViewCenter.X) * Zoom + ScreenCenterX;
        double sy = (worldPoint.Y - ViewCenter.Y) * Zoom + ScreenCenterY;
        return new PointF((float)sx, (float)sy);
    }

    public double ScreenLengthToWorld(double screenLength)
    {
        return screenLength / Zoom;
    }

    public CadRect GetVisibleWorldBounds()
    {
        CadPoint p1 = ScreenToWorld(0, 0);
        CadPoint p2 = ScreenToWorld(ClientSize.Width, ClientSize.Height);
        return CadRect.FromPoints(p1, p2);
    }
}
