namespace Anizaki.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found.
/// Maps to HTTP 404 Not Found at the API boundary.
/// </summary>
public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string resourceType, string identifier)
        : base($"{resourceType} '{identifier}' was not found.")
    {
        ResourceType = resourceType;
        Identifier   = identifier;
    }

    public string ResourceType { get; }
    public string Identifier   { get; }
}
