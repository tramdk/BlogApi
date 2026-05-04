using NetArchTest.Rules;
using Xunit;

namespace BlogApi.Tests.ArchitectureTests;

public class DependencyTests
{
    private const string DomainNamespace = "BlogApi.Domain";
    private const string ApplicationNamespace = "BlogApi.Application";
    private const string InfrastructureNamespace = "BlogApi.Infrastructure";
    private const string PresentationNamespace = "BlogApi.Controllers"; // Specific to Controllers

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Other_Namespaces()
    {
        var assembly = typeof(Program).Assembly;
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, PresentationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain has dependencies on: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var assembly = typeof(Program).Assembly;
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Application has dependencies on: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Not_Have_Dependency_On_Presentation()
    {
        var assembly = typeof(Program).Assembly;
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(InfrastructureNamespace)
            .ShouldNot()
            .HaveDependencyOn(PresentationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Infrastructure has dependencies on: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
