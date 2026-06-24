using System;
using Xunit;

[assembly: CLSCompliant(false)]

// Analyzer tests share a process-wide Gu.Roslyn SyntaxTreeCache (class fixture in AllAnalyzersValid).
// NUnit ran sequentially; xUnit parallelizes by default. Run serially to avoid racing the cache.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
