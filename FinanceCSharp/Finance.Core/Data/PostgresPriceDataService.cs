using Npgsql;

namespace Finance.Core.Data;

public sealed class PostgresPriceDataService
{
    private readonly string _connectionString;

    public PostgresPriceDataService(DbOptions options)
    {
        _connectionString = options.ToConnectionString();
    }

    public async Task<PriceSeries> GetPriceSeriesAsync(
        string ticker,
        int years,
        bool useLog = false,
        CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.Today.AddDays(-(365 * years));

        const string sql = """
            SELECT p.date, p.price
            FROM finance_price p
            INNER JOIN finance_company c
                ON p.company_id = c.id
            WHERE c.ticker = @ticker
              AND p.date >= @startDate
            ORDER BY p.date
            """;

        var bars = new List<PriceBar>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("ticker", ticker);
        cmd.Parameters.AddWithValue("startDate", startDate);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var date = reader.GetDateTime(0);
            var priceDecimal = reader.GetDecimal(1);
            double close = (double)priceDecimal;

            if (useLog)
                close = Math.Log(close);

            bars.Add(new PriceBar(date, close));
        }

        if (bars.Count == 0)
            throw new InvalidOperationException($"No price data found for {ticker}.");

        return new PriceSeries(ticker, bars);
    }

    public async Task<IReadOnlyList<PriceSeries>> GetPriceSeriesAsync(
        IEnumerable<string> tickers,
        int years,
        bool useLog = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PriceSeries>();

        foreach (var ticker in tickers)
        {
            var series = await GetPriceSeriesAsync(ticker, years, useLog, cancellationToken);
            results.Add(series);
        }

        return results;
    }
}
