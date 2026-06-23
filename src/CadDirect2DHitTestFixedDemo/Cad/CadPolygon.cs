using System.Numerics;
using Vortice.Direct2D1;

namespace CadDirect2DHitTestFixedDemo.Cad;

public sealed class CadPolygon : IDisposable
{
    private readonly List<CadPoint> _points;
    private ID2D1PathGeometry? _localGeometry;
    private bool _geometryDirty = true;

    public int Id { get; }
    public CadRect Bounds { get; private set; }
    public CadPoint GeometryOrigin { get; private set; }

    public IReadOnlyList<CadPoint> Points => _points;

    public CadPolygon(int id, IReadOnlyList<CadPoint> points)
    {
        if (points.Count < 3)
            throw new ArgumentException("Polygon requires at least 3 points.", nameof(points));

        Id = id;
        _points = points.ToList();
        UpdateBoundsAndOrigin();
    }

    public void Draw(
        ID2D1Factory factory,
        ID2D1RenderTarget renderTarget,
        CadView view,
        ID2D1Brush fillBrush,
        ID2D1Brush strokeBrush,
        float screenStrokeWidth)
    {
        EnsureGeometry(factory);

        if (_localGeometry == null)
            return;

        // local -> screen:
        // screen = local * zoom + ((origin - viewCenter) * zoom + screenCenter)
        double screenOriginX = (GeometryOrigin.X - view.ViewCenter.X) * view.Zoom + view.ScreenCenterX;
        double screenOriginY = (GeometryOrigin.Y - view.ViewCenter.Y) * view.Zoom + view.ScreenCenterY;

        Matrix3x2 oldTransform = renderTarget.Transform;
        renderTarget.Transform = new Matrix3x2(
            (float)view.Zoom, 0,
            0, (float)view.Zoom,
            (float)screenOriginX, (float)screenOriginY);

        renderTarget.FillGeometry(_localGeometry, fillBrush);

        // Because the geometry is scaled by the render transform,
        // divide by zoom so the final stroke width stays fixed in screen pixels.
        float localStrokeWidth = (float)(screenStrokeWidth / view.Zoom);
        renderTarget.DrawGeometry(_localGeometry, strokeBrush, localStrokeWidth);

        renderTarget.Transform = oldTransform;
    }

    public bool HitTest(CadPoint worldPoint, double toleranceWorld, bool fillSelectable, out double score)
    {
        score = double.MaxValue;

        // Bounds is only a coarse filter; always inflate by pick tolerance.
        if (!Bounds.Inflate(toleranceWorld).Contains(worldPoint))
            return false;

        // For filled polygons, inside hit should be accepted.
        // Direct2D Geometry is deliberately NOT used here because it is float-based.
        if (fillSelectable && PointInPolygon(worldPoint, _points))
        {
            score = 0.0;
            return true;
        }

        double minDistance = double.MaxValue;

        for (int i = 0; i < _points.Count; i++)
        {
            CadPoint a = _points[i];
            CadPoint b = _points[(i + 1) % _points.Count];
            double distance = DistancePointToSegment(worldPoint, a, b);

            if (distance < minDistance)
                minDistance = distance;
        }

        if (minDistance <= toleranceWorld)
        {
            score = minDistance;
            return true;
        }

        return false;
    }

    private void EnsureGeometry(ID2D1Factory factory)
    {
        if (!_geometryDirty && _localGeometry != null)
            return;

        _localGeometry?.Dispose();
        _localGeometry = factory.CreatePathGeometry();

        using ID2D1GeometrySink sink = _localGeometry.Open();
        sink.SetFillMode(FillMode.Winding);

        CadPoint first = _points[0];
        sink.BeginFigure(
            new Vector2(
                (float)(first.X - GeometryOrigin.X),
                (float)(first.Y - GeometryOrigin.Y)),
            FigureBegin.Filled);

        for (int i = 1; i < _points.Count; i++)
        {
            CadPoint p = _points[i];
            sink.AddLine(new Vector2(
                (float)(p.X - GeometryOrigin.X),
                (float)(p.Y - GeometryOrigin.Y)));
        }

        sink.EndFigure(FigureEnd.Closed);
        sink.Close();

        _geometryDirty = false;
    }

    private void UpdateBoundsAndOrigin()
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (CadPoint p in _points)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
        }

        Bounds = new CadRect(minX, minY, maxX, maxY);

        // Center is usually better than Left/Top because local coordinates stay smaller.
        GeometryOrigin = Bounds.Center;
        _geometryDirty = true;
    }

    private static bool PointInPolygon(CadPoint p, IReadOnlyList<CadPoint> polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            CadPoint pi = polygon[i];
            CadPoint pj = polygon[j];

            bool crossesHorizontalRay =
                ((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y) + pi.X);

            if (crossesHorizontalRay)
                inside = !inside;
        }

        return inside;
    }

    private static double DistancePointToSegment(CadPoint p, CadPoint a, CadPoint b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;

        double len2 = dx * dx + dy * dy;
        if (len2 <= 1e-24)
            return Distance(p, a);

        double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
        t = Math.Clamp(t, 0.0, 1.0);

        double x = a.X + t * dx;
        double y = a.Y + t * dy;

        double ddx = p.X - x;
        double ddy = p.Y - y;
        return Math.Sqrt(ddx * ddx + ddy * ddy);
    }

    private static double Distance(CadPoint a, CadPoint b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public void Dispose()
    {
        _localGeometry?.Dispose();
        _localGeometry = null;
    }
}
