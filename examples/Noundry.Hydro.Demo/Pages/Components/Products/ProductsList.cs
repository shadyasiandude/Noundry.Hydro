using Noundry.Hydro.Components;
using Noundry.Hydro.Demo.Models;
using Noundry.Hydro.Demo.Services;

namespace Noundry.Hydro.Demo.Pages.Components.Products;

public class ProductsList : NoundryHydroComponent
{
    private readonly IProductService _productService;

    public ProductsList(IProductService productService)
    {
        _productService = productService;
    }

    public List<Product> Products { get; set; } = new();
    public string SearchTerm { get; set; } = "";
    public ProductCategory? SelectedCategory { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool IsLoading { get; set; } = true;
    public bool ShowCreateForm { get; set; } = false;
    public string ViewMode { get; set; } = "grid"; // grid or list
    public string SortBy { get; set; } = "name";

    protected override async Task MountAsync()
    {
        await LoadProducts();
        
        // Subscribe to product events
        Subscribe<ProductCreated>(async _ => await LoadProducts());
        Subscribe<ProductUpdated>(async _ => await LoadProducts());
        Subscribe<ProductDeleted>(async _ => await LoadProducts());
        Subscribe<StockUpdated>(async _ => await LoadProducts());
    }

    public async Task LoadProducts()
    {
        IsLoading = true;
        
        try
        {
            var productsTask = _productService.GetProductsAsync(SearchTerm, SelectedCategory, CurrentPage, PageSize);
            var countTask = _productService.GetProductCountAsync(SearchTerm, SelectedCategory);
            
            await Task.WhenAll(productsTask, countTask);
            
            Products = await productsTask;
            TotalCount = await countTask;
            
            ComponentContext.TrackComponentUsage("ProductsList", "DataLoaded");
        }
        catch (Exception ex)
        {
            ShowToast("Failed to load products", "error");
            ComponentContext.TrackComponentUsage("ProductsList", "LoadError");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task Search()
    {
        CurrentPage = 1;
        ShowToast($"Searching products: {SearchTerm}", "info");
        await LoadProducts();
        ComponentContext.TrackComponentUsage("ProductsList", "Search");
    }

    public async Task FilterByCategory(ProductCategory? category)
    {
        SelectedCategory = category;
        CurrentPage = 1;
        await LoadProducts();
        
        var categoryName = category?.ToString() ?? "All Categories";
        ShowToast($"Filtered by: {categoryName}", "info");
        ComponentContext.TrackComponentUsage("ProductsList", $"FilterCategory-{categoryName}");
    }

    public async Task ClearFilters()
    {
        SearchTerm = "";
        SelectedCategory = null;
        CurrentPage = 1;
        await LoadProducts();
        ShowToast("Filters cleared", "info");
        ComponentContext.TrackComponentUsage("ProductsList", "ClearFilters");
    }

    public void ToggleViewMode()
    {
        ViewMode = ViewMode == "grid" ? "list" : "grid";
        ShowToast($"Switched to {ViewMode} view", "info");
        ComponentContext.TrackComponentUsage("ProductsList", $"ViewMode-{ViewMode}");
    }

    public async Task SortProducts(string sortBy)
    {
        SortBy = sortBy;
        await LoadProducts();
        ShowToast($"Sorted by {sortBy}", "info");
        ComponentContext.TrackComponentUsage("ProductsList", $"Sort-{sortBy}");
    }

    public void ViewProduct(int productId)
    {
        Location($"/Products/{productId}");
        ComponentContext.TrackComponentUsage("ProductsList", $"ViewProduct-{productId}");
    }

    public void EditProduct(int productId)
    {
        Location($"/Products/Edit/{productId}");
        ComponentContext.TrackComponentUsage("ProductsList", $"EditProduct-{productId}");
    }

    public async Task UpdateStock(int productId, int newStock)
    {
        try
        {
            var result = await _productService.UpdateStockAsync(productId, newStock);
            if (result)
            {
                ShowToast("Stock updated successfully", "success");
                DispatchNoundryEvent(new StockUpdated 
                { 
                    ProductId = productId, 
                    NewStock = newStock 
                }, Scope.Global);
                
                await LoadProducts();
            }
            else
            {
                ShowToast("Failed to update stock", "error");
            }
        }
        catch (Exception)
        {
            ShowToast("Error updating stock", "error");
        }
    }

    public async Task ToggleFeatured(int productId)
    {
        var product = Products.FirstOrDefault(p => p.Id == productId);
        if (product != null)
        {
            product.IsFeatured = !product.IsFeatured;
            await _productService.UpdateProductAsync(product);
            
            ShowToast($"Product {(product.IsFeatured ? "added to" : "removed from")} featured", "success");
            ComponentContext.TrackComponentUsage("ProductsList", $"ToggleFeatured-{productId}");
        }
    }

    public async Task GoToPage(int page)
    {
        if (page < 1 || page > TotalPages) return;
        
        CurrentPage = page;
        await LoadProducts();
        ComponentContext.TrackComponentUsage("ProductsList", $"PageNavigation-{page}");
    }

    public void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        ComponentContext.TrackComponentUsage("ProductsList", "ToggleCreateForm");
    }
}

public record ProductCreated(int ProductId, string ProductName);
public record ProductUpdated(int ProductId, string ProductName);
public record ProductDeleted(int ProductId, string ProductName);
public record StockUpdated(int ProductId, int NewStock);