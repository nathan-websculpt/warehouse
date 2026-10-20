using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Warehouse.Api.Infrastructure;

/*
   NOTE: a cursor is a bookmark for where the next request should continue
        
        Version        --   Identifies the cursor format (currently 1).
        CreatedAtUtc   --   Timestamp of the row used as the pagination boundary.
        Id             --   That row’s database ID, which breaks ties when timestamps match.
        Backwards      --   false for Next; true for Previous.
        Context        --   A fingerprint of the request’s search settings.

  DEV NOTE: 
        Perhaps this is over-engineered, but the point of this project is to learn advanced .NET
        and bring this project up to a very advanced state along the way.
*/


public sealed record TransferCursor(int Version, DateTime CreatedAtUtc, int Id, bool Backwards, string Context)
{
    // hashes resource name, search text, tenant/transfer filters, and page size
    //      validation compares this fingerprint with current request
    //      reusing an unchanged cursor with different settings gets rejected
    //      the context hash identifies the search settings
    public static string ContextFor(string resource, string? query, int? tenantId, Guid? transferId, int pageSize) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(
                        new 
                        { 
                            resource, 
                            query = query?.Trim() ?? "", 
                            tenantId, 
                            transferId, 
                            pageSize 
                        }
                    )
                )
            )
        );


    // serializes cursor to JSON
    //      encodes it as a Base64 string that is safe as a URL
    public static string Encode(DateTime createdAtUtc, int id, bool backwards, string context) =>
        WebEncoders.Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(
                new TransferCursor(
                    1, 
                    createdAtUtc, 
                    id, 
                    backwards, 
                    context
                )
            )
        );


    // decodes/validates incoming cursor (a missing cursor means "start at beginning")
    //      also checks for a positive ID and a valid timestamp that isn't a default
    public static bool TryRead(string? text, string context, out TransferCursor? cursor)
    {
        cursor = null;

        if (text is null)
            return true;


        if (text.Length is 0 or > 1024 || 
            text.Any(
                character => !char.IsAsciiLetterOrDigit(character) && 
                character is not '-' and not '_')
            )
        { 
            return false;
        }
        
        try
        {
            cursor = JsonSerializer.Deserialize<TransferCursor>(WebEncoders.Base64UrlDecode(text));
            
            
            return cursor is { Version: 1, Id: > 0 } && 
                cursor.CreatedAtUtc > DateTime.MinValue && 
                cursor.Context == context;
        }
        catch (Exception exception)
            when (exception is FormatException or JsonException or ArgumentException)
        {
            return false; 
        }
    }

}