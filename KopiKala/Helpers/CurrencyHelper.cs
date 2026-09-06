namespace KopiKala.Helpers;

public static class CurrencyHelper
{
    public static string ToRupiah(this decimal value)
    {
        return $"Rp {value:N0}";
    }
}
