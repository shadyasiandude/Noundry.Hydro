namespace Noundry.Hydro.Extensions;

/// <summary>
/// Interface for rendering Noundry UI components
/// </summary>
public interface IComponentRenderer
{
    /// <summary>
    /// Renders a component with the specified parameters
    /// </summary>
    /// <param name="componentName">Component name</param>
    /// <param name="parameters">Component parameters</param>
    /// <returns>Rendered HTML</returns>
    string RenderComponent(string componentName, object? parameters = null);
}

/// <summary>
/// Interface for TagHelper context integration
/// </summary>
public interface ITagHelperContext
{
    /// <summary>
    /// Gets authorization context for conditional rendering
    /// </summary>
    /// <param name="policy">Authorization policy</param>
    /// <returns>True if authorized</returns>
    bool IsAuthorized(string? policy = null);

    /// <summary>
    /// Gets form validation context
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>Validation state</returns>
    object GetValidationContext(string fieldName);
}

/// <summary>
/// Interface for Tuxedo ORM integration with Hydro components
/// </summary>
public interface ITuxedoHydroIntegration
{
    /// <summary>
    /// Binds a Tuxedo query result to a Hydro component
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">Tuxedo query</param>
    /// <returns>Bound data for component</returns>
    Task<T?> BindQueryAsync<T>(object query);

    /// <summary>
    /// Executes a Tuxedo command from a Hydro component
    /// </summary>
    /// <param name="command">Tuxedo command</param>
    /// <returns>Execution result</returns>
    Task<bool> ExecuteCommandAsync(object command);

    /// <summary>
    /// Validates entity using Tuxedo validation rules
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="entity">Entity to validate</param>
    /// <returns>Validation results</returns>
    Task<Dictionary<string, string[]>> ValidateEntityAsync<T>(T entity);
}