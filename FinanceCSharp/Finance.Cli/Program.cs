using Finance.Core.Data;
using Finance.Core.TakensClustering;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;

var db = new PostgresPriceDataService(new DbOptions());
var service = new TakensClusteringService(db);

var tickers = new[]
{
    "VOO", "VGT", "CDNS", "TSM", "CTAS",
    "WM", "MA", "APO", "BRK-B", "RACE", "AEE", "RIVN",
    "DIS", "SE", "PI", "SFM", "AUR", "FRNW", "QQQ", "SNPS",
};

// sorted tickers
tickers = tickers.OrderBy(t => t).ToArray();

var result = await service.RunAsync(
    tickers: tickers,
    years: 3,
    useLog: false,
    usePca: false,
    nComponents: 6,
    useRsqr: true
);

var points = result.Coordinates
    .Select((c, i) => new
    {
        Ticker = c.Ticker,
        X = 1.0 - c.Rho1,
        Y = 1.0 - c.Rho2,
        Z = 100.0 * c.Mu1,
        Cluster = result.Labels[i]
    })
    .OrderBy(p => p.Ticker)
    .ToArray();

var palette = new[]
{
    "#1f77b4", "#d62728", "#2ca02c",
    "#ff7f0e", "#9467bd", "#8c564b"
};

var x = points.Select(p => p.X).ToArray();
var y = points.Select(p => p.Y).ToArray();
var z = points.Select(p => p.Z).ToArray();
var labels = points.Select(p => p.Ticker).ToArray();
var colors = points
    .Select(p => palette[Math.Abs(p.Cluster) % palette.Length])
    .ToArray();

var marker = new Marker();
marker.SetValue("size", 8);
marker.SetValue("color", colors);
marker.SetValue("opacity", 0.90);

var trace = new Trace("scatter3d");
trace.SetValue("x", x);
trace.SetValue("y", y);
trace.SetValue("z", z);
trace.SetValue("mode", "markers+text");
trace.SetValue("text", labels);
trace.SetValue("textposition", "top center");
trace.SetValue("hovertext", labels);
trace.SetValue("hoverinfo", "text");
trace.SetValue("name", "Takens");
trace.SetValue("marker", marker);

var xAxis = new LinearAxis();
xAxis.SetValue("title", "x = 1 - rho1");

var yAxis = new LinearAxis();
yAxis.SetValue("title", "y = 1 - rho2");

var zAxis = new LinearAxis();
zAxis.SetValue("title", "z = 100 * mu1");

var scene = new Scene();
scene.SetValue("xaxis", xAxis);
scene.SetValue("yaxis", yAxis);
scene.SetValue("zaxis", zAxis);

var layout = new Layout();
layout.SetValue("title", "Takens Clustering");
layout.SetValue("width", 1400);
layout.SetValue("height", 950);
layout.SetValue("showlegend", false);
layout.SetValue("scene", scene);

var chart = GenericChart
    .ofTraceObject(true, trace)
    .WithLayout(layout);

var outputPath = Path.Combine(
    Environment.CurrentDirectory, "Output" ,"takens-clustering.html");
Plotly.NET.GenericChartExtensions.SaveHtml(chart, outputPath);

Console.WriteLine($"Saved chart to: {outputPath}");

foreach (var p in points)
{
    Console.WriteLine(
        $"{p.Ticker,-6}  x={p.X:F4}  y={p.Y:F4}  z={p.Z:F4}  cluster={p.Cluster}");
}
