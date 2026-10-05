using Microsoft.AspNetCore.Authorization;
using BarberApp.API.Middleware;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BarberApp.API.OpenApi;

public sealed class BearerAuthorizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = endpointMetadata.OfType<IAuthorizeData>().Any();

        if (MercadoPagoCallbackEndpoint.IsCallback(
            "/" + context.ApiDescription.RelativePath, context.ApiDescription.HttpMethod))
        {
            // O serializador omite listas vazias; um requisito vazio permite acesso sem headers.
            operation.Security = [new OpenApiSecurityRequirement()];
            return;
        }

        if (allowsAnonymous || !requiresAuthorization)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [CreateSchemeReference("ApiKey")] = Array.Empty<string>(),
                [CreateSchemeReference("Bearer")] = Array.Empty<string>()
            }
        ];
    }

    private static OpenApiSecurityScheme CreateSchemeReference(string id)
    {
        return new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = id
            }
        };
    }
}
