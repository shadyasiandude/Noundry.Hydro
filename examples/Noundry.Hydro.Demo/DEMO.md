# Noundry.Hydro Demo Application 🚀

This is a comprehensive demonstration application showcasing the complete **Noundry.Hydro ecosystem** in action. The demo illustrates how to build modern, reactive ASP.NET Core web applications using zero JavaScript complexity.

## 🌟 What This Demo Showcases

### Core Technologies
- **✅ Hydro Reactive Components** - Server-side reactivity with real-time updates
- **✅ Noundry.TagHelpers** - Enhanced TagHelpers with authorization and web APIs  
- **✅ Noundry.UI** - 44+ UI components with Tailwind CSS integration
- **✅ Tuxedo ORM** - High-performance ORM with LINQ expressions
- **✅ Alpine.js Integration** - Client-side DOM manipulation
- **✅ ASP.NET Core Identity** - Authentication and authorization

### Demo Features

#### 🔐 **Authentication System**
- **Login/Logout** functionality with reactive forms
- **Registration** with real-time validation
- **Role-based authorization** (Admin, Manager, Customer)
- **Demo credentials** for easy testing

#### 📊 **Interactive Dashboard**
- **Real-time statistics** cards with auto-refresh
- **Recent activities** feed
- **Quick action** buttons
- **Responsive design** for all devices

#### 👥 **Customer Management**
- **Customer CRUD** operations with validation
- **Search and pagination** with real-time filtering
- **Customer statistics** and analytics
- **Bulk operations** support

#### 📦 **Product Catalog**
- **Product management** with categories
- **Inventory tracking** with low-stock alerts
- **Image gallery** support
- **Featured products** showcase

#### 🛒 **Order Management**
- **Order processing** workflow
- **Status tracking** with visual indicators
- **Order items** management
- **Customer order** history

#### 📄 **Invoice System**
- **Invoice generation** from orders
- **Payment tracking** and processing
- **Overdue invoice** management
- **PDF generation** capability (demo)

## 🏃‍♂️ Quick Start

### 1. Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

### 2. Run the Demo
```bash
cd examples/Noundry.Hydro.Demo
dotnet restore
dotnet run
```

### 3. Access the Application
Navigate to `https://localhost:7000` and use the demo credentials:

| Role | Email | Password | Access Level |
|------|-------|----------|--------------|
| **Admin** | admin@noundry.demo | Admin123! | Full system access |
| **Manager** | manager@noundry.demo | Manager123! | Management features |
| **Customer** | customer@noundry.demo | Customer123! | Standard access |

## 🎯 Key Demo Scenarios

### Scenario 1: Customer Registration & Management
1. **Register a new account** using the registration form
2. **Explore customer management** features
3. **Search and filter** customers in real-time
4. **View customer details** and statistics

### Scenario 2: Product Catalog Management
1. **Browse the product catalog** with categories
2. **Add new products** with inventory tracking
3. **Monitor stock levels** and low-stock alerts
4. **Manage featured products** showcase

### Scenario 3: Order Processing Workflow
1. **Create new orders** for customers
2. **Add products** to orders with quantities
3. **Track order status** through the workflow
4. **Generate invoices** from completed orders

### Scenario 4: Dashboard Analytics
1. **View real-time statistics** on the dashboard
2. **Monitor recent activities** and system health
3. **Explore charts** and data visualizations
4. **Use quick actions** for common tasks

## 🔧 Technical Implementation Highlights

### Component Architecture
```csharp
// Enhanced base component with ecosystem integration
public class MyComponent : NoundryHydroComponent
{
    // Access to all Noundry services
    protected ICustomerService CustomerService => 
        HttpContext.RequestServices.GetRequiredService<ICustomerService>();
    
    // Real-time UI updates
    public async Task UpdateData()
    {
        var data = await CustomerService.GetCustomersAsync();
        ShowToast("Data updated!", "success");
        DispatchNoundryEvent(new DataRefreshed());
    }
}
```

### Service Integration
```csharp
// Tuxedo ORM integration
public class CustomerService : ICustomerService
{
    public async Task<List<Customer>> GetCustomersAsync()
    {
        // High-performance queries with LINQ
        return await _context.Customers
            .Where(c => c.IsActive)
            .Include(c => c.Orders)
            .OrderByDescending(c => c.DateJoined)
            .ToListAsync();
    }
}
```

### Real-time Reactivity
```razor
@model CustomersList

<!-- Reactive search with debouncing -->
<input bind="@Model.SearchTerm" 
       x-hydro-bind:input.debounce.500ms
       on:keyup.enter="@(() => Model.Search())" />

<!-- Real-time updates without page refresh -->
<button on:click="@(() => Model.LoadCustomers())">
    @if (Model.IsLoading)
    {
        @Model.LoadingSpinner("sm") Loading...
    }
    else
    {
        🔄 Refresh
    }
</button>
```

## 📁 Project Structure

```
Noundry.Hydro.Demo/
├── Areas/
│   └── Identity/           # Authentication pages
├── Data/
│   └── ApplicationDbContext.cs
├── Models/                 # Domain entities
│   ├── Customer.cs
│   ├── Product.cs
│   ├── Order.cs
│   └── Invoice.cs
├── Pages/
│   ├── Components/         # Hydro components
│   │   ├── Auth/          # Authentication components
│   │   ├── Dashboard/     # Dashboard components
│   │   └── Customers/     # Customer components
│   ├── Customers/         # Customer pages
│   ├── Products/          # Product pages
│   ├── Orders/            # Order pages
│   └── Invoices/          # Invoice pages
├── Services/              # Business logic
│   ├── CustomerService.cs
│   ├── ProductService.cs
│   ├── OrderService.cs
│   └── InvoiceService.cs
└── wwwroot/              # Static assets
```

## 🔍 Code Examples

### Creating a Reactive Form Component
```csharp
public class CustomerForm : NoundryHydroComponent
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    
    public bool IsLoading { get; set; }

    public async Task SaveCustomer()
    {
        if (!Validate()) return;
        
        IsLoading = true;
        try
        {
            await CustomerService.CreateCustomerAsync(new Customer { Email = Email });
            ShowToast("Customer created!", "success");
            DispatchNoundryEvent(new CustomerCreated());
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### Real-time Data Tables
```razor
@model CustomersList

<!-- Live search -->
<input bind="@Model.SearchTerm" 
       x-hydro-bind:input.debounce.300ms />

<!-- Reactive table -->
@foreach (var customer in Model.Customers)
{
    <tr class="hover:bg-gray-50" on:click="@(() => Model.ViewCustomer(customer.Id))">
        <td>@customer.FullName</td>
        <td>@customer.Email</td>
        <td>
            <span class="badge @customer.StatusBadgeClass">
                @(customer.IsActive ? "Active" : "Inactive")
            </span>
        </td>
    </tr>
}
```

### Event-Driven Communication
```csharp
// Component A dispatches event
DispatchNoundryEvent(new CustomerCreated { CustomerId = 123 }, Scope.Global);

// Component B subscribes to event
Subscribe<CustomerCreated>(async (customerCreated) => 
{
    await RefreshCustomerList();
    ShowToast("Customer list updated!", "info");
});
```

## 🚀 Performance Features

- **⚡ Real-time Updates** - Components update instantly without page refreshes
- **🔄 Auto-refresh** - Dashboard data updates automatically every minute
- **💾 Efficient Caching** - Smart caching reduces database calls
- **📱 Responsive Design** - Works perfectly on all device sizes
- **♿ Accessibility** - Built-in ARIA support and keyboard navigation

## 🎨 UI/UX Features

- **🎭 Component Library** - 44+ pre-built Noundry UI components
- **🌙 Dark Mode** - Automatic dark mode support
- **📱 Mobile First** - Responsive design for all devices
- **⚡ Loading States** - Beautiful loading indicators
- **🍞 Toast Notifications** - Real-time feedback system
- **🎯 Form Validation** - Client and server-side validation

## 🔒 Security Features

- **👤 Role-based Authorization** - Fine-grained access control
- **🔐 Secure Authentication** - ASP.NET Core Identity integration
- **🛡️ CSRF Protection** - Built-in anti-forgery tokens
- **📝 Audit Logging** - Comprehensive activity logging
- **🔍 Input Validation** - Multi-layer validation system

## 📊 Data Features

- **🗄️ Entity Framework Core** - Robust data access layer
- **🔄 Database Seeding** - Realistic sample data generation
- **📈 Analytics** - Real-time business analytics
- **💹 Reporting** - Comprehensive reporting system
- **🔍 Advanced Queries** - Complex data filtering and searching

## 🧪 Testing the Demo

### Manual Testing Checklist

- [ ] **Authentication Flow**
  - [ ] Login with demo credentials
  - [ ] Register new account
  - [ ] Role-based access control
  - [ ] Logout functionality

- [ ] **Dashboard Functionality**
  - [ ] Statistics cards display correctly
  - [ ] Auto-refresh works (wait 1 minute)
  - [ ] Quick actions navigate properly
  - [ ] Responsive design on mobile

- [ ] **Customer Management**
  - [ ] Search customers in real-time
  - [ ] Create new customer
  - [ ] Edit existing customer
  - [ ] Delete/deactivate customer
  - [ ] Pagination works correctly

- [ ] **Real-time Features**
  - [ ] Form validation updates live
  - [ ] Loading states show properly
  - [ ] Toast notifications appear
  - [ ] Component communication works

## 🔧 Customization

The demo is designed to be easily customizable:

### Theming
```csharp
builder.Services.AddNoundryHydro(options =>
{
    options.Styling.PrimaryColor = "#Your-Color";
    options.Styling.SupportDarkMode = true;
});
```

### Adding New Components
1. Create component class inheriting from `NoundryHydroComponent`
2. Create corresponding `.cshtml` view
3. Register component in DI if needed
4. Use in pages with `<your-component />`

### Database Configuration
```csharp
// Switch from In-Memory to SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

## 🚀 Production Deployment

### Steps for Production
1. **Update Connection String** - Switch to production database
2. **Configure Authentication** - Set up proper Identity providers
3. **Enable HTTPS** - Configure SSL certificates
4. **Add Logging** - Configure structured logging
5. **Performance Monitoring** - Add APM tools
6. **Security Hardening** - Review security settings

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Noundry.Hydro.Demo.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Noundry.Hydro.Demo.dll"]
```

## 📞 Support

For questions about this demo or the Noundry.Hydro ecosystem:

- **📚 Documentation**: [docs.noundry.dev](https://docs.noundry.dev)
- **💬 Discord**: [discord.gg/noundry](https://discord.gg/noundry)
- **🐛 Issues**: [GitHub Issues](https://github.com/plsft/Noundry.Hydro/issues)
- **📧 Email**: [support@noundry.dev](mailto:support@noundry.dev)

---

**This demo represents the future of ASP.NET Core development - reactive, component-driven, and incredibly fast to build!** 🎉