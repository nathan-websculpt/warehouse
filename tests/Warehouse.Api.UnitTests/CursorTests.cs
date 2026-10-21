using Warehouse.Api.Infrastructure;

namespace Warehouse.Api.UnitTests;

public sealed class CursorTests
{
    [Fact]
    public void Cursor_round_trips_and_rejects_another_search()
    {
        var context = TransferCursor.ContextFor("transfers", "Basil", 1, null, 5);
        var date = new DateTime(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc).AddTicks(1234);
        var text = TransferCursor.Encode(date, 8, true, context);

        Assert.True(TransferCursor.TryRead(text, context, out var cursor));
        Assert.Equal(date, cursor!.CreatedAtUtc);
        Assert.True(cursor.Backwards);
        Assert.Equal(8, cursor.Id);
        Assert.False(TransferCursor.TryRead(text, TransferCursor.ContextFor("transfers", "Basil", 2, null, 5), out _));
        Assert.False(TransferCursor.TryRead(text, TransferCursor.ContextFor("transfer-lines", "Basil", 1, null, 5), out _));
        Assert.False(TransferCursor.TryRead(text, TransferCursor.ContextFor("transfers", "Basil", 1, null, 10), out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!!")]
    [InlineData("a")]
    [InlineData("bnVsbA")]
    public void Invalid_cursor_is_rejected(string text) => 
        Assert.False(TransferCursor.TryRead(text, "context", out _));


    [Fact]
    public void Backwards_page_is_returned_in_display_order()
    {
        var date = DateTime.UtcNow;
        var rows = new List<CursorRow<int>> { new(date, 4, 4), new(date, 3, 3), new(date, 2, 2) };
        var page = CursorPage.Create(rows, 2, new(1, date, 5, true, "context"), "context");

        Assert.Equal([3, 4], page.Items);
        Assert.NotNull(page.NextCursor);
        Assert.NotNull(page.PreviousCursor);

        var firstPage = CursorPage.Create(new List<CursorRow<int>> 
            {
                new(date, 2, 2),
                new(date, 1, 1)
            },
            2,
            new(1, date, 3, true, "context"),
            "context");
           
        Assert.Null(firstPage.PreviousCursor);
        Assert.Equal([1, 2], firstPage.Items);
    }

}