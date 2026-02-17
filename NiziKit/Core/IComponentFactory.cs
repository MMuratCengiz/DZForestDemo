using System.Text.Json;
using NiziKit.Components;

namespace NiziKit.Core;

/// <summary>
/// Factory interface for creating components from JSON properties.
/// </summary>
public interface IComponentFactory
{
    /// <summary>
    /// The type name used in JSON to identify this component (e.g., "mesh", "material", "rigidbody").
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Creates a component instance from JSON properties.
    /// </summary>
    /// <param name="properties">The JSON properties dictionary from the component definition.</param>
    /// <param name="resolver">The asset resolver for resolving asset references.</param>
    /// <returns>The created component instance.</returns>
    NiziComponent Create(IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver);
}
