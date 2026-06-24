namespace IDisposableAnalyzers.Test.Helpers;

// Public test-only mirror of the internal IDisposableAnalyzers.ReturnValueSearch so that
// the (public, xUnit-required) ReturnValueWalkerTests theory methods do not expose an
// internal type in their signature. Values must stay aligned with ReturnValueSearch.
public enum RvSearch
{
    Member,
    Recursive,
    RecursiveInside,
}
