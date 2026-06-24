namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests;

using Microsoft.CodeAnalysis.Diagnostics;

public sealed class ValidArgument : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new ArgumentAnalyzer();
}

public sealed class ValidAssignment : Valid
{
    protected override DiagnosticAnalyzer Analyzer => new AssignmentAnalyzer();
}
