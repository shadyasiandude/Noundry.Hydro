using Hydro;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Noundry.Hydro.Configuration;

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