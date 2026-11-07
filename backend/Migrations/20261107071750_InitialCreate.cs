using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("areas_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                    table.CheckConstraint("users_role_check", "role IN ('Foreman', 'Mechanic', 'Fitter', 'ShopChief')");
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    area_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("equipment_pkey", x => x.id);
                    table.ForeignKey(
                        name: "equipment_area_id_fkey",
                        column: x => x.area_id,
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "New"),
                    equipment_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    assignee_user_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("work_orders_pkey", x => x.id);
                    table.CheckConstraint("work_orders_description_check", "char_length(btrim(description)) BETWEEN 10 AND 500");
                    table.CheckConstraint("work_orders_status_check", "status IN ('New', 'InProgress', 'Closed', 'Cancelled')");
                    table.CheckConstraint("work_orders_title_check", "char_length(btrim(title)) BETWEEN 5 AND 80");
                    table.CheckConstraint("work_orders_work_needs_assignee_chk", "status IN ('New', 'Cancelled') OR assignee_user_id IS NOT NULL");
                    table.ForeignKey(
                        name: "work_orders_assignee_user_id_fkey",
                        column: x => x.assignee_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "work_orders_created_by_user_id_fkey",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "work_orders_equipment_id_fkey",
                        column: x => x.equipment_id,
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "areas",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Токарный участок" },
                    { 2, "Фрезерный участок" },
                    { 3, "Сборочный участок" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "full_name", "login", "password_hash", "role" },
                values: new object[,]
                {
                    { 1, "Иванов А. П.", "ivanov", null, "Foreman" },
                    { 2, "Кузнецов Д. В.", "kuznetsov", null, "Mechanic" },
                    { 3, "Орлов Е. Н.", "orlov", null, "ShopChief" },
                    { 4, "Смирнов О. Л.", "smirnov", null, "Foreman" },
                    { 5, "Петров С. И.", "petrov", null, "Fitter" },
                    { 6, "Сидоров В. А.", "sidorov", null, "Fitter" }
                });

            migrationBuilder.InsertData(
                table: "equipment",
                columns: new[] { "id", "area_id", "inventory_number", "name" },
                values: new object[,]
                {
                    { 1, 1, "ИНВ-04501", "Токарный станок 16К20" },
                    { 2, 3, "ИНВ-04502", "Кран-балка 5 т, пролёт 1" },
                    { 3, 1, "ИНВ-04512", "Токарный станок с ЧПУ 16А20Ф3" },
                    { 4, 1, "ИНВ-04513", "Токарный станок с ЧПУ 16А20Ф3" },
                    { 5, 2, "ИНВ-04520", "Фрезерный станок 6Р82" },
                    { 6, 2, "ИНВ-04521", "Вертикально-фрезерный станок с ЧПУ" },
                    { 7, 3, "ИНВ-04530", "Гидравлический пресс П6324" },
                    { 8, 3, "ИНВ-04531", "Кривошипный пресс КД2128" },
                    { 9, 3, "ИНВ-04540", "Кран-балка 3,2 т, пролёт 2" },
                    { 10, 2, "ИНВ-04550", "Сверлильный станок 2Н135" },
                    { 11, 2, "ИНВ-04560", "Шлифовальный станок 3Е711" },
                    { 12, 3, "ИНВ-04570", "Компрессор винтовой ВК-20" }
                });

            migrationBuilder.CreateIndex(
                name: "areas_name_key",
                table: "areas",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "equipment_area_id_idx",
                table: "equipment",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "equipment_inventory_number_key",
                table: "equipment",
                column: "inventory_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_login_key",
                table: "users",
                column: "login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "work_orders_assignee_user_id_idx",
                table: "work_orders",
                column: "assignee_user_id");

            migrationBuilder.CreateIndex(
                name: "work_orders_created_at_idx",
                table: "work_orders",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "work_orders_created_by_user_id_idx",
                table: "work_orders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "work_orders_equipment_id_idx",
                table: "work_orders",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "work_orders_number_key",
                table: "work_orders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "work_orders_status_idx",
                table: "work_orders",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_orders");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "equipment");

            migrationBuilder.DropTable(
                name: "areas");
        }
    }
}
