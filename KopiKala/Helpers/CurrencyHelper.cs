using System.Globalization;

namespace KopiKala.Helpers;

public static class CurrencyHelper
{
    private static readonly CultureInfo IndonesianCulture = new("id-ID");

    public static string ToRupiah(this decimal value)
    {
        return $"Rp {value.ToString("N0", IndonesianCulture)}";
    }
}
