namespace Finance.Core.TakensClustering;

public sealed record TakensClusteringResult(
    IReadOnlyList<TakensAssetCoordinates> Coordinates,
    double[][] Features,
    double[][] FeaturesUsed,
    int[] Labels,
    double[][] Probabilities,
    object? PcaModel,
    object? GmmModel
);
