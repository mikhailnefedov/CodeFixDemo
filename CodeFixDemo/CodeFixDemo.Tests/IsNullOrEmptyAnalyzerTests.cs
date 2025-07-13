using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace CodeFixDemo.Tests;

public class IsNullOrEmptyAnalyzerTests
{
    [Fact]
    public async Task Test()
    {
        // Arrange
        const string code = @"
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

public class Examples
{
    public void Example1() {
        IEnumerable<int> myEnumerable = new[] { 1, 2, 3, 4};
        myEnumerable.IsNullOrEmpty();
    }
}
";

        var analyzerTest = new CSharpAnalyzerTest<IsNullOrEmptyAnalyzer, XUnitVerifier>()
        {
            TestCode = code,
            ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0", 
                    new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"), 
                    Path.Combine("ref", "net8.0"))
                .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.IdentityModel.Tokens", "7.0.0"))),
        };
        
        analyzerTest.ExpectedDiagnostics.Add(
            new DiagnosticResult(
                "DIAG0001", 
                Microsoft.CodeAnalysis.DiagnosticSeverity.Warning
            ).WithLocation(9, 9));

        // Act & Assert
        await analyzerTest.RunAsync();
    }
}