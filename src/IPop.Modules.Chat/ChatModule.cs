using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Attachments;
using IPop.Modules.Chat.Presentation;
using IPop.Shared.Abstractions;
using IPop.Shared.Auth;

namespace IPop.Modules.Chat;

public sealed class ChatModule : IModule
{
    public string Name => "Chat";
    public string Version => "0.2.0";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ChatModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(ChatModule).Assembly);
        services.AddSignalR();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ChatHub>("/hubs/chat").RequireAuthorization(AuthorizationPolicies.RequireAuthenticated);
        endpoints.MapHub<ChannelHub>("/hubs/channels").RequireAuthorization(AuthorizationPolicies.RequireAuthenticated);

        ChannelEndpoints.Map(endpoints);

        endpoints.MapPost("/api/chat/attachments", async (
                HttpContext httpContext,
                IFileStorage fileStorage,
                IChatRepository chatRepository,
                IOptions<FileStorageOptions> options,
                CancellationToken cancellationToken) =>
            {
                if (!ChatCsrfGuard.IsRequestSameOrigin(httpContext))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                if (!ChatCsrfGuard.HasIPopRequestHeader(httpContext))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var userIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdValue, out var currentUserId))
                {
                    return Results.Unauthorized();
                }

                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");
                if (file is null || file.Length <= 0)
                {
                    return Results.BadRequest(new { error = "File is required." });
                }

                var fileStorageOptions = options.Value;
                if (file.Length > fileStorageOptions.MaxBytes)
                {
                    return Results.BadRequest(new { error = "File is too large." });
                }

                if (!fileStorageOptions.AllowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "File type is not allowed." });
                }

                await using var stream = file.OpenReadStream();
                var stored = await fileStorage.StoreAsync(stream, file.FileName, file.ContentType, cancellationToken);
                var attachment = await chatRepository.StageAttachmentAsync(currentUserId, file.FileName, stored.StoragePath, stored.SizeBytes, file.ContentType, cancellationToken);

                return Results.Ok(new
                {
                    attachmentId = attachment.Id,
                    previewUrl = attachment.DownloadUrl,
                    originalName = attachment.OriginalName,
                    sizeBytes = attachment.SizeBytes,
                    contentType = attachment.ContentType
                });
            })
            .RequireAuthorization(AuthorizationPolicies.RequireAuthenticated)
            .DisableAntiforgery();

        endpoints.MapGet("/api/chat/attachments/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                IChatRepository chatRepository,
                IFileStorage fileStorage,
                CancellationToken cancellationToken) =>
            {
                var userIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdValue, out var currentUserId))
                {
                    return Results.Unauthorized();
                }

                var attachment = await chatRepository.FindAttachmentForDownloadAsync(id, currentUserId, cancellationToken);
                if (attachment is null)
                {
                    return Results.NotFound();
                }

                var stream = await fileStorage.OpenReadAsync(attachment.StoragePath, cancellationToken);
                httpContext.Response.Headers.XContentTypeOptions = "nosniff";
                return Results.File(stream, attachment.ContentType, attachment.OriginalName);
            })
            .RequireAuthorization(AuthorizationPolicies.RequireAuthenticated);
    }
}
