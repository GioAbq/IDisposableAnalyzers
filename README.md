# IDisposableAnalyzers.Reloaded

Roslyn analyzers for `IDisposable` - a maintained fork ("Reloaded") of the abandoned [DotNetAnalyzers/IDisposableAnalyzers](https://github.com/DotNetAnalyzers/IDisposableAnalyzers), originally created by Johan Larsson and milleniumbug.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/IDisposableAnalyzers.Reloaded.svg)](https://www.nuget.org/packages/IDisposableAnalyzers.Reloaded/)
[![Build](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/actions/workflows/ci.yml/badge.svg)](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/actions/workflows/ci.yml)

![animation](https://user-images.githubusercontent.com/1640096/51797806-5efa7380-220a-11e9-918d-c1b39da79c38.gif)

**Drop-in replacement:** same analyzer assembly (`IDisposableAnalyzers.dll`) and the same diagnostic ids (`IDISPxxx`). Swap the `IDisposableAnalyzers` package for `IDisposableAnalyzers.Reloaded` and your existing `.editorconfig` severities keep working. Targets Visual Studio 2022+ (Roslyn 4.x), the same baseline as upstream 4.x.

| Id       | Title
| :--      | :--
| [IDISP001](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP001.md)| Dispose created
| [IDISP002](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP002.md)| Dispose member
| [IDISP003](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP003.md)| Dispose previous before re-assigning
| [IDISP004](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP004.md)| Don't ignore created IDisposable
| [IDISP005](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP005.md)| Return type should indicate that the value should be disposed
| [IDISP006](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP006.md)| Implement IDisposable
| [IDISP007](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP007.md)| Don't dispose injected
| [IDISP008](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP008.md)| Don't assign member with injected and created disposables
| [IDISP009](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP009.md)| Add IDisposable interface
| [IDISP010](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP010.md)| Call base.Dispose(disposing)
| [IDISP011](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP011.md)| Don't return disposed instance
| [IDISP012](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP012.md)| Property should not return created disposable
| [IDISP013](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP013.md)| Await in using
| [IDISP014](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP014.md)| Use a single instance of HttpClient
| [IDISP015](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP015.md)| Member should not return created and cached instance
| [IDISP016](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP016.md)| Don't use disposed instance
| [IDISP017](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP017.md)| Prefer using
| [IDISP018](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP018.md)| Call SuppressFinalize
| [IDISP019](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP019.md)| Call SuppressFinalize
| [IDISP020](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP020.md)| Call SuppressFinalize(this)
| [IDISP021](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP021.md)| Call this.Dispose(true)
| [IDISP022](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP022.md)| Call this.Dispose(false)
| [IDISP023](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP023.md)| Don't use reference types in finalizer context
| [IDISP024](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP024.md)| Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer
| [IDISP025](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP025.md)| Class with no virtual dispose method should be sealed
| [IDISP026](https://github.com/GioAbq/IDisposableAnalyzers.Reloaded/blob/master/documentation/IDISP026.md)| Class with no virtual DisposeAsyncCore method should be sealed
| [SyntaxTreeCacheAnalyzer]()| Controls caching of for example semantic models for syntax trees

## Using IDisposableAnalyzers.Reloaded

Add the [IDisposableAnalyzers.Reloaded](https://www.nuget.org/packages/IDisposableAnalyzers.Reloaded/) NuGet package to your project(s). The analyzer then lights up in Visual Studio, Rider and `dotnet build` / CI.

The severity of individual rules is configured via `.editorconfig`, for example:

```ini
dotnet_diagnostic.IDISP001.severity = warning
dotnet_diagnostic.IDISP004.severity = error
```

## Installation

```
dotnet add package IDisposableAnalyzers.Reloaded
```

or

```
Install-Package IDisposableAnalyzers.Reloaded
```

## Acknowledgements

Originally created by Johan Larsson and milleniumbug as [IDisposableAnalyzers](https://github.com/DotNetAnalyzers/IDisposableAnalyzers) under the MIT license. This fork revives and maintains that work after the upstream went dormant in 2024. All original diagnostic ids and behavior are preserved.

## Current status

The analyzer tackles a genuinely hard problem and will never be perfect, but it finds real bugs and improves one test case at a time. Please file issues where it should warn but does not, or where it warns and there is no bug.
