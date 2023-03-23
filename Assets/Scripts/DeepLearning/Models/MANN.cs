using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;

namespace DeepLearning
{
    public class MANN : NeuralNetwork
    {
        public NNModel modelAsset;
        private Model m_RuntimeModel;
        private IWorker Worker;
        private Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>();

        // input and output dimensions
        public int xdim = 402;
        public int ydim = 363;
        public int omegadim = 8;

        // tensors for holding input and list of floats for holding outputs?
        private Tensor x;
        private float[] y;
        private float[] omega;

        int[,] Intervals; // a 2-D array saying when different features begin and end in the inputs, used in SetInput

        // means and std for (de-)normalizing data        
        public string DataFolder = "Assets/NormData/MANN/MANNv2"; // need path to find means and standard deviations
        private Tensor xmean;
        private Tensor xstd;
        private Tensor ymean;
        private Tensor ystd;

        private bool verbose = false;

      
        protected override bool SetupDerived()
        {
            if (Setup)
            {
                return true;
            }
            LoadDerived();
            return true;
        }

        protected override bool ShutDownDerived()
        {
            if (Setup)
            {
                UnloadDerived();
                ResetPredictionTime();
                ResetPivot();
            }
            return false;
        }

        protected void LoadDerived()
        {
            // load actual NeuralNet and create worker
            m_RuntimeModel = ModelLoader.Load(modelAsset, verbose);
            // need to explore other worker types. .  https://docs.unity3d.com/Packages/com.unity.barracuda@1.0/manual/Worker.html
            Worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel, verbose);

            // tensors to hold inputs and outputs
            x = new Tensor(1, xdim);
         
            y = new float[ydim];
            omega = new float[omegadim];

            // (un-)normalization constants
            xmean = new Tensor(new int[] { 1, 1, 1, xdim }, ReadBinary(DataFolder + "/Xmean.bin", xdim)); // what is going on here? Original SAMP code has way more dimensions here in these tensors??
            xstd = new Tensor(new int[] { 1, 1, 1, xdim }, ReadBinary(DataFolder + "/Xstd.bin", xdim));

            ymean = new Tensor(new int[] { 1, 1, 1, ydim }, ReadBinary(DataFolder + "/Ymean.bin", ydim));
            ystd = new Tensor(new int[] { 1, 1, 1, ydim }, ReadBinary(DataFolder + "/Ystd.bin", ydim));

            //network seems to be returning a lot of nans.Is this the problem?
            for (int i = 0; i < xdim; i++)
            {
                if (xstd[i] < 0.001f)
                {
                    xstd[i] += 0.001f;
                }
            }

        }

        protected void UnloadDerived()
        {

        }

        public void OnDestroy()
        {
            Worker?.Dispose(); // https://docs.unity3d.com/Packages/com.unity.barracuda@1.0/manual/MemoryManagement.html

            foreach (var key in inputs.Keys)
            {
                inputs[key].Dispose();
            }
            inputs.Clear();
        }

        public void Normalize(ref Tensor X, Tensor Xmean, Tensor Xstd)
        {
            for (int i = 0; i < X.length; i++)
            {
                X[i] = (X[i] - Xmean[i]) / Xstd[i];
            }
        }

        public void UnNormalize(ref Tensor X, Tensor Xmean, Tensor Xstd)
        {
            for (int i = 0; i < X.length; i++)
            {
                X[i] = X[i] * Xstd[i] + Xmean[i];
            }
        }

        protected override void PredictDerived()
        {
            Normalize(ref x, xmean, xstd);

            inputs["x"] = x;

            Worker.Execute(inputs);
            Tensor yout = Worker.PeekOutput("y_hat");
            Tensor omegaout = Worker.PeekOutput("omega");

            UnNormalize(ref yout, ymean, ystd);

            for (int i = 0; i < yout.length; i++)
            {
                y[i] = yout[i];
            }
            for (int i = 0; i < omegaout.length; i++)
            {
                omega[i] = omegaout[i];
            }
        }


        public override void SetInput(int index, float value)
        {
            if (Setup)
            {
            x[0, index] = value;               
            }

        }
        public override float GetOutput(int index)
        {
            if (Setup)
            {
                return y[index];
            }
            else
            {
                return 0.0f;
            }
        }
    }
}

