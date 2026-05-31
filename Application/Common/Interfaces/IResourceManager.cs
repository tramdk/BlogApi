namespace FloraCore.Application.Common.Interfaces;

/// <summary>
/// Defines a resource manager wrapper for localized error and log messages.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Gets the localized string resource.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <returns>The localized string.</returns>
    string GetString(string name);
}
