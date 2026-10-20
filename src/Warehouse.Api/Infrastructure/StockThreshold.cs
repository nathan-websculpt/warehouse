using System.Globalization;

namespace Warehouse.Api.Infrastructure;

/*
    parses/validates the low-stock threshold used by the stock availability report 
        (quantity below which a product is "LOW_STOCK")
    
    ensures that the value fits the database's DECIMAL(19,4) 
        19 digits total -- 4 reserved for decimal places

    
    StockAvailabilityController receives lowStockThreshold as text and calls TryParse
        invalid input is stopped before the database gets queried

    StockAvailabilityQuery calls IsValid again before opening a connection
        this protects callers that hit the query directly with a decimal


    dbo.GetWarehouseStockAvailability uses this parameter to assign the report's "stock status"
                     0   _>   OUT_OF_STOCK
        Below       20   _>   LOW_STOCK
        At or Above 20   _>   OK
*/

public static class StockThreshold
{
    public const decimal Maximum = 999999999999999.9999m;
    public const string ValidationMessage = "Use a decimal between 0 and 999999999999999.9999 with at most four fractional digits, without grouping separators or exponents.";

    // inspect the text before Decimal.TryParse can round an overly precise value
    public static bool TryParse(string? text, out decimal value)
    {
        // if text is null, succeed with 20m (omitted means threshold of 20)
        value = 20m;
        if (text is null)
            return true;

        // empty or long input gets rejected
        text = text.Trim();
        if (text.Length is 0 or > 256)
            return false;

        // temporarily skip +/-, split around decimal, check remaining chars -- rejects commas, exponents, extra decimal points, letters, etc
        var unsigned = text.AsSpan();
        if (unsigned[0] is '+' or '-')
            unsigned = unsigned[1..];
        if (unsigned.IsEmpty)
            return false;

        var dot = unsigned.IndexOf('.');
        var integer = dot < 0 ? unsigned : unsigned[..dot];
        var fraction = dot < 0 ? ReadOnlySpan<char>.Empty : unsigned[(dot + 1)..];
        if (integer.IsEmpty && fraction.IsEmpty)
            return false;


        foreach (var character in integer) { if (character is < '0' or > '9') { return false; } }
        foreach (var character in fraction) { if (character is < '0' or > '9') { return false; } }
        if (integer.TrimStart('0').Length > 15 || fraction.TrimEnd('0').Length > 4) { return false; }


        return decimal.TryParse(
            text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value) && IsValid(value);
    }
       
    // checks parsed number
    public static bool IsValid(decimal value) => 
        value >= 0 && 
        value <= Maximum && 
        decimal.Round(value, 4) == value;


}
