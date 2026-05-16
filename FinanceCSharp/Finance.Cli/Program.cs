using System.Diagnostics;
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
trace.SetValue("name", "Takens");
trace.SetValue("marker", new
{
    size = 8,
    color = colorStrings,
    opacity = 0.9
});

var xAxis = new LinearAxis();
xAxis.SetValue("title", new { text = "x = 1 - rho1" });

var yAxis = new LinearAxis();
yAxis.SetValue("title", new { text = "y = 1 - rho2" });

var zAxis = new LinearAxis();
zAxis.SetValue("title", new { text = "z = 100 * mu1" });

var scene = new Scene();
scene.SetValue("xaxis", xAxis);
scene.SetValue("yaxis", yAxis);
scene.SetValue("zaxis", zAxis);

var layout = new Layout();
// layout.SetValue("title", "Takens Clustering");
layout.SetValue("showlegend", false);
layout.SetValue("scene", scene);
layout.SetValue("width", 1400);
layout.SetValue("height", 950);

var chart = GenericChart.ofTraceObject(true, trace).WithLayout(layout);

var outputPath = Path.Combine(Environment.CurrentDirectory, "output", "index.html");
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(chart, outputPath, OpenInBrowser: false);

var html = File.ReadAllText(outputPath);

var summaryHtml = """
<main style="max-width: 1400px; margin: 0 auto; padding: 24px;">
  <h1 style="font-family: Arial, sans-serif; color: #222; margin: 0 0 12px 0;">
    Takens Clustering
  </h1>
  <p style="max-width: 900px; margin: 0 0 24px 0; font-family: Arial, sans-serif; line-height: 1.6; color: #222;">
    A C# prototype for clustering financial time series using Takens-style embeddings,
    regression-derived features, and Gaussian mixture models. The interactive chart
    shows how assets group together in a reduced risk-return space.
  </p>
""";

html = html.Replace(
    "<body>",
    """<body style="margin:0; background:#f7f7f7;">""" + summaryHtml
);

html = html.Replace(
    "</body>",
    """
</main>
</body>
"""
);

File.WriteAllText(outputPath, html);

Process.Start(new ProcessStartInfo
{
    FileName = outputPath,
    UseShellExecute = true
});

Console.WriteLine($"Saved chart to: {Path.GetFullPath(outputPath)}");