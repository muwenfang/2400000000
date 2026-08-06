using System.Numerics;

public static class NumberDisplayFormatter
{
    const int ScientificNotationDigitThreshold = 9;
    const int ScientificSignificantDigits = 4;

    public static string Format(BigInteger number)
    {
        string raw = number.ToString();
        bool isNegative = raw.StartsWith("-");
        string digitsOnly = isNegative ? raw.Substring(1) : raw;

        if (digitsOnly.Length > ScientificNotationDigitThreshold)
        {
            return FormatScientific(raw, digitsOnly, isNegative);
        }

        return FormatWithCommas(raw, digitsOnly, isNegative);
    }

    public static string Format(string numericString)
    {
        if (string.IsNullOrEmpty(numericString) || numericString == "0")
        {
            return "0";
        }

        if (BigInteger.TryParse(numericString, out BigInteger result))
        {
            return Format(result);
        }

        return numericString;
    }

    static string FormatScientific(string raw, string digitsOnly, bool isNegative)
    {
        // 取第一位数字 + 小数点 + 后续最多3位小数
        string leadingDigit = digitsOnly.Substring(0, 1);
        string decimals = digitsOnly.Substring(1, System.Math.Min(3, digitsOnly.Length - 1));
        // 不足3位小数时补0
        while (decimals.Length < 3)
        {
            decimals += "0";
        }
        string mantissa = leadingDigit + "." + decimals;
        int exponent = digitsOnly.Length - 1;
        string result = $"{mantissa}e{exponent}";
        return isNegative ? "-" + result : result;
    }

    static string FormatWithCommas(string raw, string digitsOnly, bool isNegative)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int firstGroupLength = digitsOnly.Length % 3;
        if (firstGroupLength == 0)
        {
            firstGroupLength = 3;
        }

        builder.Append(digitsOnly.Substring(0, firstGroupLength));

        for (int i = firstGroupLength; i < digitsOnly.Length; i += 3)
        {
            builder.Append(",");
            builder.Append(digitsOnly.Substring(i, 3));
        }

        return isNegative ? "-" + builder.ToString() : builder.ToString();
    }
}
