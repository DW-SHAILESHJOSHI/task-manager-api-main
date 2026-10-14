using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagerApi.Migrations
{
    /// <inheritdoc />
    public partial class RestrictTaskPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Priority is now an enum stored by name. The column type is unchanged,
            // but rows saved before this may hold any text ("high", "banana"), and
            // EF Core would fail to read those back. Normalise the case of valid
            // values and reset anything else to the default, Low.
            migrationBuilder.Sql(
                """
                UPDATE [Tasks] SET [Priority] = N'Low'    WHERE LOWER([Priority]) = N'low';
                UPDATE [Tasks] SET [Priority] = N'Medium' WHERE LOWER([Priority]) = N'medium';
                UPDATE [Tasks] SET [Priority] = N'High'   WHERE LOWER([Priority]) = N'high';
                UPDATE [Tasks] SET [Priority] = N'Low'    WHERE [Priority] NOT IN (N'Low', N'Medium', N'High');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the column type never changed, and the original
            // invalid values cannot be recovered.
        }
    }
}
