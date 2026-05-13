using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRichAttachmentPresenceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "DirectMessages",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<string>(
                name: "BodyPlain",
                table: "DirectMessages",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "DirectMessages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageType",
                table: "DirectMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DirectAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(127)", maxLength: 127, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScanStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectAttachments_DirectMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "DirectMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectAttachments_MessageId",
                table: "DirectAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectAttachments_SenderUserId_UploadedAt",
                table: "DirectAttachments",
                columns: new[] { "SenderUserId", "UploadedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectAttachments");

            migrationBuilder.DropColumn(
                name: "BodyPlain",
                table: "DirectMessages");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "DirectMessages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "DirectMessages");

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "DirectMessages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000);
        }
    }
}
