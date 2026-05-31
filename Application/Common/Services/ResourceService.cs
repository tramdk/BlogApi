using FloraCore.Application.Common.Interfaces;
using System;
using System.Resources;

namespace FloraCore.Application.Common.Services;

/// <summary>
/// Implements <see cref="IResourceManager"/> using System.Resources.ResourceManager.
/// </summary>
public class ResourceService : IResourceManager
{
    private readonly ResourceManager _resourceManager = new(
        "FloraCore.Application.Common.Resources.ErrorMessages",
        typeof(ResourceService).Assembly);

    /// <inheritdoc />
    public string GetString(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _resourceManager.GetString(name) ?? name;
    }
}
