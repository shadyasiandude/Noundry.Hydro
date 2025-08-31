using Hydro;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Noundry.Hydro.Configuration;
using Guardian;
using Tuxedo;
using Assertive;

namespace Noundry.Hydro.Components;

/// <summary>
/// Enhanced Hydro component with Noundry ecosystem integration
/// </summary>
public abstract class NoundryHydroComponent : HydroComponent
{
    private NoundryHydroOptions? _options;
    private INoundryComponentContext? _componentContext;

    /// <summary>
    /// Noundry.Hydro configuration options
    /// </summary>
    protected NoundryHydroOptions Options => 
        _options ??= HttpContext.RequestServices.GetRequiredService<NoundryHydroOptions>();

    /// <summary>
    /// Noundry component context for ecosystem integration
    /// </summary>
    protected INoundryComponentContext ComponentContext =>
        _componentContext ??= HttpContext.RequestServices.GetRequiredService<INoundryComponentContext>();

    /// <summary>
    /// Guardian for secure input validation
    /// </summary>
    protected IGuardian Guard =>
        HttpContext.RequestServices.GetRequiredService<IGuardian>();

    /// <summary>
    /// Tuxedo database context for data operations
    /// </summary>
    protected ITuxedoContext TuxedoContext =>
        HttpContext.RequestServices.GetRequiredService<ITuxedoContext>();

    /// <summary>
    /// Renders a Noundry UI component
    /// </summary>
    /// <param name="componentName">The component name</param>
    /// <param name="parameters">Component parameters</param>
    /// <returns>Rendered component HTML</returns>
    protected string RenderNoundryComponent(string componentName, object? parameters = null)
    {
        if (!Options.EnableNoundryUI)
            return string.Empty;

        var renderer = HttpContext.RequestServices.GetService<IComponentRenderer>();
        return renderer?.RenderComponent(componentName, parameters) ?? string.Empty;
    }

    /// <summary>
    /// Creates a Noundry alert component
    /// </summary>
    /// <param name="message">Alert message</param>
    /// <param name="type">Alert type (success, error, warning, info)</param>
    /// <param name="dismissible">Whether the alert can be dismissed</param>
    /// <returns>Alert component HTML</returns>
    protected string Alert(string message, string type = "info", bool dismissible = true)
    {
        return RenderNoundryComponent("noundry-alert", new
        {
            message,
            type,
            dismissible
        });
    }

    /// <summary>
    /// Creates a Noundry button component
    /// </summary>
    /// <param name="text">Button text</param>
    /// <param name="action">Click action</param>
    /// <param name="variant">Button variant</param>
    /// <param name="size">Button size</param>
    /// <returns>Button component HTML</returns>
    protected string Button(string text, string? action = null, string variant = "primary", string size = "md")
    {
        return RenderNoundryComponent("noundry-button", new
        {
            text,
            action,
            variant,
            size
        });
    }

    /// <summary>
    /// Creates a Noundry form group component
    /// </summary>
    /// <param name="label">Field label</param>
    /// <param name="fieldName">Field name/id</param>
    /// <param name="fieldType">Input field type</param>
    /// <param name="required">Whether the field is required</param>
    /// <returns>Form group component HTML</returns>
    protected string FormGroup(string label, string fieldName, string fieldType = "text", bool required = false)
    {
        return RenderNoundryComponent("noundry-form-group", new
        {
            label,
            fieldName,
            fieldType,
            required
        });
    }

    /// <summary>
    /// Creates a loading spinner component
    /// </summary>
    /// <param name="size">Spinner size</param>
    /// <param name="color">Spinner color</param>
    /// <returns>Spinner component HTML</returns>
    protected string LoadingSpinner(string size = "md", string? color = null)
    {
        return RenderNoundryComponent("noundry-spinner", new
        {
            size,
            color = color ?? Options.Styling.PrimaryColor
        });
    }

    /// <summary>
    /// Dispatches a Noundry-specific event with enhanced payload
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="data">Event data</param>
    /// <param name="scope">Event scope</param>
    /// <param name="metadata">Additional metadata</param>
    protected void DispatchNoundryEvent<TEvent>(TEvent data, Scope scope = Scope.Parent, object? metadata = null)
    {
        var enhancedData = new
        {
            data,
            metadata,
            timestamp = DateTime.UtcNow,
            componentId = ComponentId,
            componentType = GetType().Name
        };

        Dispatch(enhancedData, scope);
    }

    /// <summary>
    /// Shows a toast notification
    /// </summary>
    /// <param name="message">Toast message</param>
    /// <param name="type">Toast type</param>
    /// <param name="duration">Display duration in milliseconds</param>
    protected void ShowToast(string message, string type = "info", int duration = 3000)
    {
        Client.ExecuteScript($@"
            if (window.noundryToast) {{
                window.noundryToast.show({{
                    message: '{message}',
                    type: '{type}',
                    duration: {duration}
                }});
            }}
        ");
    }

    /// <summary>
    /// Validates input using Guardian security checks
    /// </summary>
    /// <param name="value">Value to validate</param>
    /// <param name="parameterName">Parameter name for error context</param>
    protected void ValidateInput<T>(T value, string parameterName)
    {
        try
        {
            Guard.Against.Null(value, parameterName);
            
            if (value is string stringValue)
            {
                Guard.Against.NullOrWhiteSpace(stringValue, parameterName);
            }
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(parameterName, ex.Message);
            ShowToast($"Validation error: {ex.Message}", "error");
        }
    }

    /// <summary>
    /// Validates email format using Guardian
    /// </summary>
    /// <param name="email">Email to validate</param>
    /// <param name="parameterName">Parameter name</param>
    protected void ValidateEmail(string email, string parameterName = "Email")
    {
        try
        {
            Guard.Against.NullOrWhiteSpace(email, parameterName);
            Guard.Against.InvalidFormat(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", parameterName, "Invalid email format");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(parameterName, ex.Message);
            ShowToast("Invalid email format", "error");
        }
    }

    /// <summary>
    /// Validates numeric range using Guardian
    /// </summary>
    /// <param name="value">Numeric value</param>
    /// <param name="min">Minimum value</param>
    /// <param name="max">Maximum value</param>
    /// <param name="parameterName">Parameter name</param>
    protected void ValidateRange(decimal value, decimal min, decimal max, string parameterName)
    {
        try
        {
            Guard.Against.OutOfRange(value, parameterName, min, max);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            ModelState.AddModelError(parameterName, ex.Message);
            ShowToast($"Value must be between {min} and {max}", "error");
        }
    }

    /// <summary>
    /// Executes a Tuxedo query with error handling
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="sql">SQL query</param>
    /// <param name="parameters">Query parameters</param>
    /// <returns>Query results</returns>
    protected async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            Guard.Against.NullOrWhiteSpace(sql, nameof(sql));
            
            return await TuxedoContext.QueryAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            ShowToast("Database query failed", "error");
            ComponentContext.TrackComponentUsage(GetType().Name, "QueryError");
            throw;
        }
    }

    /// <summary>
    /// Executes a Tuxedo command with validation
    /// </summary>
    /// <param name="sql">SQL command</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Number of affected rows</returns>
    protected async Task<int> ExecuteCommandAsync(string sql, object? parameters = null)
    {
        try
        {
            Guard.Against.NullOrWhiteSpace(sql, nameof(sql));
            
            return await TuxedoContext.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            ShowToast("Database command failed", "error");
            ComponentContext.TrackComponentUsage(GetType().Name, "CommandError");
            throw;
        }
    }

    /// <summary>
    /// Validates the model with enhanced Noundry validation features
    /// </summary>
    /// <returns>True if valid</returns>
    public new bool Validate()
    {
        var isValid = base.Validate();

        // Add Noundry-specific validation enhancements
        if (!isValid && Options.DevelopmentMode)
        {
            ComponentContext.LogValidationErrors(ModelState);
        }

        return isValid;
    }
}