using Hydro.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Noundry.Hydro.Configuration;

/// <summary>
/// Configuration options for Noundry.Hydro ecosystem integration
/// </summary>
public class NoundryHydroOptions : HydroOptions
{
    /// <summary>
    /// Enable Noundry UI components integration
    /// </summary>
    public bool EnableNoundryUI { get; set; } = true;

    /// <summary>
    /// Enable Noundry TagHelpers integration
    /// </summary>
    public bool EnableNoundryTagHelpers { get; set; } = true;

    /// <summary>
    /// Include Tailwind CSS from Noundry UI
    /// </summary>
    public bool IncludeTailwindCSS { get; set; } = true;

    /// <summary>
    /// Include Alpine.js integration
    /// </summary>
    public bool IncludeAlpineJS { get; set; } = true;

    /// <summary>
    /// Enable Tuxedo ORM integration helpers
    /// </summary>
    public bool EnableTuxedoIntegration { get; set; } = true;

    /// <summary>
    /// Development mode settings
    /// </summary>
    public bool DevelopmentMode { get; set; } = false;

    /// <summary>
    /// Custom styling options
    /// </summary>
    public NoundryHydroStylingOptions Styling { get; set; } = new();
}

/// <summary>
/// Styling configuration options
/// </summary>
public class NoundryHydroStylingOptions
{
    /// <summary>
    /// Primary theme color
    /// </summary>
    public string PrimaryColor { get; set; } = "#3B82F6";

    /// <summary>
    /// Secondary theme color
    /// </summary>
    public string SecondaryColor { get; set; } = "#6B7280";

    /// <summary>
    /// Error color for validation and alerts
    /// </summary>
    public string ErrorColor { get; set; } = "#EF4444";

    /// <summary>
    /// Success color for confirmations
    /// </summary>
    public string SuccessColor { get; set; } = "#10B981";

    /// <summary>
    /// Warning color for cautions
    /// </summary>
    public string WarningColor { get; set; } = "#F59E0B";

    /// <summary>
    /// Dark mode support
    /// </summary>
    public bool SupportDarkMode { get; set; } = true;
}