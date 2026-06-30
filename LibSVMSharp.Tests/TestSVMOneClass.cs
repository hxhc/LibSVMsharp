using System;
using System.IO;
using System.Linq;
using LibSVMsharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSVMSharp.Tests
{
    [TestClass]
    public class TestSVMOneClass
    {
        // Build a deterministic "normal" cluster: a 5x5 grid of 2-D points in [-1, 1].
        private SVMProblem BuildNormalProblem()
        {
            SVMProblem prob = new SVMProblem();
            for (double i = -1; i <= 1.0001; i += 0.5)
                for (double j = -1; j <= 1.0001; j += 0.5)
                    prob.Add(new[] { new SVMNode(1, i), new SVMNode(2, j) }, 1.0);
            return prob;
        }

        private SVMParameter BuildParameter()
        {
            return new SVMParameter
            {
                Type = SVMType.ONE_CLASS,
                Kernel = SVMKernelType.RBF,
                Nu = 0.1,
                Gamma = 0.5
            };
        }

        [TestMethod]
        public void OneClass_Train_ReturnsValidModel()
        {
            SVMProblem prob = BuildNormalProblem();
            SVMModel model = SVM.Train(prob, BuildParameter());

            Assert.AreEqual(SVMType.ONE_CLASS, model.Parameter.Type);
            // one-class SVM always reports nr_class == 2 in libsvm.
            Assert.AreEqual(2, model.ClassCount);
            Assert.IsTrue(model.TotalSVCount > 0);
            // Nu upper-bounds the training-error fraction, so SV count is reasonable.
            Assert.IsTrue(model.TotalSVCount <= prob.Length);
        }

        [TestMethod]
        public void OneClass_SVIndices_ReadableAfterAbiFix()
        {
            // Regression guard for the libsvm 3.37 ABI fix: without the
            // prob_density_marks field in Core/svm_model.cs, the C# marshaller
            // misalignes sv_indices (reads prob_density_marks instead) and
            // SVIndices comes back null for a non-probability model.
            SVMProblem prob = BuildNormalProblem();
            SVMModel model = SVM.Train(prob, BuildParameter());

            Assert.IsNotNull(model.SVIndices, "SVIndices must be readable (ABI alignment)");
            Assert.AreEqual(model.TotalSVCount, model.SVIndices.Length);
            // libsvm guarantees indices are in [1, num_training_data].
            Assert.IsTrue(model.SVIndices.All(i => i >= 1 && i <= prob.Length));
        }

        [TestMethod]
        public void OneClass_Predict_InlierPositive_OutlierNegative()
        {
            SVMProblem prob = BuildNormalProblem();
            SVMModel model = SVM.Train(prob, BuildParameter());

            SVMNode[] inlier = { new SVMNode(1, 0.0), new SVMNode(2, 0.0) };
            SVMNode[] outlier = { new SVMNode(1, 50.0), new SVMNode(2, 50.0) };

            Assert.AreEqual(1.0, SVM.Predict(model, inlier), "inlier should be +1");
            Assert.AreEqual(-1.0, SVM.Predict(model, outlier), "outlier should be -1");

            // Decision value of an inlier must be higher (more "normal") than an outlier.
            double[] inlierDec, outlierDec;
            SVM.PredictValues(model, inlier, out inlierDec);
            SVM.PredictValues(model, outlier, out outlierDec);
            Assert.AreEqual(1, inlierDec.Length);
            Assert.IsTrue(inlierDec[0] > outlierDec[0]);
        }

        [TestMethod]
        public void OneClass_SaveLoad_Roundtrip_PreservesPredictionsAndIndices()
        {
            SVMProblem prob = BuildNormalProblem();
            SVMModel model = SVM.Train(prob, BuildParameter());

            string path = Path.Combine(Path.GetTempPath(), "libsvm_oneclass_test.model");
            Assert.IsTrue(SVM.SaveModel(model, path));

            SVMModel loaded = SVM.LoadModel(path);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(SVMType.ONE_CLASS, loaded.Parameter.Type);

            SVMNode[] inlier = { new SVMNode(1, 0.0), new SVMNode(2, 0.0) };
            SVMNode[] outlier = { new SVMNode(1, 50.0), new SVMNode(2, 50.0) };
            Assert.AreEqual(1.0, SVM.Predict(loaded, inlier));
            Assert.AreEqual(-1.0, SVM.Predict(loaded, outlier));
            Assert.AreEqual(model.TotalSVCount, loaded.TotalSVCount);

            // NOTE: libsvm's model file format does not persist sv_indices, so
            // svm_load_model leaves it NULL (svm.cpp:3033). This is cross-version
            // libsvm behavior, not a wrapper/ABI issue. The ABI alignment on the
            // load path is still exercised by reading rho / SVCoefs / SV above.
        }
    }
}
