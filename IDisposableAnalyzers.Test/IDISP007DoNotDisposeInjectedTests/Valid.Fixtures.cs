namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests;

using Microsoft.CodeAnalysis.Diagnostics;

public sealed class ValidDisposeCall : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new DisposeCallAnalyzer();
}

public sealed class ValidLocalDeclaration : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new LocalDeclarationAnalyzer();
}

public sealed class ValidUsingStatement : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new UsingStatementAnalyzer();
}

public sealed class ValidRecursionDisposeCall : ValidRecursion
{
    protected override DiagnosticAnalyzer Analyzer => new DisposeCallAnalyzer();
}

public sealed class ValidRecursionLocalDeclaration : ValidRecursion
{
    protected override DiagnosticAnalyzer Analyzer => new LocalDeclarationAnalyzer();
}

public sealed class ValidRecursionUsingStatement : ValidRecursion
{
    protected override DiagnosticAnalyzer Analyzer => new UsingStatementAnalyzer();
}

public sealed class ValidReactiveDisposeCall : ValidReactive
{
    protected override DiagnosticAnalyzer Analyzer => new DisposeCallAnalyzer();
}

public sealed class ValidReactiveLocalDeclaration : ValidReactive
{
    protected override DiagnosticAnalyzer Analyzer => new LocalDeclarationAnalyzer();
}

public sealed class ValidReactiveUsingStatement : ValidReactive
{
    protected override DiagnosticAnalyzer Analyzer => new UsingStatementAnalyzer();
}
