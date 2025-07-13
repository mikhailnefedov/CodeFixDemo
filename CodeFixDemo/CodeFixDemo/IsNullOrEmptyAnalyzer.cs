using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeFixDemo;

/// <summary>
/// Analyzer detecting usages of IsNullOrEmpty() from Microsoft.IdentityModel.Tokens
/// package. 
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IsNullOrEmptyAnalyzer : DiagnosticAnalyzer
{
    // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
    public const string DiagnosticId = "DIAG0001";

    private const string Title = "Use custom IsNullOrEmpty instead of falsely exposed CollectionUtilities extension method";
    
    private const string MessageFormat = "Replace usage of Microsoft.IdentityModel.Tokens.CollectionUtilities.IsNullOrEmpty()";

    /// <summary>
    /// Category of the diagnostic -> in this case Usage.
    /// </summary>
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule =
        new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = 
        ImmutableArray.Create(Rule);
    
    public override void Initialize(AnalysisContext context)
    {
        // You must call this method to avoid analyzing generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        
        // Allow concurrent execution for better performance.
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = context.Node as InvocationExpressionSyntax;
        var symbol = context.SemanticModel.GetSymbolInfo(invocation!).Symbol as IMethodSymbol;

        if (symbol?.ContainingType.ToDisplayString() == "Microsoft.IdentityModel.Tokens.CollectionUtilities"
            && symbol.Name == "IsNullOrEmpty")
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation!.GetLocation()));
        }
    }
}