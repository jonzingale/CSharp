using Finance.Core.Clustering;
using Finance.Core.Data;
using Finance.Core.Regression;
using Finance.Core.Takens;

namespace Finance.Core.TakensClustering;

public sealed class TakensClusteringService
{
    private readonly PostgresPriceDataService _priceService;

    public TakensClusteringService(PostgresPriceDataService priceService)
    {
        _priceService = priceService;
    }

    public async Task<TakensClusteringResult> RunAsync(
        IReadOnlyList<string> tickers,
        int years,
        bool useLog = false,
        bool usePca = false,
        int pcaComponents = 2,
        int nComponents = 3,
        bool useRsqr = true,
        CancellationToken cancellationToken = default)
    {
        var delay = GetDelay(years);

        var seriesList = await _priceService.GetPriceSeriesAsync(
            tickers,
            years,
            useLog,
            cancellationToken);

        var coords = GetCoordinates(seriesList, delay);
        var features = BuildFeatures(coords, useRsqr);

        // Placeholder: no PCA yet
        var featuresUsed = features;
        object? pcaModel = null;

        if (usePca)
        {
            // Stub for now — keep plumbing in place
            // Later this becomes real PCA.
            featuresUsed = features;
            pcaModel = null;
        }

        var gmm = GmmClusterer.Fit(featuresUsed, nComponents);

        return new TakensClusteringResult(
            Coordinates: coords,
            Features: features,
            FeaturesUsed: featuresUsed,
            Labels: gmm.Labels,
            Probabilities: gmm.Probabilities,
            PcaModel: pcaModel,
            GmmModel: gmm.Model
        );
    }

    public static int GetDelay(int years)
    {
        if (years <= 3) return 50 * 4;
        if (years <= 5) return 80 * 4;
        if (years <= 7) return 80 * 4;
        if (years <= 10) return 100 * 7;
        if (years <= 15) return 100 * 10;
        return 50;
    }

    public static IReadOnlyList<TakensAssetCoordinates> GetCoordinates(
        IReadOnlyList<PriceSeries> seriesList,
        int delay)
    {
        var results = new List<TakensAssetCoordinates>();

        foreach (var series in seriesList)
        {
            var embedded = TakensEmbedding.Embed(series.Closes, delay);

            var ySeries = TakensEmbedding.ProjectY(embedded);
            var zSeries = TakensEmbedding.ProjectZ(embedded);

            var yModel = new RegressionModel(ySeries, RegressionType.Exponential);
            var zModel = new RegressionModel(zSeries, RegressionType.Exponential);

            double mu1 = Sanitize(yModel.Beta);
            double mu2 = Sanitize(zModel.Beta);
            double rho1 = Sanitize(yModel.PearsonRho);
            double rho2 = Sanitize(zModel.PearsonRho);

            results.Add(new TakensAssetCoordinates(
                series.Ticker,
                mu1,
                mu2,
                rho1,
                rho2
            ));
        }

        return results;
    }

    public static double[][] BuildFeatures(
        IReadOnlyList<TakensAssetCoordinates> coords,
        bool useRsqr = true)
    {
        var features = new List<double[]>();

        foreach (var c in coords)
        {
            features.Add([
                1.0 - c.Rho1,
                1.0 - c.Rho2,
                100.0 * c.Mu1
            ]);
        }

        return features.ToArray();
    }

    private static double Sanitize(double value)
    {
        return double.IsFinite(value) ? value : 0.0;
    }
}
