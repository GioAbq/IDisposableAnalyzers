namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests;

using Microsoft.CodeAnalysis.Diagnostics;

public sealed class ValidLocalDeclaration : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new LocalDeclarationAnalyzer();
}

public sealed class ValidArgument : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new ArgumentAnalyzer();
}

public sealed class ValidAssignment : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new AssignmentAnalyzer();
}
