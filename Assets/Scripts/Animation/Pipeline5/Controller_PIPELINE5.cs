using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEngine.InputSystem;
//using UnityEngine.XR.OpenXR.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class Controller_PIPELINE5 : MonoBehaviour
{
	public bool Inspect = false;

	public Logic[] Logics = new Logic[0];
	public Value[] Values = new Value[0];
	public Function[] Functions = new Function[0];

    //[SerializeField]
    //private InputActionReference inputVelocity;//= null;
    //[SerializeField]
    //private InputActionReference inputOrientation;// = null;
    //[SerializeField]
    //private InputActionReference sitButton;//= null;
    //[SerializeField]
    //private InputActionReference lieButton;// = null;
    //[SerializeField]
    //private InputActionReference standButton;// = null;
    //[SerializeField]
    //private InputActionReference lcPosition;// = null;
    //[SerializeField]
    //private InputActionReference rcPosition;// = null;
    //[SerializeField]
    //private InputActionReference hmdPosition;// = null;

    //private float isSitting = 0.0f;
    //private float isLieing = 0.0f;
    //private float isStanding = 0.0f;
    //private float hipHeight;
    //private float hmdHeight;


    //private float fixedDeltaTime = 1.0f / 60.0f;
    //private Vector3 inVelocity = Vector3.zero;
    //private Vector3 localInVelocity = Vector3.zero;
    //private Vector3 localTargetVelocity = Vector3.zero;
    //private Quaternion globalRotation = Quaternion.identity;

    //private Vector3 position = Vector3.zero;
    //private Vector3 velocity = Vector3.zero;
    //private Vector3 acceleration = Vector3.zero;
    //private Vector3 targetVelocity = Vector3.zero;
    //[SerializeField]
    //[Range(0.0f, 5.0f)]
    //private float velocityHalflife = 0.2f;

    //private Vector3 direction = Vector3.forward;
    //private Vector3 directionVelocity = Vector3.zero;
    //private Vector3 targetDirection = Vector3.zero;
    //[SerializeField]
    //[Range(0.0f, 5.0f)]
    //private float directionHalfLife = 0.2f;

    //[SerializeField]
    //[Range(0.0f, 10.0f)]
    //private float maxForwardIn = 4.0f;
    //[SerializeField]
    //[Range(0.0f, 10.0f)]
    //private float maxBackwardIn = 4.0f;
    //[SerializeField]
    //[Range(0.0f, 10.0f)]
    //private float maxSidewaysIn = 4.0f;

    //[SerializeField]
    //[Range(0.0f, 10.0f)]
    //private float maxForwardOut = 2.0f;
    //[SerializeField]
    //[Range(0.0f, 10.0f)]
    //private float maxBackwardOut = 2.0f;
    //[SerializeField]
    //[Range(0.0f, 10.0f)]
    //private float maxSidewaysOut = 2.0f;


    //public string PATH;
    //string assetName;

    //public bool Mirror = false;
    //public MotionData Data = null;
    //private Actor Actor = null;
    //private float Timestamp = 0f;
    //private int[] BoneMapping = null;

    //public int f = 0; // temp switch from 1
    //public int F = 1;


    //public Vector3 LFvelocity;
    //public Vector3 RFvelocity;

    //public float[][] TestSignal;


    //Vector3 LastLFPosition = Vector3.zero;
    //Vector3 LFPosition = Vector3.zero;
    //Vector3 LFVelocity = Vector3.zero;
    //Vector3 LastRFPosition = Vector3.zero;
    //Vector3 RFPosition = Vector3.zero;
    //Vector3 RFVelocity = Vector3.zero;

    [Header("Joints to track")]
    public Transform LeftFoot;
    public Transform RightFoot;
	public Transform Pelvis;

	[Header("Contact thresholds")]
	[Range(0.0f, 2.0f)]
	public float VelocityThreshold = 0.05f;
	[Range(0.0f, 2.0f)]
	public float LFPositionTheshold = 0.0f;
	[SerializeField]
	private bool LFThresholdSet = false;
	[Range(0.0f, 2.0f)]
	public float RFPositionTheshold = 0.0f;
	[SerializeField]
	private bool RFThresholdSet = false;
	[Range(0.0f, 5.0f)]
	public float FeetThresholdMultiplier = 1.05f;
	


	[Header("Action control")]
	//public Transform Neck;
	//public Transform Pelvis;
	[Range(0.0f, 2.0f)]
	public float PelvisPositionThreshold = 1.0f;
	[SerializeField]
	private bool PelvisThresholdSet = false;
	[Range(0.0f, 5.0f)]
	public float PelvisThresholdMultiplier = 0.9f;
	[Range(0.0f, 2.0f)]
	public float NeckPositionThreshold = 1.0f;
	[SerializeField]
	private bool NeckThresholdSet = false;
	[Range(0.0f, 5.0f)]
	public float NeckThresholdMultiplier = 0.9f;



	private Vector3 LeftFootPosition;
	private Vector3 LastLeftFootPosition;
	private Vector3 LeftFootGlobalVelocity;
	//private Vector3 LeftFootLocalVelocity;
	private Vector3 RightFootPosition;
	private Vector3 LastRightFootPosition;
	private Vector3 RightFootGlobalVelocity;
	//private Vector3 LeftFootLocalVelocity;

	private Vector3 PelvisPosition;
	private Vector3 LastPelvisPosition;
	private Vector3 PelvisGlobalVelocity;
	private Vector3 NeckPosition;


	private Vector3 PelvisForwardDirection = Vector3.forward;

	private float dt;

	//[Header("VR control")]
	//[SerializeField]
	//private InputActionReference LeftControllerPosition;
	//[SerializeField]
	//private InputActionReference RightControllerPosition;
	//[SerializeField]
	//private InputActionReference HMDPosition;// = null;
	//[SerializeField]
	//private InputActionReference HMDOrientation;// = null;

	private float HMDHeight;


	public void Setup(float deltatime)
	{
		//Actor = GetComponent<Actor>();
		dt = deltatime;

		// set up thresholds for body parts
		LFPositionTheshold = 0.0f;
		RFPositionTheshold = 0.0f;
		FeetThresholdMultiplier = 1.05f; //  remove if I identify perfect value

		PelvisPositionThreshold = 1.0f;
		PelvisThresholdMultiplier = 0.7f;  //  remove if I identify perfect value


		LeftFootPosition = LeftFoot.position;
        LastLeftFootPosition = LeftFoot.position;
        LeftFootGlobalVelocity = Vector3.zero;

        LastRightFootPosition = RightFoot.position;
        RightFootPosition = RightFoot.position;
        RightFootGlobalVelocity = Vector3.zero;

        PelvisPosition = Pelvis.position;
        LastPelvisPosition = Pelvis.position;
        PelvisGlobalVelocity = Vector3.zero;
        PelvisForwardDirection = Vector3.ProjectOnPlane(Pelvis.forward, Vector3.up);
	}


	// https://learn.microsoft.com/en-us/dotnet/api/system.func-1?view=net-7.0
	public class Logic
	{
		public string Name = string.Empty;
		public Func<bool> Query = () => false;
		public Logic(string name, Func<bool> func)
		{
			Name = name;
			Query = func;
		}
	}

	public class Value
	{
		public string Name = string.Empty;
		public Func<float> Query = () => 0f;
		public Value(string name, Func<float> func)
		{
			Name = name;
			Query = func;
		}
	}

	public class Function
	{
		public string Name = string.Empty;
		public Func<float, float> Query = (x) => 0f;
		public Function(string name, Func<float, float> func)
		{
			Name = name;
			Query = func;
		}
	}

    private void Update()
    {
		//Debug.Log(HMDPosition.action.ReadValue<Vector3>());
    }

    public void UpdateController()
	{

		// Create user based thresholds
		if (!LFThresholdSet )
        {
			LFPositionTheshold = LeftFoot.position.y * FeetThresholdMultiplier;
			if (LFPositionTheshold > 0.0f)
            {
				LFThresholdSet = true;
			}
		}
		if (!RFThresholdSet)
		{
			RFPositionTheshold = RightFoot.position.y * FeetThresholdMultiplier;
			if (RFPositionTheshold > 0.0f)
			{
				RFThresholdSet = true;
			}

		}
		if (!PelvisThresholdSet)
		{
			PelvisPositionThreshold = Pelvis.position.y * PelvisThresholdMultiplier;
			if (PelvisPositionThreshold != 1.0f && PelvisPositionThreshold != 0.0f)
			{
				PelvisThresholdSet = true;
			}

		}


		LastLeftFootPosition = LeftFootPosition;
        LeftFootPosition = LeftFoot.position;
        LeftFootGlobalVelocity = (LeftFootPosition - LastLeftFootPosition) / dt;

        LastRightFootPosition = RightFootPosition;
        RightFootPosition = RightFoot.position;
        RightFootGlobalVelocity = (RightFootPosition - LastRightFootPosition) / dt;

        PelvisForwardDirection = Vector3.ProjectOnPlane(Pelvis.forward, Vector3.up);
		LastPelvisPosition = PelvisPosition;
		PelvisPosition = Pelvis.position;
		PelvisGlobalVelocity = Vector3.ProjectOnPlane((PelvisPosition - LastPelvisPosition) / dt, Vector3.up);


		////HMDHeight = HMDPosition.action.ReadValue<Vector3>().y;
	}


	public Logic AddLogic(string name, Func<bool> func)
	{
		Logic item = System.Array.Find(Logics, x => x.Name == name);
		if (item != null)
		{
			Debug.Log("Logic with name " + name + " already contained.");
		}
		else
		{
			item = new Logic(name, func);
			ArrayExtensions.Append(ref Logics, item);
		}
		return item;
	}

	public bool QueryLogic(string name)
	{
		Logic item = System.Array.Find(Logics, x => x.Name == name);
		if (item == null)
		{
			Debug.Log("Logic with name " + name + " could not be found.");
			return false;
		}
		return item.Query();
	}

	public bool[] QueryLogics(string[] names)
	{
		bool[] items = new bool[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			items[i] = QueryLogic(names[i]);
		}
		return items;
	}

	public float[] PoolLogics(string[] names)
	{
		float[] items = new float[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			items[i] = QueryLogic(names[i]) ? 1f : 0f;
		}
		return items;
	}

	public Value AddValue(string name, Func<float> func)
	{
		Value value = System.Array.Find(Values, x => x.Name == name);
		if (value != null)
		{
			Debug.Log("Value with name " + name + " already contained.");
		}
		else
		{
			value = new Value(name, func);
			ArrayExtensions.Append(ref Values, value);
		}
		return value;
	}

	public float QueryValue(string name)
	{
		Value value = System.Array.Find(Values, x => x.Name == name);
		if (value == null)
		{
			Debug.Log("Value with name " + name + " could not be found.");
			return 0f;
		}
		return value.Query();
	}

	public float[] QueryValues(string[] names)
	{
		float[] items = new float[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			items[i] = QueryValue(names[i]);
		}
		return items;
	}

	public float[] PoolValues(string[] names)
	{
		float[] items = new float[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			items[i] = QueryValue(names[i]);
		}
		return items;
	}

	public Function AddFunction(string name, Func<float, float> func)
	{
		Function function = System.Array.Find(Functions, x => x.Name == name);
		if (function != null)
		{
			Debug.Log("Function with name " + name + " already contained.");
		}
		else
		{
			function = new Function(name, func);
			ArrayExtensions.Append(ref Functions, function);
		}
		return function;
	}

	public float QueryFunction(string name, float arg)
	{
		Function function = System.Array.Find(Functions, x => x.Name == name);
		if (function == null)
		{
			Debug.Log("Function with name " + name + " could not be found.");
			return 0f;
		}
		return function.Query(arg);
	}



	public Vector3 QueryTargetVelocity()
	{
		return Vector3.zero;// PelvisGlobalVelocity;
	}
	public Vector3 QueryTargetDirection()
	{
		return PelvisForwardDirection; //
	}

	public Vector3 QueryTargetLFVelocity()
	{
		return LeftFootGlobalVelocity;
	}

	public Vector3 QueryTargetRFVelocity()
	{
		return RightFootGlobalVelocity;
	}

	public float QueryLFContact()
	{
		if (LeftFootPosition.y < LFPositionTheshold)  //((LeftFootGlobalVelocity.magnitude < VelocityThreshold)) // (LeftFootPosition.y < PositionTheshold) && (
		{
			return 1.0f;
		}
        else
        {
			return 0.0f;
        }
	}

	public float QueryRFContact()
	{
		if ( RightFootPosition.y < RFPositionTheshold) //( (RightFootGlobalVelocity.magnitude < VelocityThreshold)) // (RightFootPosition.y < PositionTheshold) &&
		{
			return 1.0f;
		}
		else
		{
			return 0.0f;
		}
	}

	public float QuerySit()
	{
        //if (Pelvis.position.y < PelvisThreshold && !(Neck.position.y < NeckThreshold))
        if (Pelvis.position.y < PelvisPositionThreshold)
        //if (HMDHeight < PelvisThreshold)
        {
            return 1.0f;
        }
        else
        {
            return 0.0f;
        }
    }

	public float QueryLie()
	{
		//if (Pelvis.position.y < PelvisThreshold && Neck.position.y < NeckThreshold)
		//{
		//	return 1.0f;
		//}
		//else
		//{
			return 0.0f;
		//}
	}

	public float QueryStand()
	{
		//if (Pelvis.position.y < PelvisThreshold && !(Neck.position.y < NeckThreshold))
		//{
		//	return 1.0f;
		//}
  //      else
  //      {
			return 0.0f;
		//}
	}

}
