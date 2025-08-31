using Hydro.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Noundry.Hydro.Components;
using Noundry.Hydro.Extensions;

namespace Noundry.Hydro.Configuration;

/// <summary>
/// Noundry.Hydro extensions to IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures the complete Noundry.Hydro ecosystem
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">Configuration options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNoundryHydro(
        this IServiceCollection services, 
        Action<NoundryHydroOptions>? options = null)
    {
        var noundryOptions = new NoundryHydroOptions();
        options?.Invoke(noundryOptions);
        services.AddSingleton(noundryOptions);

        // Add core Hydro services
        services.AddHydro(hydroOptions =>
        {
            // Copy over base Hydro options
            hydroOptions.Antiforgery = noundryOptions.Antiforgery;
            hydroOptions.ValueMappers = noundryOptions.ValueMappers;
        });

        // Add Noundry UI components if enabled
        if (noundryOptions.EnableNoundryUI)
        {
            services.AddNoundryUI(uiOptions =>
            {
                uiOptions.IncludeAlpineJS = noundryOptions.IncludeAlpineJS;
                uiOptions.IncludeTailwindCSS = noundryOptions.IncludeTailwindCSS;
            });
        }

        // Add Noundry TagHelpers if enabled
        if (noundryOptions.EnableNoundryTagHelpers)
        {
            services.AddNoundryTagHelpers();
        }

        // Add Tuxedo ORM integration if enabled
        if (noundryOptions.EnableTuxedoIntegration)
        {
            services.AddTuxedo();
            services.AddBowtie(); // Add migration system
        }

        // Add Guardian for input validation
        services.AddSingleton<IGuardian, Guardian.Guardian>();

        // Add component services
        services.TryAddScoped<INoundryComponentContext, NoundryComponentContext>();
        services.TryAddScoped<IComponentRenderer, ComponentRenderer>();
        services.TryAddScoped<ITagHelperContext, TagHelperContextService>();
        services.TryAddScoped<ITuxedoHydroIntegration, TuxedoHydroIntegration>();

        return services;
    }
}
