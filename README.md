Changes are inevitable when you are working with many repositories. As your applications and codebases evolve, methods are marked as obsolete and usage patterns fall out of favor. But how do you tackle a piece of code that’s been used hundreds of times and now needs to be replaced?

Manual refactorings of the occurrences can take a lot of time. That's where Roslyn code analyzers and CodeFixes may shine.

In this blog post, I’ll walk through how to build a simple Roslyn CodeFix that upgrades problematic code. For illustration, I will be using a real-world example: the `IsNullOrEmpty()` from `Microsoft.IdentityModel.Tokens`. This method was mistakenly published as `public` and later corrected to `internal` in the 8.0.0 release. 

Note: The shown CodeFix may not work on every usage, but is perfect to illustrate some points.

## Scanning for the Problem: Writing the Analyzer

Before we can fix anything, we need to find it. That’s where Roslyn analyzers come in. Roslyn analyzers let us inspect source code during compilation and raise diagnostics when a specific pattern is found. In our case, we’re looking for any usage of the now-internal `CollectionUtilities.IsNullOrEmpty()` method from `Microsoft.IdentityModel.Tokens`.

The analyzer below registers a syntax node action on every method invocation. If it spots a method named `IsNullOrEmpty` whose containing type matches `Microsoft.IdentityModel.Tokens.CollectionUtilities`, it reports a warning diagnostic.

Key parts are:

- Diagnostic ID & Descriptor: This defines a unique ID, a readable message, and the severity level.

- Initialize Method: Sets up the analyzer to run concurrently and skip generated code.

- AnalyzeInvocation: The heart of the logic which inspects each method call and checks whether it matches the one we want to flag.

[Analyzer](https://github.com/mikhailnefedov/CodeFixDemo/blob/main/CodeFixDemo/CodeFixDemo/IsNullOrEmptyAnalyzer.cs)

## Building a Roslyn CodeFix

After detecting problematic code with our analyzer, the next step is to help developers fix it. This is where the CodeFixProvider comes into play. It listens for diagnostics we raised and offers users a one-click action to rewrite the code safely and consistently.

This `IsNullOrEmptyCodeFixProvider` listens for our analyzer's `DIAG0001` diagnostic. When it detects a flagged usage of the `IsNullOrEmpty()` method from `CollectionUtilities`, it transforms that code into a more reliable inline check:

```csharp
list is null || list.Any() is false
```

Key Components:

* `FixableDiagnosticIds`: Links this CodeFix to the diagnostic emitted by our analyzer
* `GetFixAllProvider()` returns `WellKnownFixAllProviders.BatchFixer` to allow batched fixing
* `RegisterCodeFixesAsync`: Registers the code fix with a title and links to the actual rewrite logic
* `ReplaceIsNullOrEmptyInvocationAsync`: Constructs a new expression and imports `System.Linq` if needed

[CodeFixProvider](https://github.com/mikhailnefedov/CodeFixDemo/blob/main/CodeFixDemo/CodeFixDemo/IsNullOrEmptyCodeFixProvider.cs)

Rider users will see the CodeFix option alongside other suggestions in the lightbulb menu:

![Roslyn action in Rider](https://github.com/mikhailnefedov/CodeFixDemo/blob/main/rider-roslyn-action.png)


## Testing the CodeFix code

You can find all the code shown in this post in my [GitHub repository](https://github.com/mikhailnefedov/CodeFixDemo). While building the CodeFix itself was rewarding, what surprised me was how useful and flexible the testing infrastructure turned out to be.

I started with the Roslyn Analyzer project template, which gives you an excellent out-of-the-box test setup. Especially if you don’t rely on external dependencies. But once you beginworking with external packages like Microsoft.IdentityModel.Tokens, adaptations to the tests need to be made to handle those references properly.

It is important to note the explicit usage of the CSharpCodeFixTest. This allows us to add additional assembly references (In this case: Microsoft.IdentityModel.Tokens).

[CodeFixProviderTest](https://github.com/mikhailnefedov/CodeFixDemo/blob/main/CodeFixDemo/CodeFixDemo.Tests/IsNullOrEmptyCodeFixProviderTests.cs)


## Wrapping it up

One thing I particularly appreciated while building this CodeFix: we didn’t need to import or rely on the external NuGet package in the analyzer code. That means our analyzer and the tests around remain untouched by future breaking changes. Since the example showcased here specifically targets `Microsoft.IdentityModel.Tokens` versions before 8.0.0, this isolation is a huge win for long-term maintainability.

This experience sparked a broader idea: using CodeFixes to systematically renew code across repositories. When legacy patterns get marked as Obsolete, manual updates can be a chore. Modernizing your solution becomes a breeze with a diagnostic and CodeFix in place

This command will apply your fix across all occurrences in the solution, saving time while keeping your codebase tidy:

`dotnet format --diagnostics DIAG0001`

