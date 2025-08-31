using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Noundry.Hydro.Components;

/// <summary>
/// Context interface for Noundry component ecosystem integration
/// </summary>
public interface INoundryComponentContext
{
    /// <summary>
    /// Logs validation errors for debugging
    /// </summary>
    /// <param name="modelState">Model state dictionary</param>
    void LogValidationErrors(ModelStateDictionary modelState);

    /// <summary>
    /// Gets theme configuration
    /// </summary>
    /// <returns>Current theme settings</returns>
    object GetThemeConfig();

    /// <summary>
    /// Tracks component usage for analytics
    /// </summary>
    /// <param name="componentName">Component name</param>
    /// <param name="action">Action performed</param>
    void TrackComponentUsage(string componentName, string action);
}

/// <summary>
/// Default implementation of INoundryComponentContext
/// </summary>
public class NoundryComponentContext : INoundryComponentContext
{
    private readonly ILogger<NoundryComponentContext> _logger;

    public NoundryComponentContext(ILogger<NoundryComponentContext> logger)
    {
        _logger = logger;
    }

    public void LogValidationErrors(ModelStateDictionary modelState)
    {
        foreach (var kvp in modelState)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (value?.Errors.Count > 0)
            {
                foreach (var error in value.Errors)
                {
                    _logger.LogWarning("Validation error for {FieldName}: {ErrorMessage}", key, error.ErrorMessage);
                }
            }
        }
    }

    public object GetThemeConfig()
    {
        return new
        {
            darkMode = true,
            primaryColor = "#3B82F6",
            fontFamily = "Inter, system-ui, sans-serif",
            borderRadius = "0.375rem"
        };
    }

    public void TrackComponentUsage(string componentName, string action)
    {
        _logger.LogInformation("Component usage: {ComponentName} - {Action}", componentName, action);
    }
}