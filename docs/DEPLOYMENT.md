# Deployment Guide for Noundry.Hydro Applications 🚀

This guide covers deploying Noundry.Hydro applications to various hosting platforms.

## 🎯 **Quick Deployment Checklist**

- [ ] Update connection strings for production database
- [ ] Configure authentication providers  
- [ ] Set up HTTPS certificates
- [ ] Configure environment variables
- [ ] Set up logging and monitoring
- [ ] Test all functionality in staging environment
- [ ] Configure auto-scaling (if needed)
- [ ] Set up backup and disaster recovery

## ☁️ **Azure App Service Deployment**

### **1. Prepare for Azure**

**appsettings.Production.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Azure-SQL-Connection-String"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "NoundryHydro": {
    "DevelopmentMode": false,
    "EnableTuxedoIntegration": true,
    "Styling": {
      "PrimaryColor": "#6366F1",
      "SupportDarkMode": true
    }
  }
}
```

### **2. Azure CLI Deployment**
```bash
# Login to Azure
az login

# Create resource group
az group create --name noundry-hydro-rg --location "East US"

# Create App Service plan
az appservice plan create --name noundry-hydro-plan --resource-group noundry-hydro-rg --sku B1

# Create web app
az webapp create --resource-group noundry-hydro-rg --plan noundry-hydro-plan --name your-noundry-app --runtime "DOTNET:8.0"

# Deploy the application
dotnet publish -c Release
az webapp deployment source config-zip --resource-group noundry-hydro-rg --name your-noundry-app --src publish.zip
```

### **3. Configure Azure Services**

**Application Insights**:
```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

**Azure SQL Database**:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));
```

## 🐳 **Docker Deployment**

### **Dockerfile**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["examples/Noundry.Hydro.Demo/Noundry.Hydro.Demo.csproj", "examples/Noundry.Hydro.Demo/"]
COPY ["src/Noundry.Hydro/Noundry.Hydro.csproj", "src/Noundry.Hydro/"]
COPY ["src/Noundry.Hydro.Extensions/Noundry.Hydro.Extensions.csproj", "src/Noundry.Hydro.Extensions/"]
RUN dotnet restore "examples/Noundry.Hydro.Demo/Noundry.Hydro.Demo.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/examples/Noundry.Hydro.Demo"
RUN dotnet build "Noundry.Hydro.Demo.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Noundry.Hydro.Demo.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80
ENTRYPOINT ["dotnet", "Noundry.Hydro.Demo.dll"]
```

### **Docker Compose**
```yaml
version: '3.8'

services:
  noundry-hydro-app:
    build: .
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=db;Database=NoundryHydroDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;
    depends_on:
      - db
    networks:
      - noundry-network

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    networks:
      - noundry-network

volumes:
  sqlserver_data:

networks:
  noundry-network:
    driver: bridge
```

### **Build and Run**
```bash
# Build the image
docker build -t noundry-hydro .

# Run with Docker Compose
docker-compose up -d

# View logs
docker-compose logs -f noundry-hydro-app
```

## 🌐 **AWS Deployment**

### **AWS Elastic Beanstalk**

**1. Prepare deployment package**:
```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../noundry-hydro-deployment.zip .
```

**2. Deploy to Elastic Beanstalk**:
```bash
# Install EB CLI
pip install awsebcli

# Initialize EB application
eb init noundry-hydro-app --region us-east-1 --platform "64bit Amazon Linux 2 v2.2.0 running .NET Core"

# Create environment and deploy
eb create noundry-hydro-prod --instance-type t3.small
eb deploy
```

### **AWS ECS with Fargate**

**task-definition.json**:
```json
{
  "family": "noundry-hydro-task",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::ACCOUNT:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "noundry-hydro-container",
      "image": "your-account.dkr.ecr.region.amazonaws.com/noundry-hydro:latest",
      "portMappings": [
        {
          "containerPort": 80,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/noundry-hydro",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

## 🔧 **Environment Configuration**

### **Production Settings**

**appsettings.Production.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Production-Connection-String"
  },
  "NoundryHydro": {
    "DevelopmentMode": false,
    "EnableNoundryUI": true,
    "EnableNoundryTagHelpers": true,
    "IncludeTailwindCSS": true,
    "EnableTuxedoIntegration": true,
    "Styling": {
      "PrimaryColor": "#6366F1",
      "SupportDarkMode": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Noundry.Hydro": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### **Environment Variables**
```bash
# Database
CONNECTIONSTRINGS__DEFAULTCONNECTION="Your-Connection-String"

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80

# Noundry.Hydro
NOUNDRYHYDRO__DEVELOPMENTMODE=false
NOUNDRYHYDRO__STYLING__PRIMARYCOLOR="#6366F1"

# Authentication (if using external providers)
AUTHENTICATION__GOOGLE__CLIENTID="your-google-client-id"
AUTHENTICATION__GOOGLE__CLIENTSECRET="your-google-client-secret"
```

## 📊 **Monitoring & Logging**

### **Application Insights Integration**
```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry
builder.Services.AddSingleton<ITelemetryInitializer, NoundryTelemetryInitializer>();
```

### **Structured Logging**
```csharp
// In Program.cs
builder.Logging.AddJsonConsole();

// Custom logging
public class CustomerService : ICustomerService
{
    private readonly ILogger<CustomerService> _logger;

    public async Task CreateCustomerAsync(Customer customer)
    {
        _logger.LogInformation("Creating customer {Email} from {IPAddress}", 
            customer.Email, httpContext.Connection.RemoteIpAddress);
            
        // Implementation...
        
        _logger.LogInformation("Customer {CustomerId} created successfully", customer.Id);
    }
}
```

### **Health Checks**
```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>()
    .AddCheck<NoundryHydroHealthCheck>("noundry-hydro");

// Use health checks
app.MapHealthChecks("/health");
```

## 🔒 **Security Configuration**

### **HTTPS Configuration**
```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Configure HTTPS redirection
app.UseHttpsRedirection();
```

### **Authentication in Production**
```csharp
// Configure authentication providers
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    });
```

### **Security Headers**
```csharp
// Add security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    await next();
});
```

## ⚡ **Performance Optimization**

### **Production Optimizations**
```csharp
// In Program.cs
builder.Services.Configure<NoundryHydroOptions>(options =>
{
    options.DevelopmentMode = false;
    // Enable production optimizations
});

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add output caching
builder.Services.AddOutputCache();
```

### **Database Optimizations**
```csharp
// Connection pooling
builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, 
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Configure EF Core for production
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.EnableSensitiveDataLogging(false);
    options.EnableDetailedErrors(false);
});
```

## 🔄 **CI/CD Pipeline**

### **GitHub Actions**
```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore -c Release
      
    - name: Test
      run: dotnet test --no-build -c Release
      
    - name: Publish
      run: dotnet publish examples/Noundry.Hydro.Demo -c Release -o ./publish
      
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'your-noundry-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: './publish'
```

### **Azure DevOps Pipeline**
```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '8.0'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration)'

- task: DotNetCoreCLI@2
  displayName: 'Publish'
  inputs:
    command: 'publish'
    projects: 'examples/Noundry.Hydro.Demo/Noundry.Hydro.Demo.csproj'
    arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'

- task: AzureWebApp@1
  displayName: 'Deploy to Azure Web App'
  inputs:
    azureSubscription: 'your-azure-subscription'
    appName: 'your-noundry-app'
    package: '$(Build.ArtifactStagingDirectory)/**/*.zip'
```

## 🐧 **Linux/Ubuntu Deployment**

### **1. Install Prerequisites**
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Install Nginx
sudo apt install nginx -y

# Install PostgreSQL (optional)
sudo apt install postgresql postgresql-contrib -y
```

### **2. Deploy Application**
```bash
# Clone and build
git clone https://github.com/plsft/Noundry.Hydro.git
cd Noundry.Hydro
dotnet publish examples/Noundry.Hydro.Demo -c Release -o /var/www/noundry-hydro

# Set permissions
sudo chown -R www-data:www-data /var/www/noundry-hydro
sudo chmod -R 755 /var/www/noundry-hydro
```

### **3. Configure Nginx**
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### **4. Create Systemd Service**
```ini
[Unit]
Description=Noundry.Hydro Demo Application
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/noundry-hydro/Noundry.Hydro.Demo.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=noundry-hydro
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

```bash
# Enable and start service
sudo systemctl enable noundry-hydro
sudo systemctl start noundry-hydro
sudo systemctl status noundry-hydro
```

## 🔄 **Database Migration**

### **Entity Framework Migrations**
```bash
# Create migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### **Production Migration Script**
```csharp
// In Program.cs for production
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    if (app.Environment.IsProduction())
    {
        // Apply migrations
        await context.Database.MigrateAsync();
        
        // Seed initial data
        await SeedProductionDataAsync(context);
    }
}
```

## 📈 **Scaling Considerations**

### **Horizontal Scaling**
```csharp
// Configure for load balancing
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

// Add distributed caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Configure session state for scale-out
builder.Services.AddSession(options =>
{
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

### **Database Scaling**
```csharp
// Read/write splitting
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var writeConnectionString = builder.Configuration.GetConnectionString("WriteConnection");
    var readConnectionString = builder.Configuration.GetConnectionString("ReadConnection");
    
    options.UseSqlServer(writeConnectionString);
    // Configure read replicas with Tuxedo ORM
});
```

## 🔍 **Monitoring & Observability**

### **Application Monitoring**
```csharp
// Add telemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
```

### **Custom Metrics**
```csharp
public class CustomerService : ICustomerService
{
    private readonly IMetrics _metrics;
    
    public async Task CreateCustomerAsync(Customer customer)
    {
        using var activity = _metrics.StartActivity("customer.create");
        
        try
        {
            await SaveCustomerAsync(customer);
            _metrics.Counter("customers.created").Add(1);
        }
        catch (Exception ex)
        {
            _metrics.Counter("customers.create.errors").Add(1);
            throw;
        }
    }
}
```

## 🔐 **Production Security**

### **Security Checklist**
- [ ] HTTPS enforced with valid certificates
- [ ] Strong authentication policies configured
- [ ] Authorization policies properly implemented
- [ ] Input validation on all forms
- [ ] SQL injection protection enabled
- [ ] XSS protection configured
- [ ] CSRF tokens validated
- [ ] Security headers configured
- [ ] Sensitive data encrypted
- [ ] Audit logging enabled

### **Secrets Management**
```csharp
// Use Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    keyVaultEndpoint,
    new DefaultAzureCredential());

// Or use AWS Systems Manager
builder.Configuration.AddSystemsManager("/noundry-hydro/");
```

## 🚨 **Troubleshooting**

### **Common Issues**

1. **Application won't start**
   - Check connection strings
   - Verify .NET runtime is installed
   - Check file permissions

2. **Database connection errors**
   - Verify connection string format
   - Check firewall rules
   - Confirm database server is running

3. **Components not updating**
   - Check JavaScript console for errors
   - Verify Hydro scripts are loaded
   - Check network requests in browser dev tools

### **Debugging in Production**
```csharp
// Enable detailed errors (temporarily)
if (app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage(); // Remove after debugging
}

// Add custom error handling
app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error/{0}");
```

## 📞 **Support**

For deployment issues:
- **📚 Documentation**: [docs.noundry.dev](https://docs.noundry.dev)
- **💬 Discord**: [discord.gg/noundry](https://discord.gg/noundry)
- **📧 Email**: [support@noundry.dev](mailto:support@noundry.dev)

---

<div align="center">
  <strong>🚀 Deploy with confidence using Noundry.Hydro!</strong>
</div>