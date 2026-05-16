namespace Finance.Core.Takens;

public static class TakensEmbedding
{
    public static IReadOnlyList<TakensPoint> Embed(
        IReadOnlyList<double> series,
        int delay)
    {
        if (series is null)
            throw new ArgumentNullException(nameof(series));

        if (delay <= 0)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be positive.");

        int n = series.Count - 2 * delay;
        if (n <= 0)
            throw new ArgumentException(
                "Series is too short for the requested delay.",
                nameof(series));

        var points = new List<TakensPoint>(n);

        for (int i = 0; i < n; i++)
        {
            var x = series[i];
            var y = series[i + delay];
            var z = series[i + 2 * delay];

            points.Add(new TakensPoint(x, y, z));
        }

        return points;
    }

    public static double[] ProjectX(IReadOnlyList<TakensPoint> points)
        => points.Select(p => p.X).ToArray();

    public static double[] ProjectY(IReadOnlyList<TakensPoint> points)
        => points.Select(p => p.Y).ToArray();

    public static double[] ProjectZ(IReadOnlyList<TakensPoint> points)
        => points.Select(p => p.Z).ToArray();
}
