using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppointmentBook.Api.Filters;

/// <summary>Rejects requests that do not carry the configured X-Admin-Key header (deny by default).</summary>
public sealed class AdminKeyFilter(IConfiguration configuration) : IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expectedKey = configuration["Admin:ApiKey"];
        var suppliedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        var isAuthorised = !string.IsNullOrEmpty(expectedKey)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedKey), Encoding.UTF8.GetBytes(suppliedKey));

        if (!isAuthorised)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
