using System;
using Xunit;

[assembly: CLSCompliant(false)]

// These analyzer tests share a process-wide Gu.Roslyn SyntaxTreeCache (started as a class
// fixture in AllAnalyzersValid). NUnit ran them sequentially; xUnit parallelizes collections
// by default, which races on that global cache and yields spurious diagnostics. Run serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
