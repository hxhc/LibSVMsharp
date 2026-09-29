## LibSVMsharp

LibSVMsharp is a simple and easy-to-use C# wrapper for Support Vector Machines.
This library uses **LibSVM version 3.37** with x64 and arm64 support.

For more information visit the official [libsvm](http://www.csie.ntu.edu.tw/~cjlin/libsvm/) webpage.

## What's Changed in This Fork

> The items below are the focus of recent updates over the upstream 3.23 baseline.

- **Native libsvm upgraded 3.23 → 3.37.** `Core/svm_model.cs` now includes the
  `prob_density_marks` field so the managed struct matches the 3.37 memory layout.
  Without it `sv_indices` / `label` / `nSV` were misaligned by 8 bytes and one-class
  / probability models read garbage. `Core/libsvm.cs` `VERSION` bumped to `3.37`.
- **Multi-target `netstandard2.0;netstandard2.1;net8.0`.** The library is now
  consumable from **any .NET Core version** — 2.0/2.1/2.2 via `netstandard2.0`,
  3.0/3.1 and 5/6/7 via `netstandard2.1`, 8+ via `net8.0` — as well as from
  **.NET Framework 4.6.1+**, Mono, Unity and Xamarin. NuGet picks the highest
  compatible target automatically. The smart `NativeLibrary` resolver is compiled
  in only on the `net8.0` target (`#if NET6_0_OR_GREATER`); the netstandard targets
  fall back to the runtime's default `libsvm` resolution.
- **`build-native.sh` rewritten.**  Builds both `linux-x64`
  and `linux-arm64` (arm64 cross-compile on x64 needs `gcc-aarch64-linux-gnu`).
- **One-class SVM validated.** Added `LibSVMSharp.Tests/TestSVMOneClass.cs` — 4
  tests covering train / predict (+1 inlier / −1 outlier), decision values,
  save-load roundtrip, and an ABI regression guard on `SVIndices`. Full suite:
  **35/35 green**.
- **Native binaries no longer in source control.** `runtimes/` is gitignored; run
  `build-native.sh` locally to populate it.

## How to Install

To install LibSVMsharp, download the [Nuget package](https://www.nuget.org/packages/LibSVMsharp) or run the following command in the Package Manager Console:

`PM> Install-Package LibSVMsharp`

> **Note:** nuget.org only hosts this package up to **1.2.5**. The **3.37.0** build
> in this repository is *not* published there, so restore it from a local feed
> instead (see below).

### Local package (3.37.0)

Pack the library from this repository:

```bash
dotnet pack LibSVMsharp/LibSVMsharp.csproj -c Release -o LibSVMsharp/nupkg
```

The repository root already ships a `nuget.config` that registers
`LibSVMsharp/nupkg` as a package source, so projects **inside this repository tree**
restore `3.37.0` without further setup. For a project that lives elsewhere, either
copy that `nuget.config` next to it, or register the feed machine-wide once:

```bash
dotnet nuget add source "<repo>/LibSVMsharp/nupkg" -n libsvmsharp-local
```

Then reference it normally:

```xml
<PackageReference Include="LibSVMsharp" Version="3.37.0" />
```

> `.nupkg` files are gitignored, so run the `dotnet pack` command above on each
> machine (or after changing the sources). If a machine previously cached a
> different `3.37.0` build, clear `~/.nuget/packages/libsvmsharp/3.37.0` — NuGet
> treats a package version as immutable.

## Platform Support

LibSVMsharp multi-targets **`netstandard2.0`**, **`netstandard2.1`** and
**`net8.0`**, so it can be referenced from **any .NET Core version** as well as
**.NET Framework 4.6.1+**, **Mono/Unity/Xamarin**. NuGet selects the highest
compatible target:

| Consumer framework                        | Resolved asset          |
|-------------------------------------------|-------------------------|
| .NET Core 2.0 / 2.1 / 2.2, .NET Framework 4.6.1+ | `netstandard2.0` |
| .NET Core 3.0 / 3.1, .NET 5 / 6 / 7       | `netstandard2.1`        |
| .NET 8 and newer                          | `net8.0`                |

It runs on **Windows, Linux, and macOS** on both **x64 and arm64**.
The managed wrapper is platform-agnostic; it only needs the native libsvm shared
library to be present at run time.

| OS      | Native file        | Architectures        | Source                                            |
|---------|--------------------|----------------------|---------------------------------------------------|
| Windows | `libsvm.dll`       | x64 (arm64: build)   | Included in the repo / NuGet package              |
| Linux   | `libsvm.so`        | x64, arm64           | Build from source (see below)                     |
| macOS   | `libsvm.dylib`     | x64, arm64 (Apple S) | Build from source                                 |

On the **`net8.0`** target the library registers a custom `NativeLibrary` resolver
that derives the current runtime identifier (e.g. `linux-arm64`, `win-x64`,
`osx-arm64`) from the OS and the process architecture, then looks for the native
file under `runtimes/<rid>/native/` (and next to the assembly as a fallback). No
`LD_LIBRARY_PATH` tweaking is required, and x64/arm64 libraries can coexist side by
side — the matching one is loaded automatically. On the **`netstandard2.0` /
`netstandard2.1`** targets the custom resolver is not compiled in (`NativeLibrary`
is not available there), so the runtime's default resolution applies: the native
file is found next to the assembly, through the `runtimes/<rid>/native/` entry that
NuGet records in `deps.json`, or on the system library path.

### Building on Linux

The native libsvm shared library is **not** shipped for Linux. Build it with the provided
helper script (requires `wget` or `curl`, `tar`, `make`, and a C/C++ compiler such as
`gcc`/`g++`); it downloads `libsvm-3.37.tar.gz` from the official mirror:

```bash
./build-native.sh            # build for the host architecture
./build-native.sh x64        # build x86-64 explicitly
./build-native.sh arm64      # build aarch64 (native on arm64, or cross-compiled on x64)
dotnet build LibSVMsharp.sln
```

Run it once per architecture to populate both `runtimes/linux-x64/native/` and
`runtimes/linux-arm64/native/`. For arm64 cross-compilation on an x86-64 host, install
the aarch64 toolchain first (`sudo apt install gcc-aarch64-linux-gnu g++-aarch64-linux-gnu`).

Then run an example:

```bash
cd LibSVMsharp.Examples.Classification
dotnet run
```

## License
LibSVMsharp is released under the MIT License and libsvm is released under the [modified BSD Lisence](http://www.csie.ntu.edu.tw/~cjlin/libsvm/faq.html#f204) which is compatible with many free software licenses such as GPL.

## Example Codes

#### Simple Classification

```C#
SVMProblem problem = SVMProblemHelper.Load(@"dataset_path.txt");
SVMProblem testProblem = SVMProblemHelper.Load(@"test_dataset_path.txt");

SVMParameter parameter = new SVMParameter();
parameter.Type = SVMType.C_SVC;
parameter.Kernel = SVMKernelType.RBF;
parameter.C = 1;
parameter.Gamma = 1;

SVMModel model = SVM.Train(problem, parameter);

double[] target = new double[testProblem.Length];
for (int i = 0; i < testProblem.Length; i++)
  target[i] = SVM.Predict(model, testProblem.X[i]);

double accuracy = SVMHelper.EvaluateClassificationProblem(testProblem, target);
```

#### Simple Classification with Extension Methods

```C#
SVMProblem problem = SVMProblemHelper.Load(@"dataset_path.txt");
SVMProblem testProblem = SVMProblemHelper.Load(@"test_dataset_path.txt");

SVMParameter parameter = new SVMParameter();

SVMModel model = problem.Train(parameter);

double[] target = testProblem.Predict(model);
double accuracy = testProblem.EvaluateClassificationProblem(target);
```

#### Simple Regression
```C#
SVMProblem problem = SVMProblemHelper.Load(@"dataset_path.txt");
SVMProblem testProblem = SVMProblemHelper.Load(@"test_dataset_path.txt");

SVMParameter parameter = new SVMParameter();

SVMModel model = problem.Train(parameter);

double[] target = testProblem.Predict(model);
double correlationCoeff;
double meanSquaredErr = testProblem.EvaluateRegressionProblem(target, out correlationCoeff);
```

#### One-Class SVM (Anomaly Detection)
```C#
SVMProblem problem = SVMProblemHelper.Load(@"normal_data.txt");

SVMParameter parameter = new SVMParameter();
parameter.Type = SVMType.ONE_CLASS;   // distribution estimation
parameter.Kernel = SVMKernelType.RBF;
parameter.Nu = 0.1;                   // upper bound on training outliers

SVMModel model = SVM.Train(problem, parameter);

// Predict returns +1 for inliers (normal) and -1 for outliers (anomalies).
double label = SVM.Predict(model, new[]
{
    new SVMNode(1, 0.0),
    new SVMNode(2, 0.0)
});
```


