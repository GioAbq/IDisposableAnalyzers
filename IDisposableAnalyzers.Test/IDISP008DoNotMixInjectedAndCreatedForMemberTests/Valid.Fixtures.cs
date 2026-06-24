namespace IDisposableAnalyzers.Test.IDISP008DoNotMixInjectedAndCreatedForMemberTests;

using Microsoft.CodeAnalysis.Diagnostics;

public sealed class ValidFieldAndPropertyDeclaration : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new FieldAndPropertyDeclarationAnalyzer();
}

public sealed class ValidAssignment : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new AssignmentAnalyzer();
}
