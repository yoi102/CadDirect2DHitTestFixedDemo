using System.Diagnostics;
using System.Drawing;
using CadDirect2DHitTestFixedDemo.Cad;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace CadDirect2DHitTestFixedDemo;

public sealed class Direct2DCanvas : Control
{
    private const double BaseWorldTolerance = 0.001;
    private const double PickTolerancePixels = 6.0;
    private const float NormalStrokePixels = 1.2f;
    private const float SelectedStrokePixels = 3.0f;

    private readonly List<CadPolygon> _polygons = CadSceneFactory.CreateDemoPolygons();
    private readonly CadView _view = new()
    {
        ViewCenter = new CadPoint(100_100_000, 200_100_000),
        Zoom = 0.004
    };

    private ID2D1Factory? _factory;
    private ID2D1HwndRenderTarget? _renderTarget;
    private ID2D1SolidColorBrush? _fillBrush;
    private ID2D1SolidColorBrush? _strokeBrush;
    private ID2D1SolidColorBrush? _selectedFillBrush;
    private ID2D1SolidColorBrush? _selectedStrokeBrush;

    private CadPolygon? _selected;
    private bool _isPanning;
    private Point _lastMouse;
    private int _lastDrawCount;
    private long _lastRenderMs;

    private readonly Label _debugOverlayLabel;

    public event Action<string>? StatusChanged;

    public Direct2DCanvas()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.Opaque |
            ControlStyles.ResizeRedraw,
            true);

        TabStop = true;
        BackColor = System.Drawing.Color.Black;

        _debugOverlayLabel = new Label
        {
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(12, 8),
            Padding = new Padding(4),
            Font = new Font("Consolas", 10.5f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = System.Drawing.Color.White,
            BackColor = System.Drawing.Color.FromArgb(24, 28, 34),
            TextAlign = ContentAlignment.TopLeft
        };

        Controls.Add(_debugOverlayLabel);
        _debugOverlayLabel.BringToFront();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CreateDeviceIndependentResources();
        CreateDeviceResources();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        DisposeDeviceResources();
        DisposeDeviceIndependentResources();

        foreach (CadPolygon polygon in _polygons)
            polygon.Dispose();

        base.OnHandleDestroyed(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_renderTarget != null && ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            _renderTarget.Resize(new SizeI(ClientSize.Width, ClientSize.Height));
        }

        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Direct2D clears the background. Avoid GDI flicker.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Render();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        _lastMouse = e.Location;

        if (e.Button == MouseButtons.Right)
        {
            _isPanning = true;
            Cursor = Cursors.Hand;
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            SelectAt(e.Location);
            Invalidate();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isPanning)
            return;

        int dx = e.X - _lastMouse.X;
        int dy = e.Y - _lastMouse.Y;

        _view.ViewCenter = new CadPoint(
            _view.ViewCenter.X - dx / _view.Zoom,
            _view.ViewCenter.Y - dy / _view.Zoom);

        _lastMouse = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Right)
        {
            _isPanning = false;
            Cursor = Cursors.Default;
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        _view.ClientSize = ClientSize;

        CadPoint beforeZoomWorld = _view.ScreenToWorld(e.Location);

        double zoomFactor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        _view.Zoom = Math.Clamp(_view.Zoom * zoomFactor, 0.00005, 10.0);

        CadPoint afterZoomWorld = _view.ScreenToWorld(e.Location);

        // Keep the world coordinate below the mouse stable while zooming.
        _view.ViewCenter = new CadPoint(
            _view.ViewCenter.X + beforeZoomWorld.X - afterZoomWorld.X,
            _view.ViewCenter.Y + beforeZoomWorld.Y - afterZoomWorld.Y);

        Invalidate();
    }

    private void SelectAt(Point screenPoint)
    {
        _view.ClientSize = ClientSize;

        CadPoint worldPoint = _view.ScreenToWorld(screenPoint);
        double toleranceWorld = Math.Max(
            _view.ScreenLengthToWorld(PickTolerancePixels),
            BaseWorldTolerance);

        CadPolygon? hit = null;

        // Reverse order simulates top-most object first.
        for (int i = _polygons.Count - 1; i >= 0; i--)
        {
            CadPolygon polygon = _polygons[i];

            if (polygon.HitTest(worldPoint, toleranceWorld, fillSelectable: true, out _))
            {
                hit = polygon;
                break;
            }
        }

        _selected = hit;
    }

    private void Render()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        CreateDeviceIndependentResources();
        CreateDeviceResources();

        if (_factory == null || _renderTarget == null ||
            _fillBrush == null || _strokeBrush == null ||
            _selectedFillBrush == null || _selectedStrokeBrush == null)
            return;

        _view.ClientSize = ClientSize;

        CadRect visibleWorldBounds = _view.GetVisibleWorldBounds();
        double drawMarginWorld = _view.ScreenLengthToWorld(24.0);
        CadRect drawBounds = visibleWorldBounds.Inflate(drawMarginWorld);

        int drawCount = 0;
        var sw = Stopwatch.StartNew();

        _renderTarget.BeginDraw();
        _renderTarget.Transform = System.Numerics.Matrix3x2.Identity;
        _renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
        _renderTarget.Clear(new Color4(0.094f, 0.110f, 0.133f, 1.0f));

        foreach (CadPolygon polygon in _polygons)
        {
            if (!polygon.Bounds.Intersects(drawBounds))
                continue;

            bool selected = ReferenceEquals(polygon, _selected);

            polygon.Draw(
                _factory,
                _renderTarget,
                _view,
                selected ? _selectedFillBrush : _fillBrush,
                selected ? _selectedStrokeBrush : _strokeBrush,
                selected ? SelectedStrokePixels : NormalStrokePixels);

            drawCount++;
        }

        _renderTarget.EndDraw();

        sw.Stop();
        _lastDrawCount = drawCount;
        _lastRenderMs = sw.ElapsedMilliseconds;

        UpdateDebugOverlay(visibleWorldBounds);

        StatusChanged?.Invoke(
            $"Objects={_polygons.Count}, Drawn={_lastDrawCount}, " +
            $"Zoom={_view.Zoom:F6}, Render={_lastRenderMs}ms, " +
            $"Selected={(_selected == null ? "none" : _selected.Id.ToString())}");
    }

    private void UpdateDebugOverlay(CadRect visibleWorldBounds)
    {
        _debugOverlayLabel.Text =
            $"Total Objects: {_polygons.Count}\n" +
            $"Drawn Objects: {_lastDrawCount}\n" +
            $"Zoom: {_view.Zoom:F6}\n" +
            $"ViewCenter: ({_view.ViewCenter.X:F3}, {_view.ViewCenter.Y:F3})\n" +
            $"VisibleWorldBounds:\n" +
            $"  L={visibleWorldBounds.Left:F1}\n" +
            $"  T={visibleWorldBounds.Top:F1}\n" +
            $"  R={visibleWorldBounds.Right:F1}\n" +
            $"  B={visibleWorldBounds.Bottom:F1}\n" +
            $"Render: {_lastRenderMs} ms\n\n" +
            $"Mouse Wheel: Zoom\n" +
            $"Right Drag: Pan\n" +
            $"Left Click: HitTest";

        _debugOverlayLabel.Location = new Point(12, 8);
        _debugOverlayLabel.BringToFront();
    }

    private void CreateDeviceIndependentResources()
    {
        if (_factory != null)
            return;

        _factory = D2D1.D2D1CreateFactory<ID2D1Factory>(FactoryType.SingleThreaded);
    }

    private void CreateDeviceResources()
    {
        if (_renderTarget != null)
            return;

        if (_factory == null)
            return;

        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        var renderTargetProperties = new RenderTargetProperties
        {
            Type = RenderTargetType.Default,
            PixelFormat = new PixelFormat(Format.Unknown, Vortice.DCommon.AlphaMode.Unknown),
            DpiX = 96.0f,
            DpiY = 96.0f,
            Usage = RenderTargetUsage.None,
            MinLevel = FeatureLevel.Default
        };

        var hwndRenderTargetProperties = new HwndRenderTargetProperties
        {
            Hwnd = Handle,
            PixelSize = new SizeI(ClientSize.Width, ClientSize.Height),
            PresentOptions = PresentOptions.None
        };

        _renderTarget = _factory.CreateHwndRenderTarget(
            renderTargetProperties,
            hwndRenderTargetProperties);

        _renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

        _fillBrush = _renderTarget.CreateSolidColorBrush(new Color4(0.255f, 0.627f, 1.0f, 0.22f));
        _strokeBrush = _renderTarget.CreateSolidColorBrush(new Color4(0.470f, 0.745f, 1.0f, 0.86f));
        _selectedFillBrush = _renderTarget.CreateSolidColorBrush(new Color4(1.0f, 0.705f, 0.235f, 0.46f));
        _selectedStrokeBrush = _renderTarget.CreateSolidColorBrush(new Color4(1.0f, 0.882f, 0.270f, 1.0f));
    }

    private void DisposeDeviceResources()
    {
        _selectedStrokeBrush?.Dispose();
        _selectedFillBrush?.Dispose();
        _strokeBrush?.Dispose();
        _fillBrush?.Dispose();
        _renderTarget?.Dispose();

        _selectedStrokeBrush = null;
        _selectedFillBrush = null;
        _strokeBrush = null;
        _fillBrush = null;
        _renderTarget = null;
    }

    private void DisposeDeviceIndependentResources()
    {
        _factory?.Dispose();
        _factory = null;
    }
}
