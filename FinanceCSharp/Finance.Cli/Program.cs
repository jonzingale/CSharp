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
xAxis.SetValue("title", new { text = "x = 1 - ρ1" });

var yAxis = new LinearAxis();
yAxis.SetValue("title", new { text = "y = 1 - ρ2" });

var zAxis = new LinearAxis();
zAxis.SetValue("title", new { text = "z = 100 * μ1" });

var scene = new Scene();
scene.SetValue("xaxis", xAxis);
scene.SetValue("yaxis", yAxis);
scene.SetValue("zaxis", zAxis);
scene.SetValue("domain", new { x = new[] { 0.00, 0.68 }, y = new[] { 0.2, 1.0 } });
scene.SetValue("camera", new
{
    eye = new { x = 1.55, y = -1.55, z = 1.05 },
    center = new { x = -0.08, y = 0.04, z = 0.04 },
    up = new { x = 0.0, y = 0.0, z = 1.0 }
});

var layout = new Layout();
layout.SetValue("showlegend", false);
layout.SetValue("scene", scene);
layout.SetValue("width", 1360);
layout.SetValue("height", 660);
layout.SetValue("margin", new { l = 0, r = 24, b = 0, t = 0, pad = 0 });

var chart = GenericChart.ofTraceObject(true, trace).WithLayout(layout);

var outputPath = Path.Combine(Environment.CurrentDirectory, "output", "index.html");
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(chart, outputPath, OpenInBrowser: false);

var html = File.ReadAllText(outputPath);

var summaryHtml = """
<main style="max-width: 980px; margin: 0 auto; padding: 12px 24px 8px 24px;">
  <h2 style="margin: 0 0 8px 0; font-family: Arial, sans-serif; color: #222;">Takens-Markowitz Portfolio Analysis</h2>

  <p style="max-width: 960px; margin: 0 0 12px 0; font-family: Arial, sans-serif; line-height: 1.45; color: #222;">
    This visualization clusters a basket of equities and ETFs using 3 years of daily price data.
    Each point is built from a Takens-style embedding and exponential-regression features, then grouped
    with a 6-component Gaussian mixture model. In this run, the analysis uses raw prices rather than
    log returns, does not apply PCA, and includes regression strength terms in the clustering features.
  </p>

  <div style="overflow-x: auto; background: white; border-radius: 8px; padding: 4px; box-shadow: 0 2px 10px rgba(0,0,0,0.08);">
""";

html = html.Replace(
    "<body>",
    """<body style="margin:0; background:#f7f7f7;">""" + summaryHtml
);

html = html.Replace(
    """var config = {"responsive":true};""",
    """var config = {"responsive":false};"""
);

html = html.Replace(
    "</body>",
    """
  </div>
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