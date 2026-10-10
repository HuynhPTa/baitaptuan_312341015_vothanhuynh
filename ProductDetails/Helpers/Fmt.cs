using System.Globalization;

namespace ProductDetails.Helpers;

public static class Fmt
{
    private static readonly CultureInfo Vi = new("vi-VN");

    public static string Vnd(this decimal value) => value.ToString("N0", Vi) + " ₫";
}
