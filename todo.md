# Dependency Update TODO

## Completed Updates

- [x] **AWSSDK.Core** 3.1.3.2 -> 3.7.500.85 (latest v3 series)
- [x] **AWSSDK.S3** 3.1.3.1 -> 3.7.510.11 (latest v3 series)
- [x] **Newtonsoft.Json** 7.0.1 -> 13.0.4 (**semi-breaking**: assembly version changed from 7.0.0.0 to 13.0.0.0; binding redirects updated; API is backwards-compatible but serialization behavior may differ for edge cases)
- [x] **MongoDB.Driver** 2.1.0 (mongocsharpdriver) -> 3.6.0 (MongoDB.Driver) (**BREAKING**: legacy v1 API removed; MongoReaderPlugin.cs rewritten to use v3 GridFSBucket API; mongocsharpdriver meta-package replaced)
- [x] **protobuf-net** 2.0.0.668 -> 3.2.56 (**BREAKING**: v3 has API changes; TinyCache plugin may need code updates if serialization attributes changed)
- [x] **xunit** 2.1.0 -> 2.9.3 (all test projects; sub-packages updated: abstractions 2.0.3, analyzers 1.27.0 new)
- [x] **xunit** 1.9.1 -> 2.9.3 (WebP.Test; migrated from single-dll to multi-assembly xunit 2.x)
- [x] **xunit.runner.console** 2.0.0 -> 2.9.3 (CI; tool path changed from `tools/` to `tools/net472/`)
- [x] **xunit.runner.visualstudio** 2.1.0 -> 2.8.2 (latest v2 line; build props path changed from `build/net20/` to `build/net462/`)
- [x] **xunit.runners** 2.0.0 -> removed (deprecated; was in .nuget/packages.config files)
- [x] **Moq** 4.7.8 -> 4.20.72 (transitive deps: Castle.Core 5.1.1, System.Threading.Tasks.Extensions 4.5.4, System.Runtime.CompilerServices.Unsafe 4.5.3)
- [x] **Castle.Core** 4.0.0 -> 5.1.1 (follows Moq; assembly version 4.0.0.0 -> 5.0.0.0)
- [x] **NSubstitute** 1.9.2 -> 5.3.0 (ProviderTests only; removed from LicenseVerifier.Tests as unused)
- [x] **FsUnit.xUnit** 1.3.0.1 -> removed (WebP.Test; was imported but no assertions used)
- [x] Removed unused: **Should** 1.1.20, **xunit.should** 1.1, **NSubstitute** 1.9.2 from LicenseVerifier.Tests
- [x] Bumped **WebP.Test** target framework net45 -> net472
- [x] **Gallio/MbUnit** 3.4.14 -> removed, migrated to xunit 2.9.3 (DiskCache.Tests, HttpTests, PdfRenderer.Tests; `[TestFixture]` removed, `[Test]` -> `[Fact]`, `[Row]` -> `[InlineData]`/`[Theory]`, `[FixtureSetUp/TearDown]` -> constructor/IDisposable, `[ThreadedRepeat]` -> explicit thread loops, `Assert.AreEqual` -> `Assert.Equal`, `Assert.AreApproximatelyEqual` -> `Assert.Equal(e,a,precision)`, `Assert.Contains(coll,item)` -> reversed args; TFMs bumped net40/net35 -> net472)

- [x] **NLog** 3.2.0 -> 6.1.0 (Logging plugin; API stable — `LogManager`, `Logger`, `LogLevel.FromString`, `XmlLoggingConfiguration` all unchanged)
- [x] **PdfiumViewer** 2.13.0 -> **PdfiumViewer.Updated** 2.14.5 (drop-in maintained fork; same `PdfiumViewer` namespace; PdfiumRenderer.Tests switched to `PdfiumViewer.Native.x86.v8-xfa` PackageReference for native DLL)

## Abandoned / Deprecated (action needed)
2. **AForge / AForge.Imaging / AForge.Math** — dead since 2013; `.NetStandard` forks exist
3. **OpenCvSharp-WithoutDll** — deprecated, replaced by `OpenCvSharp4`
4. **WindowsAzure.Storage** — deprecated 2018, replaced by `Azure.Storage.Blobs`

## Image Processing (pending)

| Package | In Use | Latest | Gap | Status |
|---------|--------|--------|-----|--------|
| AForge | 2.2.5 | 2.2.5 | none | **ABANDONED** (2013) |
| AForge.Imaging | 2.2.5 | 2.2.5 | none | **ABANDONED** (2013) |
| AForge.Math | 2.2.5 | 2.2.5 | none | **ABANDONED** (2013) |
| OpenCvSharp-WithoutDll | 2.4.10.x | — | — | **DEPRECATED** -> OpenCvSharp4 4.13.0 |
| Imazen.WebP | 10.0.1 (csproj) | — | — | new release pending |

## Other (pending)

| Package | In Use | Latest | Gap | Status |
|---------|--------|--------|-----|--------|
| WindowsAzure.Storage | 6.0 | 9.3.3 | **MAJOR** | **DEPRECATED** -> Azure.Storage.Blobs |
| Microsoft.AspNet.Mvc | 3.0.x | 5.3.0 | **2 MAJOR** | legacy |
| FAKE | 3.26.1 | 5.16.0 / CLI 6.1.4 | **2-3 MAJOR** | |
