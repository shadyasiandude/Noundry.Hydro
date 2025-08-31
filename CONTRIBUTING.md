# Contributing to Noundry.Hydro 🤝

Thank you for your interest in contributing to **Noundry.Hydro**! We welcome contributions from the community and are excited to see what you'll build.

## 🌟 **How to Contribute**

### **Types of Contributions**
- 🐛 **Bug Reports** - Help us identify and fix issues
- ✨ **Feature Requests** - Suggest new features or improvements
- 📝 **Documentation** - Improve guides, examples, and API docs
- 💻 **Code Contributions** - Bug fixes, new features, optimizations
- 🎨 **UI/UX Improvements** - Better designs and user experiences
- 🧪 **Testing** - Add tests, improve test coverage
- 📖 **Examples** - Share real-world usage examples

## 🚀 **Getting Started**

### **1. Fork & Clone**
```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/YOUR-USERNAME/Noundry.Hydro.git
cd Noundry.Hydro

# Add upstream remote
git remote add upstream https://github.com/plsft/Noundry.Hydro.git
```

### **2. Set Up Development Environment**
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the demo application
cd examples/Noundry.Hydro.Demo
dotnet run
```

### **3. Create a Feature Branch**
```bash
# Create and switch to a new branch
git checkout -b feature/your-amazing-feature

# Or for bug fixes
git checkout -b fix/issue-description
```

## 📋 **Development Guidelines**

### **Code Style**
- Follow **C# coding conventions** and naming guidelines
- Use **meaningful variable and method names**
- Add **XML documentation** for public APIs
- Keep methods **focused and single-purpose**
- Follow **SOLID principles**

### **Component Development**
```csharp
// ✅ Good component example
public class CustomerList : NoundryHydroComponent
{
    /// <summary>
    /// List of customers to display
    /// </summary>
    public List<Customer> Customers { get; set; } = new();
    
    /// <summary>
    /// Loads customer data asynchronously
    /// </summary>
    public async Task LoadCustomers()
    {
        // Implementation
    }
}
```

### **Naming Conventions**
- **Components**: PascalCase (e.g., `CustomerForm`, `ProductList`)
- **Properties**: PascalCase (e.g., `FirstName`, `IsActive`)
- **Methods**: PascalCase (e.g., `LoadData`, `SaveCustomer`)
- **Events**: PascalCase records (e.g., `CustomerCreated`, `OrderUpdated`)
- **Files**: Match class names (e.g., `CustomerForm.cs`, `CustomerForm.cshtml`)

### **Testing Requirements**
- Add **unit tests** for new components
- Include **integration tests** for complex workflows
- Test **error scenarios** and edge cases
- Maintain **test coverage** above 80%

```csharp
// Example test structure
[TestClass]
public class CustomerFormTests
{
    [TestMethod]
    public async Task SaveCustomer_WithValidData_ShouldSucceed()
    {
        // Arrange
        var component = CreateComponent();
        component.Email = "test@example.com";
        
        // Act
        await component.SaveCustomer();
        
        // Assert
        Assert.IsTrue(component.IsValid);
    }
}
```

## 🐛 **Reporting Issues**

### **Before Reporting**
1. **Search existing issues** to avoid duplicates
2. **Test with the latest version** of Noundry.Hydro
3. **Check the documentation** for known limitations
4. **Try the demo application** to reproduce the issue

### **Issue Template**
```markdown
## 🐛 Bug Report

### Description
A clear description of what the bug is.

### Steps to Reproduce
1. Go to '...'
2. Click on '...'
3. See error

### Expected Behavior
What you expected to happen.

### Actual Behavior
What actually happened.

### Environment
- **Noundry.Hydro Version**: 1.0.0
- **OS**: Windows 11 / macOS 13 / Ubuntu 22.04
- **.NET Version**: 8.0
- **Browser**: Chrome 118, Firefox 119, etc.

### Additional Context
Add any other context about the problem here.
```

## ✨ **Feature Requests**

### **Feature Request Template**
```markdown
## ✨ Feature Request

### Problem Statement
Describe the problem this feature would solve.

### Proposed Solution
Describe your proposed solution.

### Alternative Solutions
Describe any alternative solutions you've considered.

### Additional Context
Add any other context or screenshots about the feature request.

### Implementation Ideas
If you have ideas about how to implement this, please share them.
```

## 🔀 **Pull Request Process**

### **1. Prepare Your Changes**
```bash
# Make sure you're on the latest main branch
git checkout main
git pull upstream main

# Create your feature branch
git checkout -b feature/your-feature

# Make your changes
# ... code, code, code ...

# Commit your changes
git add .
git commit -m "feat: add amazing new feature

- Implemented XYZ functionality
- Added tests for ABC
- Updated documentation"
```

### **2. Submit Pull Request**
1. **Push your branch** to your fork
2. **Create a Pull Request** on GitHub
3. **Fill out the PR template** completely
4. **Link any related issues**
5. **Request review** from maintainers

### **PR Template**
```markdown
## 📝 Pull Request

### Description
Brief description of changes made.

### Type of Change
- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)  
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📝 Documentation update

### Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] Demo application tested

### Checklist
- [ ] Code follows the project's coding standards
- [ ] Self-review of code completed
- [ ] Code is commented, particularly in hard-to-understand areas
- [ ] Corresponding changes to documentation made
- [ ] Changes generate no new warnings
- [ ] Tests pass locally
```

## 🎯 **Development Focus Areas**

We're particularly interested in contributions in these areas:

### **🔥 High Priority**
- 🐛 **Bug fixes** and stability improvements
- 📚 **Documentation** improvements and examples
- 🧪 **Test coverage** expansion
- 🎨 **UI component** enhancements
- ⚡ **Performance** optimizations

### **🚀 Future Enhancements**
- 📱 **Mobile app integration** templates
- 🌐 **Internationalization** (i18n) support
- 📊 **Advanced charting** components
- 🔌 **Plugin architecture** for extensibility
- ☁️ **Cloud deployment** templates

## 💬 **Communication**

### **Getting Help**
- **💬 Discord**: Join our [Discord server](https://discord.gg/noundry) for real-time help
- **📧 Email**: Reach out to [support@noundry.dev](mailto:support@noundry.dev)
- **🐛 Issues**: Use GitHub Issues for bug reports and feature requests

### **Asking Questions**
- **Check existing discussions** first
- **Be specific** about your use case
- **Provide code examples** when possible
- **Share error messages** and stack traces
- **Mention your environment** details

## 🏆 **Recognition**

Contributors will be recognized in:
- **📝 Release notes** for significant contributions
- **👥 Contributors list** in the README
- **🎖️ GitHub contributor insights**
- **📢 Social media** shout-outs for major features

## 📜 **Code of Conduct**

We are committed to fostering a welcoming community. Please read and follow our [Code of Conduct](CODE_OF_CONDUCT.md).

### **Our Pledge**
- **Be respectful** and inclusive
- **Be collaborative** and helpful
- **Be patient** with newcomers
- **Be constructive** in feedback
- **Be professional** in all interactions

## 🎉 **Thank You!**

Every contribution, no matter how small, makes Noundry.Hydro better for everyone. Thank you for taking the time to contribute!

---

<div align="center">
  <strong>Happy coding! 🚀</strong><br>
  <em>The Noundry.Hydro Team</em>
</div>