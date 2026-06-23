namespace CadDirect2DHitTestFixedDemo.Cad;

public readonly struct CadRect
{
    public readonly double Left;
    public readonly double Top;
    public readonly double Right;
    public readonly double Bottom;

    public CadRect(double left, double top, double right, double bottom)
    {
        Left = Math.Min(left, right);
        Top = Math.Min(top, bottom);
        Right = Math.Max(left, right);
        Bottom = Math.Max(top, bottom);
    }

    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public CadPoint Center => new((Left + Right) * 0.5, (Top + Bottom) * 0.5);

    public bool Intersects(CadRect other)
    {
        return !(Right < other.Left ||
                 Left > other.Right ||
                 Bottom < other.Top ||
                 Top > other.Bottom);
    }

    public bool Contains(CadPoint point)
    {
        return point.X >= Left &&
               point.X <= Right &&
               point.Y >= Top &&
               point.Y <= Bottom;
    }

    public CadRect Inflate(double margin)
    {
        return new CadRect(
            Left - margin,
            Top - margin,
            Right + margin,
            Bottom + margin);
    }

    public static CadRect FromCenter(CadPoint center, double halfSize)
    {
        return new CadRect(
            center.X - halfSize,
            center.Y - halfSize,
            center.X + halfSize,
            center.Y + halfSize);
    }

    public static CadRect FromPoints(CadPoint p1, CadPoint p2)
    {
        return new CadRect(p1.X, p1.Y, p2.X, p2.Y);
    }
}
