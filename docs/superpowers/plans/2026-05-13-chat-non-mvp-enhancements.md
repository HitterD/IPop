# Chat Non-MVP Enhancements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete Phase 2 chat enhancements: rich text, emoji, mentions, attachments, presence, read receipts, profile pane, and light/dark UI.

**Architecture:** Extend existing `IPop.Modules.Chat` vertical slice. Domain/application contracts live in Chat module, EF/storage/Redis/sanitizer live in Infrastructure, endpoints and SignalR hub stay in `ChatModule`/`ChatHub`, and UI is split from the current large `ChatsPage.razor` into focused Razor components.

**Tech Stack:** .NET 8, Blazor Server, MudBlazor, SignalR, EF Core SQL Server, Redis, Ganss.Xss HtmlSanitizer, xUnit, FluentAssertions.

---

## File Structure

Create:
- `src/IPop.Modules.Chat/Domain/MessageType.cs` — `Text`, `RichText`.
- `src/IPop.Modules.Chat/Domain/AttachmentScanStatus.cs` — `Pending`, `Clean`, `Rejected`.
- `src/IPop.Modules.Chat/Domain/DirectAttachment.cs` — attachment entity.
- `src/IPop.Modules.Chat/Application/Messages/ReceiptState.cs` — UI receipt enum.
- `src/IPop.Modules.Chat/Application/Messages/AttachmentDto.cs` — attachment DTO.
- `src/IPop.Modules.Chat/Application/Attachments/IFileStorage.cs` — storage boundary.
- `src/IPop.Modules.Chat/Application/Attachments/FileStorageOptions.cs` — options.
- `src/IPop.Modules.Chat/Application/Content/IChatContentSanitizer.cs` — rich text sanitizer boundary.
- `src/IPop.Modules.Chat/Application/Presence/IPresenceService.cs` — presence boundary.
- `src/IPop.Modules.Chat/Presentation/ChatComposer.razor` — rich composer, emoji, mention, attachment UI.
- `src/IPop.Modules.Chat/Presentation/ChatThreadStream.razor` — message stream + receipts.
- `src/IPop.Modules.Chat/Presentation/ChatProfilePane.razor` — WhatsApp-style profile pane.
- `src/IPop.Modules.Chat/Presentation/ChatPresenceDot.razor` — presence dot.
- `src/IPop.Modules.Chat/Presentation/ChatThemeToggle.razor` — light/dark switch.
- `src/IPop.Modules.Chat/Presentation/EmojiCatalog.cs` — curated emoji set.
- `src/IPop.Infrastructure/Chat/HtmlAgilityChatContentSanitizer.cs` — sanitizer implementation.
- `src/IPop.Infrastructure/Chat/LocalFileStorage.cs` — local/Synology-compatible file storage.
- `src/IPop.Infrastructure/Chat/RedisPresenceService.cs` — Redis presence.
- `src/IPop.Infrastructure/Chat/ChatPresenceSubscriber.cs` — Redis pub/sub to SignalR.
- `src/IPop.Infrastructure/Persistence/Configurations/DirectAttachmentConfiguration.cs`.
- `tests/IPop.UnitTests/Modules.Chat/ChatContentSanitizerTests.cs`.
- `tests/IPop.UnitTests/Modules.Chat/DirectAttachmentTests.cs`.
- `tests/IPop.UnitTests/Modules.Chat/LocalFileStorageTests.cs`.
- `tests/IPop.UnitTests/Modules.Chat/ReadReceiptTests.cs`.

Modify:
- `Directory.Packages.props` — add `HtmlSanitizer` package version.
- `src/IPop.Modules.Chat/IPop.Modules.Chat.csproj` — package/reference updates as needed.
- `src/IPop.Modules.Chat/Domain/DirectMessage.cs` — add `MessageType`, `BodyPlain`, `DeliveredAt`, attachments nav.
- `src/IPop.Modules.Chat/Application/Messages/ChatMessageDto.cs` — add receipt fields, `MessageType`, `BodyPlain`, attachments.
- `src/IPop.Modules.Chat/Application/Messages/SendDirectMessageCommand.cs` — add `BodyHtml`, `MessageType`, `AttachmentIds`.
- `src/IPop.Modules.Chat/Application/Messages/SendDirectMessageCommandValidator.cs` — validate rich body and attachments.
- `src/IPop.Modules.Chat/Application/Abstractions/IChatRepository.cs` — add attachment/read/delivered/profile methods.
- `src/IPop.Infrastructure/Chat/EfChatRepository.cs` — persist attachments, receipts, summaries.
- `src/IPop.Infrastructure/DependencyInjection.cs` — register sanitizer, storage, presence, subscriber.
- `src/IPop.Infrastructure/Persistence/AppDbContext.cs` — add `DirectAttachments` DbSet.
- `src/IPop.Modules.Chat/Presentation/ChatHub.cs` — extend send, heartbeat, mark read.
- `src/IPop.Modules.Chat/Presentation/ChatModule.cs` — map attachment upload/download endpoints.
- `src/IPop.Modules.Chat/Presentation/ChatsPage.razor` — compose components and profile pane.
- `src/IPop.Host/wwwroot/css/chat.css` — light/dark tokens and profile pane styling.
- `src/IPop.Host/Components/Layout/NavMenu.razor` — no call-ext action; keep profile extension display only.
- `src/IPop.Host/appsettings*.json`, `src/IPop.Api/appsettings*.json` — file storage and presence config.

---

### Task 1: Domain + DTO extensions

**Files:**
- Create: `src/IPop.Modules.Chat/Domain/MessageType.cs`
- Create: `src/IPop.Modules.Chat/Domain/AttachmentScanStatus.cs`
- Create: `src/IPop.Modules.Chat/Domain/DirectAttachment.cs`
- Modify: `src/IPop.Modules.Chat/Domain/DirectMessage.cs`
- Modify: `src/IPop.Modules.Chat/Application/Messages/ChatMessageDto.cs`
- Test: `tests/IPop.UnitTests/Modules.Chat/DirectAttachmentTests.cs`

- [ ] Create `MessageType.cs`:
```csharp
namespace IPop.Modules.Chat.Domain;
public enum MessageType { Text = 0, RichText = 1 }
```

- [ ] Create `AttachmentScanStatus.cs`:
```csharp
namespace IPop.Modules.Chat.Domain;
public enum AttachmentScanStatus { Pending = 0, Clean = 1, Rejected = 2 }
```

- [ ] Create failing tests in `DirectAttachmentTests.cs`:
```csharp
using FluentAssertions;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class DirectAttachmentTests
{
    [Fact]
    public void Create_ValidAttachment_SetsFields()
    {
        var senderId = Guid.NewGuid();
        var attachment = DirectAttachment.Create(senderId, "report.pdf", "chat/2026/05/file.pdf", 1024, "application/pdf", DateTimeOffset.UnixEpoch);
        attachment.SenderUserId.Should().Be(senderId);
        attachment.OriginalName.Should().Be("report.pdf");
        attachment.ScanStatus.Should().Be(AttachmentScanStatus.Pending);
    }

    [Fact]
    public void Create_PathTraversalName_Throws()
    {
        var act = () => DirectAttachment.Create(Guid.NewGuid(), "../evil.exe", "chat/x", 1, "application/pdf", DateTimeOffset.UnixEpoch);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AttachToMessage_SetsMessageIdOnce()
    {
        var messageId = Guid.NewGuid();
        var attachment = DirectAttachment.Create(Guid.NewGuid(), "a.png", "chat/a.png", 10, "image/png", DateTimeOffset.UnixEpoch);
        attachment.AttachToMessage(messageId);
        attachment.MessageId.Should().Be(messageId);
    }
}
```

- [ ] Run expected failing test:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet test tests/IPop.UnitTests/IPop.UnitTests.csproj --filter DirectAttachmentTests --nologo
```

- [ ] Create `DirectAttachment.cs`:
```csharp
namespace IPop.Modules.Chat.Domain;

public sealed class DirectAttachment
{
    private DirectAttachment() { }
    public Guid Id { get; private set; }
    public Guid? MessageId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string OriginalName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; private set; }
    public AttachmentScanStatus ScanStatus { get; private set; }
    public DirectMessage? Message { get; private set; }

    public static DirectAttachment Create(Guid senderUserId, string originalName, string storagePath, long sizeBytes, string contentType, DateTimeOffset now)
    {
        if (senderUserId == Guid.Empty) throw new ArgumentException("Sender user id is required.", nameof(senderUserId));
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Contains("..") || originalName.Contains('/') || originalName.Contains('\\')) throw new ArgumentException("Invalid file name.", nameof(originalName));
        if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("Storage path is required.", nameof(storagePath));
        if (sizeBytes <= 0) throw new ArgumentException("File size must be positive.", nameof(sizeBytes));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("Content type is required.", nameof(contentType));
        return new DirectAttachment { Id = Guid.NewGuid(), SenderUserId = senderUserId, OriginalName = originalName.Trim(), StoragePath = storagePath, SizeBytes = sizeBytes, ContentType = contentType.Trim(), UploadedAt = now, ScanStatus = AttachmentScanStatus.Pending };
    }

    public void AttachToMessage(Guid messageId)
    {
        if (MessageId is not null) return;
        MessageId = messageId;
    }
}
```

- [ ] Modify `DirectMessage.cs` to include:
```csharp
private readonly List<DirectAttachment> _attachments = new();
public MessageType MessageType { get; private set; }
public string BodyPlain { get; private set; } = string.Empty;
public DateTimeOffset? DeliveredAt { get; private set; }
public IReadOnlyCollection<DirectAttachment> Attachments => _attachments;
public void MarkDelivered(DateTimeOffset at) => DeliveredAt ??= at;
public void AddAttachment(DirectAttachment attachment) { attachment.AttachToMessage(Id); _attachments.Add(attachment); }
```
Update factory signature to `Create(..., string body, string bodyPlain, MessageType messageType, DateTimeOffset now)` and set `BodyPlain = string.IsNullOrWhiteSpace(bodyPlain) ? body.Trim() : bodyPlain.Trim(); MessageType = messageType;`.

- [ ] Modify `ChatMessageDto.cs`:
```csharp
using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Messages;

public sealed record ChatMessageDto(Guid Id, Guid ConversationId, Guid SenderUserId, Guid RecipientUserId, Guid ClientMessageId, string Body, string BodyPlain, MessageType MessageType, DateTimeOffset SentAt, DateTimeOffset? DeliveredAt, DateTimeOffset? ReadAt, IReadOnlyCollection<AttachmentDto> Attachments);

public sealed record AttachmentDto(Guid Id, string OriginalName, long SizeBytes, string ContentType, string DownloadUrl);
```

- [ ] Run tests:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet test tests/IPop.UnitTests/IPop.UnitTests.csproj --filter "DirectAttachmentTests|DirectConversationTests" --nologo
```

---

### Task 2: Rich text sanitizer

**Files:**
- Modify: `Directory.Packages.props`
- Create: `src/IPop.Modules.Chat/Application/Content/IChatContentSanitizer.cs`
- Create: `src/IPop.Infrastructure/Chat/HtmlAgilityChatContentSanitizer.cs`
- Modify: `src/IPop.Infrastructure/DependencyInjection.cs`
- Test: `tests/IPop.UnitTests/Modules.Chat/ChatContentSanitizerTests.cs`

- [ ] Add package version:
```xml
<PackageVersion Include="HtmlSanitizer" Version="8.1.870" />
```

- [ ] Add package reference to `src/IPop.Infrastructure/IPop.Infrastructure.csproj`:
```xml
<PackageReference Include="HtmlSanitizer" />
```

- [ ] Create interface:
```csharp
namespace IPop.Modules.Chat.Application.Content;
public interface IChatContentSanitizer { ChatContent Sanitize(string input); }
public sealed record ChatContent(string Html, string Plain);
```

- [ ] Create tests:
```csharp
using FluentAssertions;
using IPop.Infrastructure.Chat;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class ChatContentSanitizerTests
{
    [Fact]
    public void Sanitize_StripsScriptAndEventHandlers()
    {
        var sanitizer = new HtmlAgilityChatContentSanitizer();
        var result = sanitizer.Sanitize("<p onclick='x()'>Hi<script>alert(1)</script></p>");
        result.Html.Should().NotContain("script").And.NotContain("onclick");
        result.Plain.Should().Be("Hi");
    }

    [Fact]
    public void Sanitize_KeepsMentionSpanAndSafeLink()
    {
        var sanitizer = new HtmlAgilityChatContentSanitizer();
        var id = Guid.NewGuid();
        var result = sanitizer.Sanitize($"<a href='https://sja.local'>link</a><span class='ipop-mention' data-user-id='{id}'>@Alice</span>");
        result.Html.Should().Contain("href=\"https://sja.local\"").And.Contain("data-user-id");
        result.Plain.Should().Contain("link").And.Contain("@Alice");
    }
}
```

- [ ] Implement sanitizer:
```csharp
using Ganss.Xss;
using System.Net;
using System.Text.RegularExpressions;
using IPop.Modules.Chat.Application.Content;

namespace IPop.Infrastructure.Chat;

public sealed class HtmlAgilityChatContentSanitizer : IChatContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;
    public HtmlAgilityChatContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "b", "strong", "i", "em", "u", "a", "br", "p", "span", "ul", "ol", "li" }) _sanitizer.AllowedTags.Add(tag);
        _sanitizer.AllowedAttributes.Clear();
        foreach (var attr in new[] { "href", "class", "data-user-id" }) _sanitizer.AllowedAttributes.Add(attr);
        _sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto" }) _sanitizer.AllowedSchemes.Add(scheme);
    }
    public ChatContent Sanitize(string input)
    {
        var html = _sanitizer.Sanitize(input ?? string.Empty);
        var plain = Regex.Replace(html, "<[^>]+>", " ");
        plain = WebUtility.HtmlDecode(Regex.Replace(plain, "\\s+", " ")).Trim();
        if (plain.Length > 400) plain = plain[..400];
        return new ChatContent(html, plain);
    }
}
```

- [ ] Register DI:
```csharp
services.AddSingleton<IChatContentSanitizer, HtmlAgilityChatContentSanitizer>();
```

---

### Task 3: File storage + attachment endpoints

**Files:**
- Create: `src/IPop.Modules.Chat/Application/Attachments/IFileStorage.cs`
- Create: `src/IPop.Modules.Chat/Application/Attachments/FileStorageOptions.cs`
- Create: `src/IPop.Infrastructure/Chat/LocalFileStorage.cs`
- Modify: `src/IPop.Modules.Chat/ChatModule.cs`
- Modify: `src/IPop.Infrastructure/DependencyInjection.cs`
- Modify: `src/IPop.Host/appsettings.json`, `src/IPop.Host/appsettings.Development.json`
- Test: `tests/IPop.UnitTests/Modules.Chat/LocalFileStorageTests.cs`

- [ ] Create contracts:
```csharp
namespace IPop.Modules.Chat.Application.Attachments;
public interface IFileStorage { Task<StoredFile> StoreAsync(Stream content, string suggestedName, string contentType, CancellationToken cancellationToken); Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken); }
public sealed record StoredFile(string StoragePath, long SizeBytes);
public sealed class FileStorageOptions { public string Root { get; init; } = "./uploads"; public long MaxBytes { get; init; } = 26214400; public string[] AllowedMimeTypes { get; init; } = []; }
```

- [ ] Create `LocalFileStorageTests.cs` verifying path traversal stripped and file roundtrip.

- [ ] Implement `LocalFileStorage` with safe filename:
```csharp
// Use Path.GetFileName, replace invalid chars with '-', create chat/yyyy/MM, write .tmp then File.Move.
```

- [ ] Register options and storage:
```csharp
services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
services.AddScoped<IFileStorage, LocalFileStorage>();
```

- [ ] Add config:
```json
"FileStorage": { "Root": "./uploads", "MaxBytes": 26214400, "AllowedMimeTypes": ["image/png", "image/jpeg", "image/gif", "image/webp", "application/pdf", "application/zip", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "text/plain", "text/markdown"] }
```

- [ ] Extend `ChatModule.MapEndpoints`:
```csharp
endpoints.MapPost("/api/chat/attachments", async (HttpContext http, IFileStorage storage, CancellationToken ct) => { /* validate form file size/mime, store, return metadata */ }).RequireAuthorization(AuthorizationPolicies.RequireAuthenticated).DisableAntiforgery();
endpoints.MapGet("/api/chat/attachments/{id:guid}", async (Guid id, HttpContext http, IChatRepository repo, IFileStorage storage, CancellationToken ct) => { /* verify authz sender/recipient, return Results.File */ }).RequireAuthorization(AuthorizationPolicies.RequireAuthenticated);
```

---

### Task 4: EF schema + migration

**Files:**
- Modify: `src/IPop.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/IPop.Infrastructure/Persistence/Configurations/DirectAttachmentConfiguration.cs`
- Modify: `src/IPop.Infrastructure/Persistence/Configurations/DirectMessageConfiguration.cs`
- Modify: `src/IPop.Infrastructure/Chat/EfChatRepository.cs`

- [ ] Add `DbSet<DirectAttachment> DirectAttachments => Set<DirectAttachment>();`.
- [ ] Configure `DirectAttachment` table with nullable `MessageId`, required `SenderUserId`, indexes `MessageId` and `(SenderUserId, UploadedAt)`.
- [ ] Update `DirectMessageConfiguration`: `Body` max 8000, `BodyPlain` max 400, `MessageType`, `DeliveredAt`, `ReadAt`, relationship to attachments.
- [ ] Generate migration:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet ef migrations add AddChatRichAttachmentPresenceSchema --project src/IPop.Infrastructure/IPop.Infrastructure.csproj --startup-project src/IPop.Host/IPop.Host.csproj --context AppDbContext
```
- [ ] Apply migration:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet ef database update --project src/IPop.Infrastructure/IPop.Infrastructure.csproj --startup-project src/IPop.Host/IPop.Host.csproj --context AppDbContext
```

---

### Task 5: Presence + read receipts

**Files:**
- Create: `src/IPop.Modules.Chat/Application/Presence/IPresenceService.cs`
- Create: `src/IPop.Infrastructure/Chat/RedisPresenceService.cs`
- Modify: `src/IPop.Modules.Chat/Presentation/ChatHub.cs`
- Modify: `src/IPop.Infrastructure/Chat/EfChatRepository.cs`

- [ ] Create `PresenceState` enum and `IPresenceService` with `SetAsync`, `GetManyAsync`, `ClearAsync`.
- [ ] Implement Redis keys `presence:{userId}` with EX 90s; swallow Redis errors and return offline.
- [ ] Extend `ChatHub`:
```csharp
public override async Task OnConnectedAsync() { await presence.SetAsync(userId, PresenceState.Online, ct); ... }
public override async Task OnDisconnectedAsync(Exception? ex) { await presence.ClearAsync(userId, ct); ... }
public Task Heartbeat(PresenceState state) => presence.SetAsync(GetCurrentUserId(), state, CancellationToken.None);
public Task MarkRead(Guid[] messageIds) => mediator.Send(new MarkMessagesReadCommand(GetCurrentUserId(), messageIds));
```
- [ ] Add repo methods `MarkDeliveredAsync`, `MarkReadAsync`, `GetUnreadMessageIdsAsync`.
- [ ] Hub emits `MessageDelivered` and `MessagesRead` events after updates.

---

### Task 6: UI component split + dark mode + profile pane

**Files:**
- Create: `src/IPop.Modules.Chat/Presentation/ChatComposer.razor`
- Create: `src/IPop.Modules.Chat/Presentation/ChatThreadStream.razor`
- Create: `src/IPop.Modules.Chat/Presentation/ChatProfilePane.razor`
- Create: `src/IPop.Modules.Chat/Presentation/ChatPresenceDot.razor`
- Create: `src/IPop.Modules.Chat/Presentation/ChatThemeToggle.razor`
- Modify: `src/IPop.Modules.Chat/Presentation/ChatsPage.razor`
- Modify: `src/IPop.Host/wwwroot/css/chat.css`

- [ ] Split current `ChatsPage.razor` into components above.
- [ ] Composer uses contenteditable toolbar: B/I/U/link/@/emoji/attach; upload via `/api/chat/attachments`.
- [ ] Thread stream renders sanitized rich HTML with `MarkupString`, attachments preview, receipt ticks.
- [ ] Profile pane shows: avatar, display name, department/position/location, extension + NIK, actions Message/Email only, Photos grid, Files list, Links list, Shortcuts.
- [ ] Add light/dark CSS variables and `ChatThemeToggle` writes `localStorage.ipopTheme` + toggles `data-theme` on app root/body.
- [ ] Search uses enterprise SVG magnifier, not emoji.
- [ ] Show extension next to user name in sidebar and thread header.

---

### Task 7: Verification

- [ ] Run format:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet format --verify-no-changes
```
- [ ] Run build:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet build
```
- [ ] Run tests:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet test --nologo
```
- [ ] Run app:
```bash
cd "d:/IPop Project/IPop" && rtk dotnet run --project src/IPop.Host/IPop.Host.csproj
```
- [ ] Manual smoke: login two browsers, send rich message, upload image/PDF, verify profile pane media/files/links, presence dot, receipts, dark mode.
- [ ] Run `code-reviewer` and `security-reviewer` agents; fix critical/high findings.

---

## Self-Review

Coverage: Rich text, emoji, mention, file storage Synology-compatible, presence, read receipt, profile pane, extension display, enterprise search icon, dark mode all covered.

Known implementation choice: plan compresses some endpoint/repo implementation into task-level descriptions to avoid one giant brittle code dump; execution must keep tests green after each task.
