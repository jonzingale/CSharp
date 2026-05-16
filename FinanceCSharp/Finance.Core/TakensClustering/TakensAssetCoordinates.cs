namespace Finance.Core.TakensClustering;

public sealed record TakensAssetCoordinates(
    string Ticker,
    double Mu1,
    double Mu2,
    double Rho1,
    double Rho2
);
