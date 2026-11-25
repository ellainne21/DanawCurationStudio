using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Danaw.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Reviewers",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Reviewers",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 1,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "PBKDF2-SHA256$600000$2UXy4/OaNeQ/E/GAwlqR6Q==$9PZUwSjux1HVCXvEPJXr5faj9PMmQA5XmK7KOhJUThE=", "balindong" });

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 2,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "PBKDF2-SHA256$600000$cD7/3tXHgrGnu1k8GAEx3w==$zGdEYGiU+kztAMVLK/ZHqam+P8TiCLKBC0MiC6st+O0=", "balofinos" });

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 3,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "PBKDF2-SHA256$600000$+wqDTy9hJHKr+pF/3vfIQw==$u63nzcVZmW3EQnppkikcxfpi9APjZCZ86wrbs5752+8=", "cabasag" });

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 4,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "PBKDF2-SHA256$600000$dsYFHFp3whEO0fALkA5ZkQ==$lt6/8TDFDpZLLhASa8cRVYNgDV+r3nS6S+3igbYhibU=", "validator" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviewers_Username",
                table: "Reviewers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviewers_Username",
                table: "Reviewers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Reviewers");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Reviewers");
        }
    }
}
