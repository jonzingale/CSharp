using Finance.Core.Data;
using Finance.Core.TakensClustering;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;

var db = new PostgresPriceDataService(new DbOptions());
var service = new TakensClusteringService(db);

var tickers = new[]
{
    "VOO", "VGT", "CDNS", "TSM", "CTAS", "PGR",
    "AJG", "WM", "MA", "APO", "BRK-B", "RACE", "AEE",
    "DIS", "SE", "PI", "SFM", "FRNW", "QQQ",
    "SNPS",
};

var result = await service.RunAsync(
    tickers: tickers,
    years: 3,
    useLog: false,
    usePca: false,
    nComponents: 6,
    useRsqr: true
);

var coords = result.Coordinates;

var x = coords.Select(c => 1.0 - c.Rho1).ToArray();
var y = coords.Select(c => 1.0 - c.Rho2).ToArray();
var z = coords.Select(c => 100.0 * c.Mu1).ToArray();
var labels = coords.Select(c => c.Ticker).ToArray();
var clusters = result.Labels;

var palette = new[]
{
    "#1f77b4", "#d62728", "#2ca02c", "#ff7f0e", "#9467bd", "#8c564b"
};

var colorStrings = clusters
    .Select(label => palette[Math.Abs(label) % palette.Length])
    .ToArray();

var trace = new Plotly.NET.Trace("scatter3d");
trace.SetValue("x", x);
trace.SetValue("y", y);
trace.SetValue("z", z);
trace.SetValue("type", "scatter3d");
trace.SetValue("mode", "markers+text");
trace.SetValue("text", labels);
trace.SetValue("textposition", "top center");
trace.SetValue("hovertext", labels);
trace.SetValue("hoverinfo", "text");
trace.SetValue("marker", new
{
    size = 6,
    color = colorStrings,
    opacity = 0.85
});

var xAxis = new LinearAxis();
xAxis.SetValue("title", new { text = "σ1 = 1 - rho1" });

var yAxis = new LinearAxis();
yAxis.SetValue("title", new { text = "σ2 = 1 - rho2" });

var zAxis = new LinearAxis();
zAxis.SetValue("title", new { text = "100 * mu1" });

var scene = new Scene();
scene.SetValue("xaxis", xAxis);
scene.SetValue("yaxis", yAxis);
scene.SetValue("zaxis", zAxis);

var layout = new Layout();
layout.SetValue("title", new { text = "Takens Clustering" });
layout.SetValue("scene", scene);
layout.SetValue("width", 1400);
layout.SetValue("height", 950);
layout.SetValue("margin", new { l = 0, r = 0, b = 0, t = 40 });

var chart = GenericChart.ofTraceObject(true, trace).WithLayout(layout);

var outputPath = Path.Combine(Environment.CurrentDirectory, "..", "output", "takens-clustering.html");
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(chart, outputPath, OpenInBrowser: true);

Console.WriteLine($"Saved chart to: {Path.GetFullPath(outputPath)}");