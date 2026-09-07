namespace KopiKala.Helpers;

public static class InvoiceCodeHelper
{
    public static string GenerateInvoiceCode(DateOnly date)
    {
        var dateStr = date.ToString("yyyyMMdd");
        var randomStr = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"INV/{dateStr}/{randomStr}";
    }
}
