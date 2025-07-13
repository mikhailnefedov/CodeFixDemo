using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace CodeFixDemo.Tests;

public class IsNullOrEmptyCodeFixProviderTests
{
    [Fact]
    public async Task Test()
    {
        // Arrange
        const string testCode = @"
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

public class Examples
{
    public void Example1() {
        IEnumerable<int> myEnumerable = new[] { 1, 2, 3, 4};
        if (myEnumerable.IsNullOrEmpty())
        {
            // do something
        }
    }
}
";
        
        const string expectedCode = @"
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

public class Examples
{
    public void Example1() {
        IEnumerable<int> myEnumerable = new[] { 1, 2, 3, 4};
        if (myEnumerable is null || myEnumerable.Any() is false)
        {
            // do something
        }
    }
}
";
        
        var codeFixTest = new CSharpCodeFixTest<
            IsNullOrEmptyAnalyzer,
            IsNullOrEmptyCodeFixProvider,
            XUnitVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0", 
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref", "8.0.0"), 
                    Path.Combine("ref", "net8.0"))
                .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.IdentityModel.Tokens", "7.0.0"))),
        };
        
        codeFixTest.ExpectedDiagnostics.Add(
            new DiagnosticResult(
                "DIAG0001", 
                Microsoft.CodeAnalysis.DiagnosticSeverity.Warning
            ).WithLocation(9, 13));
        
        codeFixTest.FixedCode = expectedCode;

        // Act & Assert
        await codeFixTest.RunAsync();
    }
}