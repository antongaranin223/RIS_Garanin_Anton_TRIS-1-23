using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1,
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEK9xYXLLB0yddYWkG5lGhzr5cTQzMD/fJP+wPFPF2VXtHUvz5jW9xHyyVlkeVvNo2Q==");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 2,
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEDrCEArfWCc58XiP0M1Rjed2j8lPg7QZi9W3vkLdEd7l0woJivhLf4KEd/R3qJLijg==");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 3,
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEOU97bNa4jvrTTAs0hqMbBchNumB+rDilC563iT68tf1CS+MA+NmanWO+Hq6tX8iGg==");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 4,
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEB7xdBVySztCh0BsATpxLPeUkXmHpWdF/1orjk3/jGQAzU5RT5/OD+iDStUj1xeo3w==");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 5,
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAELwMJP5H923/eP/y4Hb/clavUK5dCcKiIwu919eTj5B9vmzj8uVyH45zAMMWtfcKAw==");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 6,
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEMIQbS5uxYNDxQClDuL1A5G2EpZ1pMTf0yVJgrt4AEiTY2VjXSW5sIqWQY1qXcqVog==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1,
                column: "password_hash",
                value: null);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 2,
                column: "password_hash",
                value: null);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 3,
                column: "password_hash",
                value: null);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 4,
                column: "password_hash",
                value: null);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 5,
                column: "password_hash",
                value: null);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 6,
                column: "password_hash",
                value: null);
        }
    }
}
