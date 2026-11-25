using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Danaw.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDemoPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 1,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$Yx8fqvAhckUhuguzM3+1cA==$LZeoBUy9ho89yRHc46sBAUMgky/LsaNlu16kTkPTGXY=");

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 2,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$yUs4zzD7MRVOlTWQZkU9zQ==$yfjn/zhez6erRallXMzkNUvIcgPwoKyJ9uJIpG2evNc=");

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 3,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$cpGMZ/PJJqFTBGXQo34q5A==$Fww7lMadnYKsmivaFt+iG1M+Aiv2+Mag72rlvRMaTDw=");

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 4,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$PvhdP/mHm28+UeqD50XUpA==$fWUTiC8aX92dNXeJPHN7aYh2QMTnad+FuLj2jl0jIUk=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 1,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$2UXy4/OaNeQ/E/GAwlqR6Q==$9PZUwSjux1HVCXvEPJXr5faj9PMmQA5XmK7KOhJUThE=");

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 2,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$cD7/3tXHgrGnu1k8GAEx3w==$zGdEYGiU+kztAMVLK/ZHqam+P8TiCLKBC0MiC6st+O0=");

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 3,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$+wqDTy9hJHKr+pF/3vfIQw==$u63nzcVZmW3EQnppkikcxfpi9APjZCZ86wrbs5752+8=");

            migrationBuilder.UpdateData(
                table: "Reviewers",
                keyColumn: "ReviewerId",
                keyValue: 4,
                column: "PasswordHash",
                value: "PBKDF2-SHA256$600000$dsYFHFp3whEO0fALkA5ZkQ==$lt6/8TDFDpZLLhASa8cRVYNgDV+r3nS6S+3igbYhibU=");
        }
    }
}
