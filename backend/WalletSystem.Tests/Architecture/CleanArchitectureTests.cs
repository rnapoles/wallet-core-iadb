using System.Reflection;
using WalletSystem.Api.Endpoints;
using WalletSystem.Application.Contracts.Services.Persistence;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Infrastructure.Persistence;
using Xunit;

namespace WalletSystem.Tests.Architecture;

public class CleanArchitectureTests
{
    private readonly Assembly _domainAssembly;
    private readonly Assembly _applicationAssembly;
    private readonly Assembly _infrastructureAssembly;
    private readonly Assembly _apiAssembly;

    public CleanArchitectureTests()
    {
        _domainAssembly = typeof(User).Assembly;
        _applicationAssembly = typeof(IApplicationDbContext).Assembly;
        _infrastructureAssembly = typeof(ApplicationDbContext).Assembly;
        _apiAssembly = typeof(AuthEndpoints).Assembly;
    }

    [Fact]
    public void Domain_ShouldNotDependOnApplication()
    {
        // Arrange
        var domainTypes = _domainAssembly.GetTypes();

        // Act & Assert
        foreach (var type in domainTypes)
        {
            // Check that no types from Application namespace appear in domain type signatures
            var typeName = type.FullName ?? string.Empty;
            var baseType = type.BaseType?.FullName ?? string.Empty;
            var interfaceNames = type.GetInterfaces().Select(i => i.FullName ?? string.Empty);

            Assert.DoesNotContain("WalletSystem.Application", typeName);
            Assert.DoesNotContain("WalletSystem.Application", baseType);
            foreach (var iface in interfaceNames)
            {
                Assert.DoesNotContain("WalletSystem.Application", iface);
            }
        }
    }

    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        var domainTypes = _domainAssembly.GetTypes();

        foreach (var type in domainTypes)
        {
            var typeName = type.FullName ?? string.Empty;
            var baseType = type.BaseType?.FullName ?? string.Empty;
            var interfaceNames = type.GetInterfaces().Select(i => i.FullName ?? string.Empty);

            Assert.DoesNotContain("WalletSystem.Infrastructure", typeName);
            Assert.DoesNotContain("WalletSystem.Infrastructure", baseType);
            foreach (var iface in interfaceNames)
            {
                Assert.DoesNotContain("WalletSystem.Infrastructure", iface);
            }
        }
    }

    [Fact]
    public void Domain_ShouldNotDependOnApi()
    {
        var domainTypes = _domainAssembly.GetTypes();

        foreach (var type in domainTypes)
        {
            var typeName = type.FullName ?? string.Empty;
            var baseType = type.BaseType?.FullName ?? string.Empty;
            var interfaceNames = type.GetInterfaces().Select(i => i.FullName ?? string.Empty);

            Assert.DoesNotContain("WalletSystem.Api", typeName);
            Assert.DoesNotContain("WalletSystem.Api", baseType);
            foreach (var iface in interfaceNames)
            {
                Assert.DoesNotContain("WalletSystem.Api", iface);
            }
        }
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        var applicationTypes = _applicationAssembly.GetTypes();

        foreach (var type in applicationTypes)
        {
            var typeName = type.FullName ?? string.Empty;
            var baseType = type.BaseType?.FullName ?? string.Empty;
            var interfaceNames = type.GetInterfaces().Select(i => i.FullName ?? string.Empty);
            var propertyTypes = type.GetProperties().Select(p => p.PropertyType.FullName ?? string.Empty);
            var methodReturnTypes = type.GetMethods().SelectMany(m =>
                new[] { m.ReturnType.FullName ?? string.Empty }
                    .Concat(m.GetParameters().Select(p => p.ParameterType.FullName ?? string.Empty)));

            Assert.DoesNotContain("WalletSystem.Infrastructure", typeName);
            Assert.DoesNotContain("WalletSystem.Infrastructure", baseType);
            foreach (var iface in interfaceNames)
            {
                Assert.DoesNotContain("WalletSystem.Infrastructure", iface);
            }
            foreach (var prop in propertyTypes)
            {
                Assert.DoesNotContain("WalletSystem.Infrastructure", prop);
            }
            foreach (var method in methodReturnTypes)
            {
                Assert.DoesNotContain("WalletSystem.Infrastructure", method);
            }
        }
    }

    [Fact]
    public void Application_ShouldNotDependOnApi()
    {
        var applicationTypes = _applicationAssembly.GetTypes();

        foreach (var type in applicationTypes)
        {
            var typeName = type.FullName ?? string.Empty;
            var baseType = type.BaseType?.FullName ?? string.Empty;
            var interfaceNames = type.GetInterfaces().Select(i => i.FullName ?? string.Empty);
            var propertyTypes = type.GetProperties().Select(p => p.PropertyType.FullName ?? string.Empty);
            var methodReturnTypes = type.GetMethods().SelectMany(m =>
                new[] { m.ReturnType.FullName ?? string.Empty }
                    .Concat(m.GetParameters().Select(p => p.ParameterType.FullName ?? string.Empty)));

            Assert.DoesNotContain("WalletSystem.Api", typeName);
            Assert.DoesNotContain("WalletSystem.Api", baseType);
            foreach (var iface in interfaceNames)
            {
                Assert.DoesNotContain("WalletSystem.Api", iface);
            }
            foreach (var prop in propertyTypes)
            {
                Assert.DoesNotContain("WalletSystem.Api", prop);
            }
            foreach (var method in methodReturnTypes)
            {
                Assert.DoesNotContain("WalletSystem.Api", method);
            }
        }
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        var infrastructureTypes = _infrastructureAssembly.GetTypes();

        foreach (var type in infrastructureTypes)
        {
            var typeName = type.FullName ?? string.Empty;
            var baseType = type.BaseType?.FullName ?? string.Empty;
            var interfaceNames = type.GetInterfaces().Select(i => i.FullName ?? string.Empty);
            var propertyTypes = type.GetProperties().Select(p => p.PropertyType.FullName ?? string.Empty);
            var methodReturnTypes = type.GetMethods().SelectMany(m =>
                new[] { m.ReturnType.FullName ?? string.Empty }
                    .Concat(m.GetParameters().Select(p => p.ParameterType.FullName ?? string.Empty)));

            Assert.DoesNotContain("WalletSystem.Api", typeName);
            Assert.DoesNotContain("WalletSystem.Api", baseType);
            foreach (var iface in interfaceNames)
            {
                Assert.DoesNotContain("WalletSystem.Api", iface);
            }
            foreach (var prop in propertyTypes)
            {
                Assert.DoesNotContain("WalletSystem.Api", prop);
            }
            foreach (var method in methodReturnTypes)
            {
                Assert.DoesNotContain("WalletSystem.Api", method);
            }
        }
    }

    [Fact]
    public void Domain_ShouldHaveNoExternalDependenciesExceptNetStandard()
    {
        var referencedAssemblies = _domainAssembly.GetReferencedAssemblies();

        foreach (var asm in referencedAssemblies)
        {
            // Domain can only depend on netstandard, System.*, Microsoft.* (basic libs), and itself
            var name = asm.Name;
            Assert.True(
                name!.StartsWith("System.") ||
                name.StartsWith("Microsoft.") ||
                name == "netstandard" ||
                name == "mscorlib" ||
                name == "System.Runtime" ||
                name == "System.Private.CoreLib" ||
                name == "WalletSystem.Domain",
                $"Domain layer should not reference {name}"
            );
        }
    }

    [Fact]
    public void Application_ShouldOnlyDependOnDomainAndBasicLibs()
    {
        var referencedAssemblies = _applicationAssembly.GetReferencedAssemblies();

        foreach (var asm in referencedAssemblies)
        {
            var name = asm.Name;
            Assert.True(
                name!.StartsWith("System.") ||
                name.StartsWith("Microsoft.") ||
                name == "netstandard" ||
                name == "mscorlib" ||
                name == "System.Runtime" ||
                name == "System.Private.CoreLib" ||
                name == "WalletSystem.Domain" ||
                name == "WalletSystem.Application" ||
                name == "MediatR" ||
                name == "MediatR.Contracts" ||
                name == "FluentValidation" ||
                name == "Microsoft.Extensions.Logging.Abstractions" ||
                name == "Microsoft.Extensions.DependencyInjection.Abstractions" ||
                name == "MassTransit.Abstractions" ||
                name == "MassTransit",
                $"Application layer should not reference {name}"
            );
        }
    }
}
