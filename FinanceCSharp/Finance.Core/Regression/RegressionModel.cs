using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.Statistics;

namespace Finance.Core.Regression;

public enum RegressionType
{
    Linear,
    Exponential,
    Best
}

public sealed class RegressionModel
{
    public int Const { get; }
    public double[] KValues { get; }
    public double[] KPrices => KValues;

    public double Alpha { get; private set; }
    public double Beta { get; private set; }
    public double PearsonRho { get; private set; }
    public double[] Approxes { get; private set; } = Array.Empty<double>();
    public RegressionType Type { get; private set; }

    public RegressionModel(IEnumerable<double> values, RegressionType type)
    {
        var raw = values.ToArray();
        if (raw.Length == 0)
            throw new ArgumentException("Values must not be empty.", nameof(values));

        // This regression fitter may not have same limitations numpy had
        Const = 1; // GetKonst(raw);
        KValues = Coarsen(raw, Const);

        FitByType(KValues, type);
    }

    public double Predict(int daysAhead)
    {
        if (Approxes.Length == 0)
            throw new InvalidOperationException("Model is not fitted yet.");

        int n = Approxes.Length;
        int tFuture = n + daysAhead;

        return Type switch
        {
            RegressionType.Exponential => ExpF(tFuture, Alpha, Beta),
            RegressionType.Linear => LinF(tFuture, Alpha, Beta),
            _ => throw new InvalidOperationException($"Unknown model type: {Type}")
        };
    }

    private void FitByType(double[] values, RegressionType requestedType)
    {
        switch (requestedType)
        {
            case RegressionType.Linear:
                ApplyFit(Fit(values, RegressionType.Linear));
                break;

            case RegressionType.Exponential:
                ApplyFit(Fit(values, RegressionType.Exponential));
                break;

            case RegressionType.Best:
                var expFit = Fit(values, RegressionType.Exponential);
                var linFit = Fit(values, RegressionType.Linear);
                ApplyFit(expFit.PearsonRho > linFit.PearsonRho ? expFit : linFit);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(requestedType), requestedType, null);
        }
    }

    private void ApplyFit(FitResult fit)
    {
        Alpha = fit.Alpha;
        Beta = fit.Beta;
        PearsonRho = fit.PearsonRho;
        Approxes = fit.Approxes;
        Type = fit.Type;
    }

    private static FitResult Fit(double[] values, RegressionType type)
    {
        if (type != RegressionType.Linear && type != RegressionType.Exponential)
            throw new ArgumentException("Fit only supports Linear or Exponential.", nameof(type));

        var cleanValues = values
            .Select(v => double.IsNaN(v) ? 0.0 : v)
            .ToArray();

        int n = cleanValues.Length;
        var xs = Enumerable.Range(1, n).Select(i => (double)i).ToArray();

        Func<double, double, double, double> func = type switch
        {
            RegressionType.Linear => LinF,
            RegressionType.Exponential => ExpF,
            _ => throw new InvalidOperationException("Unsupported fit type.")
        };

        double Objective(Vector<double> p)
        {
            double a = p[0];
            double b = p[1];

            double sumAbs = 0.0;
            for (int i = 0; i < n; i++)
            {
                double yHat = func(xs[i], a, b);
                sumAbs += Math.Abs(cleanValues[i] - yHat);
            }

            if (double.IsNaN(sumAbs) || double.IsInfinity(sumAbs))
                return double.MaxValue;

            return sumAbs;
        }

        var initialGuess = Vector<double>.Build.Dense(new[] { 4.0, 0.1 });
        var objective = ObjectiveFunction.Value(Objective);

        var solver = new NelderMeadSimplex(convergenceTolerance: 1e-8, maximumIterations: 10_000);
        var result = solver.FindMinimum(objective, initialGuess);

        double alpha = result.MinimizingPoint[0];
        double beta = result.MinimizingPoint[1];

        var approxes = xs.Select(x => func(x, alpha, beta)).ToArray();
        double rho = Correlation.Pearson(cleanValues, approxes);

        if (double.IsNaN(rho) || double.IsInfinity(rho))
            rho = 0.0;

        return new FitResult(alpha, beta, rho, approxes, type);
    }

    private static int GetKonst(double[] values)
    {
        return values.Length < 361 ? 1 : (int)Math.Ceiling(values.Length / 361.0);
    }

    private static double[] Coarsen(double[] values, int step)
    {
        if (step <= 0)
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be positive.");

        var result = new List<double>();
        for (int i = 0; i < values.Length; i += step)
            result.Add(values[i]);

        return result.ToArray();
    }

    private static double ExpF(double t, double a, double b) => a * Math.Exp(b * t);
    private static double LinF(double t, double a, double b) => a + b * t;

    private readonly record struct FitResult(
        double Alpha,
        double Beta,
        double PearsonRho,
        double[] Approxes,
        RegressionType Type
    );
}
