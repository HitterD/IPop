using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    BodyPlain = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploaderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImAttachments_ImMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ImMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Folder = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImRecipients_ImMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ImMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImAttachments_MessageId",
                table: "ImAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ImMessages_ParentMessageId",
                table: "ImMessages",
                column: "ParentMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ImMessages_SenderUserId_SentAt",
                table: "ImMessages",
                columns: new[] { "SenderUserId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImRecipients_MessageId_UserId",
                table: "ImRecipients",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImRecipients_UserId_Folder_IsRead",
                table: "ImRecipients",
                columns: new[] { "UserId", "Folder", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImAttachments");

            migrationBuilder.DropTable(
                name: "ImRecipients");

            migrationBuilder.DropTable(
                name: "ImMessages");
        }
    }
}
