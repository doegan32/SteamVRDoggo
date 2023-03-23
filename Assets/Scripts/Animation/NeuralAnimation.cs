using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepLearning;

#if UNITY_EDITOR // what happens if I get rid of this?
using UnityEditor;
#endif

public abstract class NeuralAnimation : MonoBehaviour
    // this is a base script for animation a character using a neural network
    // all demos for doing such should derive from this 
{
    public enum FPS { Eighteen, Twenty, Thirty, Sixty};

    public NeuralNetwork NeuralNetwork = null;
    public Actor Actor = null;

    public float InferenceTime { get; private set; }
    public FPS FrameRate = FPS.Twenty;

    protected abstract void Setup();
    protected abstract void Feed();
    protected abstract void Read();
    protected abstract void OnGUIDerived();
    protected abstract void OnRenderObjectDerived();


    private void Reset()
    {
        NeuralNetwork = GetComponent<NeuralNetwork>();
        Actor = GetComponent<Actor>();

    }


    private void Start()
    {
        Setup();

        Time.fixedDeltaTime = 1.0f / GetFrameRate();
    }

    private void FixedUpdate()
    {
        System.DateTime t = Utility.GetTimestamp();

        Time.fixedDeltaTime = 1.0f / GetFrameRate();

        if (NeuralNetwork != null && NeuralNetwork.Setup)
        {
            NeuralNetwork.ResetPivot();
            Feed();
            NeuralNetwork.Predict();
            NeuralNetwork.ResetPivot();
            Read();
        }
        InferenceTime = (float)Utility.GetElapsedTime(t);
    }

    private void Update()
    {
        //Utility.SetFPS(Mathf.RoundToInt(90.0f));
        
    }

    //private void LateUpdate() // Starke uses Updata? Does it matter?
    //{
    //    System.DateTime t = Utility.GetTimestamp();
    //    Utility.SetFPS(Mathf.RoundToInt(GetFrameRate()));
    //    if (NeuralNetwork != null && NeuralNetwork.Setup)
    //    {
    //        NeuralNetwork.ResetPivot();
    //        Feed();
    //        NeuralNetwork.Predict();
    //        NeuralNetwork.ResetPivot();
    //        Read();
    //    }
    //    InferenceTime = (float)Utility.GetElapsedTime(t);
    //}


    public float GetFrameRate()
    {
        switch (FrameRate)
        {
            case FPS.Eighteen:
                return 18.0f;
            case FPS.Twenty:
                return 20.0f;
            case FPS.Thirty:
                return 30.0f;
            case FPS.Sixty:
                return 60.0f;
        }
        return 20.0f;
    }

    private void OnGUI()
    {
        if (NeuralNetwork != null && NeuralNetwork.Setup)
        {
            OnGUIDerived();
        }
    }

    private void OnRenderObject()
    {
        if (NeuralNetwork != null && NeuralNetwork.Setup && Application.isPlaying)
        {
            OnRenderObjectDerived();
        }
    }


# if UNITY_EDITOR
    [CustomEditor(typeof(NeuralAnimation), true)] //https://docs.unity3d.com/2020.3/Documentation/ScriptReference/CustomEditor-ctor.html
    public class NeuralAnimationEditor : Editor
    {
        public NeuralAnimation Target;

        private void Awake()
        {
            Target = (NeuralAnimation)target;
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(Target, Target.name); // not sure what this does file:///C:/Program%20Files/Unity/Hub/Editor/2020.3.29f1/Editor/Data/Documentation/en/ScriptReference/Undo.RecordObject.html

            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Inference time: " + 1000f*Target.InferenceTime + "ms", MessageType.None);

       

            if (GUI.changed) // not sure of this loop's purpose
            {
                EditorUtility.SetDirty(Target);
            }
        }
    }
# endif



}
