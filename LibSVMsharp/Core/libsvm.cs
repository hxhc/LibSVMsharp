using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LibSVMsharp.Core
{
    /// <summary>
    /// For more information about libsvm project:
    /// Official: http://www.csie.ntu.edu.tw/~cjlin/libsvm/
    /// Github: https://github.com/cjlin1/libsvm
    /// </summary>
    public static class libsvm
    {
        public static string VERSION = "3.23";

        static libsvm()
        {
            // Register a custom DllImport resolver so the native libsvm library is
            // located on every OS/architecture without a hard-coded file name. It
            // probes next to the assembly and under runtimes/<os>-<arch>/native/
            // (e.g. runtimes/linux-x64/native/, runtimes/linux-arm64/native/).
            // NativeLibrary is only available on .NET 5+; under netstandard2.1 we
            // fall back to the runtime's default resolution rules for "libsvm".
#if NET6_0_OR_GREATER
            NativeLibrary.SetDllImportResolver(typeof(libsvm).Assembly, ResolveLibSvm);
#endif
        }

#if NET6_0_OR_GREATER
        private static IntPtr ResolveLibSvm(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "libsvm")
            {
                string baseDir = AppContext.BaseDirectory;
                string fileName = NativeLibFileName();
                string rid = CurrentRuntimeId();

                var candidates = new List<string>
                {
                    Path.Combine(baseDir, fileName),
                    Path.Combine(baseDir, "runtimes", rid, "native", fileName),
                };

                // libsvm's Makefile also emits a versioned SONAME (libsvm.so.3) on Linux.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    candidates.Add(Path.Combine(baseDir, "libsvm.so.3"));
                    candidates.Add("libsvm.so.3");
                }

                foreach (string path in candidates)
                {
                    if (NativeLibrary.TryLoad(path, assembly, DllImportSearchPath.AssemblyDirectory, out IntPtr handle))
                        return handle;
                }
            }

            // Fall back to the default resolution rules for the requested name.
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out IntPtr fallback))
                return fallback;

            return IntPtr.Zero;
        }

        private static string NativeLibFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "libsvm.dll";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "libsvm.dylib";
            return "libsvm.so";
        }

        private static string CurrentRuntimeId()
        {
            string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
                      : "linux";

            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                Architecture.X86 => "x86",
                _ => "x64",
            };

            return os + "-" + arch;
        }
#endif

        /// <param name="prob">svm_problem</param>
        /// <param name="param">svm_parameter</param>
        /// <returns>svm_model</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr svm_train(IntPtr prob, IntPtr param);
        /// <param name="prob">svm_problem</param>
        /// <param name="param">svm_parameter</param>
        /// <param name="nr_fold">int</param>
        /// <param name="target">double[]</param>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern void svm_cross_validation(IntPtr prob, IntPtr param, int nr_fold, IntPtr target);

        /// <param name="model_file_name">string</param>
        /// <param name="model">svm_model</param>
        /// <returns>bool</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern int svm_save_model(IntPtr model_file_name, IntPtr model);
        /// <param name="model_file_name">string</param>
        /// <returns>svm_model</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr svm_load_model(IntPtr model_file_name);

        /// <param name="model">svm_model</param>
        /// <returns>int</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern int svm_get_svm_type(IntPtr model);
        /// <param name="model">svm_model</param>
        /// <returns>int</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern int svm_get_nr_class(IntPtr model);
        /// <param name="model">svm_model</param>
        /// <param name="label">int[]</param>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern void svm_get_labels(IntPtr model, IntPtr label);
        /// <param name="model">svm_model</param>
        /// <param name="label">int[]</param>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern void svm_get_sv_indices(IntPtr model, IntPtr sv_indices);
        /// <param name="model">svm_model</param>
        /// <returns>int</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern int svm_get_nr_sv(IntPtr model);
        /// <param name="model">svm_model</param>
        /// <returns>double</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern double svm_get_svr_probability(IntPtr model);

        /// <param name="model">svm_model</param>
        /// <param name="x">svm_node[]</param>
        /// <param name="dec_values">double[]</param>
        /// <returns>double</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern double svm_predict_values(IntPtr model, IntPtr x, IntPtr dec_values);
        /// <param name="model">svm_model</param>
        /// <param name="dec_values">double[]</param>
        /// <returns>double</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern double svm_predict(IntPtr model, IntPtr x);
        /// <param name="model">svm_model</param>
        /// <param name="x">svm_node[]</param>
        /// <param name="dec_values">double[]</param>
        /// <returns>double</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern double svm_predict_probability(IntPtr model, IntPtr x, IntPtr prob_estimates);

        /// <param name="model_ptr">svm_model</param>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern void svm_free_model_content(IntPtr model_ptr);
        /// <param name="model_ptr_ptr">svm_model*</param>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern void svm_free_and_destroy_model(ref IntPtr model_ptr_ptr);
        /// <param name="param">svm_parameter</param>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern void svm_destroy_param(IntPtr param);

        /// <param name="prob">svm_problem</param>
        /// <param name="param">svm_parameter</param>
        /// <returns>string</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr svm_check_parameter(IntPtr prob, IntPtr param);
        /// <param name="model">svm_model</param>
        /// <returns>int</returns>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool svm_check_probability_model(IntPtr model);

        /// <param name="print_function">void (*)(const char *)</param>
        [DllImport("libsvm", CallingConvention = CallingConvention.Cdecl)]
        public static extern void svm_set_print_string_function(IntPtr print_function);
    }
}
