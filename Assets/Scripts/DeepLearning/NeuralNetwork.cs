using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace DeepLearning
{
    // this is a general abstract class that all networks, e.g. MVAE or PFNN or MANN should derive from
    public abstract class NeuralNetwork : MonoBehaviour
    {

        public double PredictionTime { get; set; } = 0.0f;

        // https://www.w3schools.com/cs/cs_properties.php
        // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/auto-implemented-properties
        public bool Setup { get; set; } = false;

        private int Pivot = -1;

        // protected keyword: accessible from their containing class or types dervied from it
        protected abstract bool SetupDerived(); // Every specific model, e.g. must DERIVE from this class, hence SetupDERIVED or PredictDERIVED
        protected abstract bool ShutDownDerived();
        protected abstract void PredictDerived();

        public abstract void SetInput(int index, float value);
        // similar to SAMP where they have a SetInput in the derived class
        // I guess if just one input I could do it here
        // but currently I have multiple inputs, e.g. x and z
        // and so I need to do it similar to SAMP with Intervals
        // actually I don't even need it this complicated as z is just a noise sample and not really an input
        // still though, I guess best to keep this as general as possible
        public abstract float GetOutput(int index);

        private void OnEnable()
        {
            Setup = SetupDerived();
        }

        private void OnDisable()
        {
            Setup = ShutDownDerived();
        }

        public void Predict()
        {
            System.DateTime timestamp = Utility.GetTimestamp();
            PredictDerived();
            PredictionTime = Utility.GetElapsedTime(timestamp);
        }

        public void ResetPivot()
        {
            Pivot = -1;
        }

        public void ResetPredictionTime()
        {
            PredictionTime = 0.0f;
        }


        public void Feed(float value)
        {
            if (Setup)
            {
                Pivot += 1;
                SetInput(Pivot, value);
            }
        }

        public void Feed(float[] values)
        {
            if (Setup)
            {
                for (int i=0; i<values.Length; i++)
                {
                    Feed(values[i]);
                }    
            }
        }
        public void Feed(Vector2 vector)
        {
            Feed(vector.x);
            Feed(vector.y);
        }

        public void Feed(Vector3 vector)
        {
            Feed(vector.x);
            Feed(vector.y);
            Feed(vector.z);
        }

        public void FeedXY(Vector3 vector)
        {
            Feed(vector.x);
            Feed(vector.y);
        }

        public void FeedXZ(Vector3 vector)
        {
            Feed(vector.x);
            Feed(vector.z);
        }
        
        public void FeedYZ(Vector3 vector)
        {
            Feed(vector.y);
            Feed(vector.z);
        }

        public float Read()
        {
            Pivot += 1;
            return GetOutput(Pivot);
        }
        
        public float Read(float min, float max)
        {
            Pivot += 1;
            return Mathf.Clamp(GetOutput(Pivot), min, max);
        }

        public float[] Read(int count)
        {
            float[] values = new float[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = Read();
            }
            return values;
        }

        public float[] Read(int count, float min, float max)
        {
            float[] values = new float[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = Read(min, max);
            }
            return values;
        }
        public Vector3 ReadVector2()
        {
            return new Vector2(Read(), Read());
        }

        public Vector3 ReadVector3()
        {
            return new Vector3(Read(), Read(), Read());
        }

        public Vector3 ReadXY()
        {
            return new Vector3(Read(), Read(), 0f);
        }

        public Vector3 ReadXZ()
        {
            return new Vector3(Read(), 0f, Read());
        }

        public Vector3 ReadYZ()
        {
            return new Vector3(0f, Read(), Read());
        }

        public static float[] ReadBinary(string fn, int size)
        {
            if (File.Exists(fn))
            {
                float[] buffer = new float[size];
                BinaryReader reader = new BinaryReader(File.Open(fn, FileMode.Open));
                for (int i = 0; i < size; i++)
                {
                    try
                    {
                        buffer[i] = reader.ReadSingle();
                    }
                    catch
                    {
                        Debug.Log("There were errors reading file at path " + fn + ".");
                        reader.Close();
                        return null;
                    }
                }

                reader.Close();
                return buffer;
            }
            else
            {
                Debug.Log("File at path " + fn + " does not exist.");
                return null;
            }
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NeuralNetwork), true)] //https://docs.unity3d.com/2020.3/Documentation/ScriptReference/CustomEditor-ctor.html
    public class NeuralNetworkEditor : Editor
    {
        public NeuralNetwork Target;

        private void Awake()
        {
            Target = (NeuralNetwork)target;
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(Target, Target.name); // not sure what this does file:///C:/Program%20Files/Unity/Hub/Editor/2020.3.29f1/Editor/Data/Documentation/en/ScriptReference/Undo.RecordObject.html

            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Prediction time: " + 1000f * Target.PredictionTime + "ms", MessageType.None);

            if (GUI.changed) // not sure of this loop's purpose
            {
                EditorUtility.SetDirty(Target);
            }
        }
    }
# endif
}

