namespace CadDirect2DHitTestFixedDemo.Cad;

public static class CadSceneFactory
{
    public static List<CadPolygon> CreateDemoPolygons()
    {
        var polygons = new List<CadPolygon>();
        var random = new Random(1);

        double baseX = 100_000_000;
        double baseY = 200_000_000;

        for (int i = 0; i < 2000; i++)
        {
            double cx = baseX + random.NextDouble() * 200_000;
            double cy = baseY + random.NextDouble() * 200_000;

            double w = 800 + random.NextDouble() * 4000;
            double h = 800 + random.NextDouble() * 4000;

            var points = new List<CadPoint>
            {
                new(cx - w / 2, cy - h / 2),
                new(cx + w / 2, cy - h / 2),
                new(cx + w / 2 + random.NextDouble() * 500, cy + h / 2),
                new(cx - w / 2, cy + h / 2 + random.NextDouble() * 500),
            };

            polygons.Add(new CadPolygon(i + 1, points));
        }

        return polygons;
    }
}
