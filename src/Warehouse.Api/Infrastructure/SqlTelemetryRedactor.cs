using System.Diagnostics;
using OpenTelemetry;

namespace Warehouse.Api.Infrastructure;

/*
    NOTE: This will come in handy later on -- currently only used in 1 test

    Removes sensitive SQL details from OpenTelemetry traces before they get exported

    Identifies SQL Server activities, removes certain tags, replaces span's name and query summary.

    *** Be sure this is registered in Program.cs BEFORE the exporters
            so its changes happen before they receive the completed span.
 */

public sealed class SqlTelemetryRedactor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.GetTagItem("db.system.name") as string != "microsoft.sql_server" &&
            activity.Source.Name != "OpenTelemetry.Instrumentation.SqlClient" &&
            activity.GetTagItem("db.system") as string != "mssql")
                return;

        foreach (var tag in activity.TagObjects.ToArray())
        {
            if (tag.Key is "db.query.text" or "db.statement" or "db.connection_string" || 
                tag.Key.StartsWith("db.query.parameter.", StringComparison.Ordinal))
            {
                activity.SetTag(tag.Key, null);
            }
        }

        var name = activity.GetTagItem("db.stored_procedure.name") as string;
        activity.SetTag("db.query.summary", name ?? "SQL command");
        activity.DisplayName = name ?? "SQL command";
    }
}

