## LibSVMsharp

LibSVMsharp is a simple and easy-to-use C# wrapper for Support Vector Machines. 
This library uses LibSVM version 3.23 with x64 support, released on 15th of July in 2018. 

For more information visit the official [libsvm](http://www.csie.ntu.edu.tw/~cjlin/libsvm/) webpage.

## How to Install

To install LibSVMsharp, download the [Nuget package](https://www.nuget.org/packages/LibSVMsharp) or run the following command in the Package Manager Console:

`PM> Install-Package LibSVMsharp`

## Platform Support

LibSVMsharp targets **.NET 8** and runs on **Windows, Linux, and macOS** on both
**x64 and arm64**. The managed wrapper is platform-agnostic; it only needs the native
libsvm shared library to be present at run time.

| OS      | Native file        | Architectures        | Source                                            |
|---------|--------------------|----------------------|---------------------------------------------------|
| Windows | `libsvm.dll`       | x64 (arm64: build)   | Included in the repo / NuGet package              |
| Linux   | `libsvm.so`        | x64, arm64           | Build from source (see below)                     |
| macOS   | `libsvm.dylib`     | x64, arm64 (Apple S) | Build from source                                 |

The library registers a custom `NativeLibrary` resolver that derives the current
runtime identifier (e.g. `linux-arm64`, `win-x64`, `osx-arm64`) from the OS and the
process architecture, then looks for the native file under
`runtimes/<rid>/native/` (and next to the assembly as a fallback). No
`LD_LIBRARY_PATH` tweaking is required, and x64/arm64 libraries can coexist side by
side — the matching one is loaded automatically.

### Building on Linux

The native libsvm shared library is **not** shipped for Linux. Build it with the provided
helper script (requires `git`, `make`, and a C/C++ compiler such as `gcc`/`g++`):

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


