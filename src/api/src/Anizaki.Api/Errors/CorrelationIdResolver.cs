namespace Anizaki.Api.Errors;

public static class CorrelationIdResolver
{
    public static string Resolve(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("correlationId", out var queryValue) &&
            !string.IsNullOrWhiteSpace(queryValue))
        {
            return queryValue.ToString();
        }

        if (context.Request.Headers.TryGetValue("x-correlation-id", out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return context.TraceIdentifier;
    }
}
