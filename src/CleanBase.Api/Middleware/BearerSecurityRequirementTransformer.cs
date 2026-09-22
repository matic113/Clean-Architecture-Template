using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CleanBase.Api.Middleware;

internal sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        // If the endpoint is marked as AllowAnonymous, do not add security requirements
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            return Task.CompletedTask;
        }

        // If no AllowAnonymous is found, assume it requires Bearer authentication
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference("Bearer", context.Document, null)
            ] = new List<string>()
        });

        return Task.CompletedTask;
    }
}