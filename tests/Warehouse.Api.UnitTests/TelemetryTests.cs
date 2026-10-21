using System.Diagnostics;
using Warehouse.Api.Infrastructure;

namespace Warehouse.Api.UnitTests;

public sealed class TelemetryTests
{
    [Fact]
    public void Sql_export_removes_query_text_parameters_and_connection_details()
    {
        using var activity = new Activity("SELECT secret-value").Start();
        activity.SetTag("db.system.name", "microsoft.sql_server");
        activity.SetTag("db.query.text", "SELECT secret-value");
        activity.SetTag("db.statement", "SELECT secret-value");
        activity.SetTag("db.connection_string", "credentials");
        activity.SetTag("db.query.parameter.secret", "secret-value");
        activity.SetTag("db.operation.name", "SELECT");

        new SqlTelemetryRedactor().OnEnd(activity);

        Assert.Equal("SQL command", activity.DisplayName);
        Assert.Null(activity.GetTagItem("db.query.text"));
        Assert.Null(activity.GetTagItem("db.statement"));
        Assert.Null(activity.GetTagItem("db.connection_string"));
        Assert.Null(activity.GetTagItem("db.query.parameter.secret"));
        Assert.Equal("SELECT", activity.GetTagItem("db.operation.name"));
    }
}
