namespace CadDirect2DHitTestFixedDemo.Cad;

public readonly struct CadPoint
{
    public readonly double X;
    public readonly double Y;

    public CadPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static CadPoint operator +(CadPoint a, CadPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static CadPoint operator -(CadPoint a, CadPoint b) => new(a.X - b.X, a.Y - b.Y);
}
