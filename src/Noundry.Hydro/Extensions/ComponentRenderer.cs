using System.Text;
using System.Text.Json;

namespace Noundry.Hydro.Extensions;

/// <summary>
/// Default implementation of IComponentRenderer for Noundry UI components
/// </summary>
public class ComponentRenderer : IComponentRenderer
{
    public string RenderComponent(string componentName, object? parameters = null)
    {
        var parametersJson = parameters != null ? 
            JsonSerializer.Serialize(parameters, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) : 
            "{}";

        return componentName.ToLowerInvariant() switch
        {
            "noundry-alert" => RenderAlert(parameters),
            "noundry-button" => RenderButton(parameters),
            "noundry-form-group" => RenderFormGroup(parameters),
            "noundry-spinner" => RenderSpinner(parameters),
            _ => $"<div data-noundry-component=\"{componentName}\" data-noundry-params='{parametersJson}'></div>"
        };
    }

    private string RenderAlert(object? parameters)
    {
        if (parameters == null) return string.Empty;
        
        var props = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(parameters));

        var message = props?.GetValueOrDefault("message")?.ToString() ?? "";
        var type = props?.GetValueOrDefault("type")?.ToString() ?? "info";
        var dismissible = bool.Parse(props?.GetValueOrDefault("dismissible")?.ToString() ?? "true");

        var typeClasses = type switch
        {
            "success" => "bg-green-50 text-green-800 border-green-200",
            "error" => "bg-red-50 text-red-800 border-red-200",
            "warning" => "bg-yellow-50 text-yellow-800 border-yellow-200",
            _ => "bg-blue-50 text-blue-800 border-blue-200"
        };

        var dismissButton = dismissible ? 
            """<button type="button" class="ml-auto -mx-1.5 -my-1.5 bg-transparent text-gray-400 hover:text-gray-900 rounded-lg focus:ring-2 focus:ring-gray-300 p-1.5 hover:bg-gray-100 inline-flex h-8 w-8" onclick="this.parentElement.remove()">×</button>""" : 
            "";

        return $"""
            <div class="flex items-center p-4 mb-4 text-sm border rounded-lg {typeClasses}" role="alert">
                <span class="sr-only">{type} alert</span>
                <div class="flex-1">{message}</div>
                {dismissButton}
            </div>
            """;
    }

    private string RenderButton(object? parameters)
    {
        if (parameters == null) return string.Empty;

        var props = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(parameters));

        var text = props?.GetValueOrDefault("text")?.ToString() ?? "";
        var action = props?.GetValueOrDefault("action")?.ToString() ?? "";
        var variant = props?.GetValueOrDefault("variant")?.ToString() ?? "primary";
        var size = props?.GetValueOrDefault("size")?.ToString() ?? "md";

        var variantClasses = variant switch
        {
            "secondary" => "bg-gray-600 hover:bg-gray-700 text-white",
            "success" => "bg-green-600 hover:bg-green-700 text-white",
            "danger" => "bg-red-600 hover:bg-red-700 text-white",
            _ => "bg-blue-600 hover:bg-blue-700 text-white"
        };

        var sizeClasses = size switch
        {
            "sm" => "px-3 py-2 text-sm",
            "lg" => "px-6 py-3 text-lg",
            _ => "px-4 py-2 text-base"
        };

        var onClickAttribute = !string.IsNullOrEmpty(action) ? $"""on:click="{action}" """ : "";

        return $"""
            <button type="button" {onClickAttribute}class="inline-flex items-center justify-center font-medium rounded-lg focus:ring-4 focus:outline-none transition-colors {variantClasses} {sizeClasses}">
                {text}
            </button>
            """;
    }

    private string RenderFormGroup(object? parameters)
    {
        if (parameters == null) return string.Empty;

        var props = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(parameters));

        var label = props?.GetValueOrDefault("label")?.ToString() ?? "";
        var fieldName = props?.GetValueOrDefault("fieldName")?.ToString() ?? "";
        var fieldType = props?.GetValueOrDefault("fieldType")?.ToString() ?? "text";
        var required = bool.Parse(props?.GetValueOrDefault("required")?.ToString() ?? "false");

        var requiredAsterisk = required ? """<span class="text-red-500">*</span>""" : "";

        return $"""
            <div class="mb-4">
                <label for="{fieldName}" class="block mb-2 text-sm font-medium text-gray-900">
                    {label}{requiredAsterisk}
                </label>
                <input type="{fieldType}" id="{fieldName}" name="{fieldName}" 
                       class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5"
                       {(required ? "required" : "")} />
                <div class="text-red-500 text-sm mt-1" x-show="$errors['{fieldName}']" x-text="$errors['{fieldName}']"></div>
            </div>
            """;
    }

    private string RenderSpinner(object? parameters)
    {
        if (parameters == null) return string.Empty;

        var props = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(parameters));

        var size = props?.GetValueOrDefault("size")?.ToString() ?? "md";
        var color = props?.GetValueOrDefault("color")?.ToString() ?? "#3B82F6";

        var sizeClasses = size switch
        {
            "sm" => "w-4 h-4",
            "lg" => "w-8 h-8",
            _ => "w-6 h-6"
        };

        return $"""
            <div class="flex items-center justify-center">
                <svg class="animate-spin {sizeClasses}" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="{color}" d="m12 2a10 10 0 0 1 10 10h-2a8 8 0 0 0-8-8v-2z"></path>
                </svg>
            </div>
            """;
    }
}

/// <summary>
/// TagHelper context service implementation
/// </summary>
public class TagHelperContextService : ITagHelperContext
{
    private readonly IServiceProvider _serviceProvider;

    public TagHelperContextService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool IsAuthorized(string? policy = null)
    {
        // Implement authorization logic here
        // This would typically use IAuthorizationService
        return true; // Simplified for demo
    }

    public object GetValidationContext(string fieldName)
    {
        return new
        {
            isValid = true,
            errors = Array.Empty<string>(),
            fieldName
        };
    }
}

/// <summary>
/// Tuxedo ORM integration service implementation
/// </summary>
public class TuxedoHydroIntegration : ITuxedoHydroIntegration
{
    private readonly ITuxedoContext _tuxedoContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TuxedoHydroIntegration> _logger;

    public TuxedoHydroIntegration(
        ITuxedoContext tuxedoContext, 
        IServiceProvider serviceProvider, 
        ILogger<TuxedoHydroIntegration> logger)
    {
        _tuxedoContext = tuxedoContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<T?> BindQueryAsync<T>(object query)
    {
        try
        {
            _logger.LogInformation("Executing Tuxedo query of type {QueryType}", query.GetType().Name);
            
            // Use Tuxedo's high-performance query capabilities
            if (query is string sql)
            {
                var results = await _tuxedoContext.QueryAsync<T>(sql);
                return results.FirstOrDefault();
            }
            
            // Handle complex query objects
            var queryBuilder = _tuxedoContext.QueryBuilder<T>();
            // Apply query parameters from the query object
            
            var result = await queryBuilder.ExecuteAsync();
            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Tuxedo query");
            throw;
        }
    }

    public async Task<bool> ExecuteCommandAsync(object command)
    {
        try
        {
            _logger.LogInformation("Executing Tuxedo command of type {CommandType}", command.GetType().Name);
            
            if (command is string sql)
            {
                var rowsAffected = await _tuxedoContext.ExecuteAsync(sql);
                return rowsAffected > 0;
            }
            
            // Handle command objects
            // Implementation would depend on command type
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Tuxedo command");
            throw;
        }
    }

    public async Task<Dictionary<string, string[]>> ValidateEntityAsync<T>(T entity)
    {
        try
        {
            _logger.LogInformation("Validating entity of type {EntityType}", typeof(T).Name);
            
            var validationErrors = new Dictionary<string, string[]>();
            
            // Use Tuxedo's built-in validation features
            var isValid = await _tuxedoContext.ValidateAsync(entity);
            
            if (!isValid)
            {
                // Extract validation errors from Tuxedo validation result
                // This would be implemented based on Tuxedo's validation API
                validationErrors.Add("Entity", new[] { "Entity validation failed" });
            }
            
            return validationErrors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating entity");
            throw;
        }
    }
}