namespace Finance.Core.Clustering;

public sealed record GmmClusterResult(
    int[] Labels,
    double[][] Probabilities,
    object Model
);