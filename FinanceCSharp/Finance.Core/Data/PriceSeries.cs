namespace Finance.Core.Data;

public sealed record PriceSeries(string Ticker, IReadOnlyList<PriceBar> Bars)
{
    public double[] Closes => Bars.Select(b => b.Close).ToArray();
}
