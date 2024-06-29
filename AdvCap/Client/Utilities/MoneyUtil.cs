using System;

public static class MoneyUtil
{
    public static string MoneyToString(double cost)
    {
        string[] moneyConstants = { "", "k", "M", "B", "T" };
        int costSimplification = (int)Math.Floor(Math.Min((cost.ToString("0").Length - 1) / 3.0, moneyConstants.Length - 1));
        cost /= Math.Pow(1000, costSimplification);
        string mark = moneyConstants[costSimplification];
        return $"${cost:F2}{mark}";
    }
}
