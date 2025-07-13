using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace CodeFixDemo;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IsNullOrEmptyCodeFixProvider))]
public class IsNullOrEmptyCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// We use the static BatchFixer.
    /// <see href="https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md#spectrum-of-fixall-providers"/>
    /// </summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override ImmutableArray<string> FixableDiagnosticIds => 
        ImmutableArray.Create(IsNullOrEmptyAnalyzer.DiagnosticId);
    
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root?.FindNode(context.Span) is not InvocationExpressionSyntax invocation)
        {
            return;
        }
        
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with is null || .Any() is false",
                createChangedSolution: ct => ReplaceIsNullOrEmptyInvocationAsync(context.Document, invocation, ct),
                equivalenceKey: "ReplaceWithIsNullOrAny"), context.Diagnostics);
    }

    private async Task<Solution> ReplaceIsNullOrEmptyInvocationAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        
        var identifier = invocation.Expression.ChildNodes().OfType<IdentifierNameSyntax>().First();
        var newExpression = SyntaxFactory
            .ParseExpression($"{identifier} is null || {identifier}.Any() is false")
            .WithLeadingTrivia(invocation.GetLeadingTrivia())
            .WithTrailingTrivia(invocation.GetTrailingTrivia());
        editor.ReplaceNode(invocation, newExpression);

        var newDocument = editor.GetChangedDocument();

        if (await newDocument.GetSyntaxRootAsync(cancellationToken) is CompilationUnitSyntax compilationUnit &&
            compilationUnit.Usings.Any(u => u.Name.ToString() == "System.Linq") is false)
        {
            var linqUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq"));
            var newRoot = compilationUnit.AddUsings(linqUsing);
            newDocument = newDocument.WithSyntaxRoot(newRoot);
        }
        
        return newDocument.Project.Solution;
    }
}