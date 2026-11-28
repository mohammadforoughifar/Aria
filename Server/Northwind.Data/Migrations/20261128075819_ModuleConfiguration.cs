using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northwind.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModuleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtraCompanies",
                schema: "dbo",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeAndroid",
                schema: "dbo",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeIos",
                schema: "dbo",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Users",
                schema: "dbo",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMobileAddons",
                schema: "dbo",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMultiCompany",
                schema: "dbo",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CompaniesIncluded",
                schema: "dbo",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxExtraCompanies",
                schema: "dbo",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                schema: "dbo",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAndroidToman",
                schema: "dbo",
                table: "Categories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceIosToman",
                schema: "dbo",
                table: "Categories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerExtraCompanyToman",
                schema: "dbo",
                table: "Categories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerUserToman",
                schema: "dbo",
                table: "Categories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UsersIncluded",
                schema: "dbo",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ConfiguredUnitPriceToman",
                schema: "dbo",
                table: "CartItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtraCompanies",
                schema: "dbo",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeAndroid",
                schema: "dbo",
                table: "CartItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeIos",
                schema: "dbo",
                table: "CartItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Users",
                schema: "dbo",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraCompanies",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IncludeAndroid",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IncludeIos",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Users",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "AllowMobileAddons",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AllowMultiCompany",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CompaniesIncluded",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "MaxExtraCompanies",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PriceAndroidToman",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PriceIosToman",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PricePerExtraCompanyToman",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PricePerUserToman",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UsersIncluded",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ConfiguredUnitPriceToman",
                schema: "dbo",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ExtraCompanies",
                schema: "dbo",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "IncludeAndroid",
                schema: "dbo",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "IncludeIos",
                schema: "dbo",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "Users",
                schema: "dbo",
                table: "CartItems");
        }
    }
}
