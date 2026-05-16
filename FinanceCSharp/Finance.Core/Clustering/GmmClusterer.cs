using Accord.MachineLearning;
using Accord.Statistics.Distributions.Fitting;

namespace Finance.Core.Clustering;

public static class GmmClusterer
{
    public static GmmClusterResult Fit(
        double[][] features,
        int nComponents = 3)
    {
        if (features is null)
            throw new ArgumentNullException(nameof(features));

        if (features.Length == 0)
            throw new ArgumentException("Features must not be empty.", nameof(features));

        if (nComponents <= 0)
            throw new ArgumentOutOfRangeException(nameof(nComponents));

        nComponents = Math.Min(nComponents, features.Length);

        var gmm = new GaussianMixtureModel(nComponents)
        {
            Options = new NormalOptions
            {
                Regularization = 1e-4
            }
        };

        var clusters = gmm.Learn(features);
        var labels = clusters.Decide(features);
        var probabilities = clusters.Probabilities(features);

        return new GmmClusterResult(labels, probabilities, clusters);
    }
}