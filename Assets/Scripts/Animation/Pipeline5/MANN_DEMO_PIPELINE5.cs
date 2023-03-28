using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepLearning;

public class MANN_DEMO_PIPELINE5 : NeuralAnimation
{
    public Camera Camera = null;

    // Controller stuff as per Starke - possibly over complicated for what I'm doing
    private Controller_PIPELINE5 Controller;
    [Header("Controller Stuff")]
    public float PositionBias = 0.025f;
    public float RotationBias = 0.05f;
    public float VelocityBias = 0.1f;
    public float ActionBias = 0.25f;
    public float CorrectionBias = 0.25f;
    public float ControlStrength = 0.1f;
    public float CorrectionStrength = 1f;

    // Time series stuff
    private TimeSeries TimeSeries;
    private TimeSeries TimeSeriesPast;
    [Header("TimeSeries Stuff")]
    public float PastWindow = 3.0f;
    public float FutureWindow = 3.0f;
    public int PastKeys = 6;
    public int FutureKeys = 6;

    private RootSeries RootSeries;
    private StyleSeries StyleSeries;
    private ContactSeries ControlContactSeries;
    private RootSeries LFSeries;
    private RootSeries RFSeries;
  

    // Control targets received from human
    float LFContact = 1.0f;
    float RFContact = 1.0f;
    Vector3 TargetLFLocalVelocity = Vector3.zero;
    Vector3 TargetRFLocalVelocity = Vector3.zero;
    Vector3 TargetLFGlobalVelocity = Vector3.zero;
    Vector3 TargetRFGlobalVelocity = Vector3.zero;
    Vector3 TargetDirection = Vector3.forward;
    Quaternion TargetRotation = Quaternion.identity; // might need to change intial value?
    Quaternion InverseTargetRotation = Quaternion.identity; // might need to change intial value?

    Vector3 SmoothedTargetLFLocalVelocity = Vector3.zero;
    Vector3 SmoothedTargetLFLocalVelocityAcceleration = Vector3.zero;
    Vector3 SmoothedTargetRFLocalVelocity = Vector3.zero;
    Vector3 SmoothedTargetRFLocalVelocityAcceleration = Vector3.zero;

    // Root velocity prediction
    [Header("Root velocity prediction")]
    [SerializeField]
    private NeuralNetwork RootVelPredictionNet;
    [SerializeField]
    private Vector3 TargetLocalRootVelocity = Vector3.zero;
    private Vector3 TargetGlobalRootVelocity = Vector3.zero;
    private RootSeries TargetLocalRootVelocitySeries;

    // smoothing/damping stuff? Is this even necessary?
    [Header("Smoothing and scaling control signals")]
    [Range(0.0f, 2.0f)]
    public float HalfLifeDirection = 0.2f;
    [Range(0.0f, 2.0f)]
    public float HalfLifeVelocity = 0.2f;
    [Range(0.0f, 2.0f)]
    public float HalfLifeFeetVelocity = 0.2f;
    private float dt;
    private Vector3 SmoothedTargetDirection = Vector3.forward;
    private Vector3 SmoothedTargetDirectionVelocity = Vector3.zero;
    private Vector3 SmoothedTargetGlobalRootVelocity= Vector3.zero;
    private Vector3 SmoothedTargetGlobalRootVelocityAcceleration = Vector3.zero;

    [Range(0.0f, 10.0f)]
    public float ForwardScaling = 3.0f;
    [Range(0.0f, 10.0f)]
    public float BackwardScaling = 0.1f;
    [Range(0.0f, 10.0f)]
    public float SidewaysScaling = 0.1f;

    // determining actions
    [Header("Action/Style")]
    [Range(0.0f, 5.0f)]
    public float MoveThreshold = 1.0f / 3.0f;
    private float idle = 1.0f;
    private float move = 0.0f;
    private float isSitting = 0.0f;
    private float isLieing = 0.0f;
    private float isStanding = 0.0f;



    private UltimateIK.Model LeftHandIK;
    private UltimateIK.Model RightHandIK;

    private Camera GetCamera()
    {
        return Camera == null ? Camera.main : Camera;
    }

    protected override void Setup()
    {
        dt = 1.0f / GetFrameRate();
        Controller = GetComponent<Controller_PIPELINE5>();//new ControllerVR3();
        Controller.Setup(dt);

        // Need to set these up for each possible action
        // corrections go from CorrectionStrength to 0.0 as x goes from Pivot to TimeSeries.Samples - meaning we fully use NN output for current timestep and gradually leave as is in the future
        // Controls go from 0.1 to ControlStrength as x goes from Pivot to TimeSeries.Samples - meaning the input control strength gets stronger the further we go into the future
        Controller_PIPELINE5.Logic idle = Controller.AddLogic("idle", () => Controller.QueryTargetVelocity().magnitude < 0.01f); /////////////////////////////////////////////////////////////////////////////////// Need to check this velocity. In training it's like 1/3 I think - will depend on output of PreNet
        Controller_PIPELINE5.Function idleControl = Controller.AddFunction("idleControl", (x) => TimeSeries.GetControl((int)x, ActionBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function idleCorrection = Controller.AddFunction("idleCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));
        Controller_PIPELINE5.Logic move = Controller.AddLogic("move", () => !idle.Query());
        Controller_PIPELINE5.Function moveControl = Controller.AddFunction("moveControl", (x) => TimeSeries.GetControl((int)x, ActionBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function moveCorrection = Controller.AddFunction("moveCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));
        //Controller_PIPELINE5.Logic sit = Controller.AddLogic("sit", () => Controller.QuerySit());
        Controller_PIPELINE5.Function sitControl = Controller.AddFunction("sitControl", (x) => TimeSeries.GetControl((int)x, ActionBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function sitCorrection = Controller.AddFunction("sitCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));
        // Controller_PIPELINE5.Logic lie = Controller.AddLogic("lie", () => Controller.QueryLie());
        Controller_PIPELINE5.Function lieControl = Controller.AddFunction("lieControl", (x) => TimeSeries.GetControl((int)x, ActionBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function lieCorrection = Controller.AddFunction("lieCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));
        //Controller_PIPELINE5.Logic stand = Controller.AddLogic("stand", () => Controller.QueryStand());
        Controller_PIPELINE5.Function standControl = Controller.AddFunction("standControl", (x) => TimeSeries.GetControl((int)x, ActionBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function standCorrection = Controller.AddFunction("standCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));


        // need to set these up for each element of RootSeries
        Controller_PIPELINE5.Function rootPositionControl = Controller.AddFunction("rootPositionControl", (x) => TimeSeries.GetControl((int)x, PositionBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function rootPositionCorrection = Controller.AddFunction("rootPositionCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));
        Controller_PIPELINE5.Function rootRotionControl = Controller.AddFunction("rootRotationControl", (x) => TimeSeries.GetControl((int)x, RotationBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function rootRotionCorrection = Controller.AddFunction("rootRotationCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));
        Controller_PIPELINE5.Function rootVelocityControl = Controller.AddFunction("rootVelocityControl", (x) => TimeSeries.GetControl((int)x, VelocityBias, 0.1f, ControlStrength));
        Controller_PIPELINE5.Function rootVelocityCorrection = Controller.AddFunction("rootVelocityCorrection", (x) => TimeSeries.GetCorrection((int)x, CorrectionBias, CorrectionStrength, 0.0f));

        // Set up series stuff
        int resolution =(int)((PastWindow * GetFrameRate())/PastKeys);
        TimeSeries = new TimeSeries(PastKeys, FutureKeys, PastWindow, FutureWindow, resolution); // this is past and future time series
        TimeSeriesPast = new TimeSeries(PastKeys, 0, PastWindow, 0, resolution);
        RootSeries = new RootSeries(TimeSeries, transform);
        StyleSeries = new StyleSeries(TimeSeries, new string[] { "idle", "move", "sit", "lie", "stand"}, new float[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f }); //need to confirm what's best here
        //StyleSeries = new StyleSeries(TimeSeries, new string[] { "idle", "move", "sit", "lie", "stand", "jump" }, new float[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f });
        ControlContactSeries = new ContactSeries(TimeSeriesPast, new string[] { "FrontLeftPaw", "FrontRightPaw" });
        LFSeries = new RootSeries(TimeSeriesPast); // we don't care about positions and velocities are intialised to zero so these are grand as is?
        RFSeries = new RootSeries(TimeSeriesPast);
        TargetLocalRootVelocitySeries = new RootSeries(TimeSeriesPast);
        // initialize contacts to be one, coincided with initialising action to be idle
        for (int i = 0; i < ControlContactSeries.SampleCount; i++)
        {
            for (int j = 0; j < ControlContactSeries.Bones.Length; j++)
            {
                ControlContactSeries.Values[i][j] = 1.0f;
            }
        }

        RootVelPredictionNet = GetComponent<Feet2Rootv0>(); // Will clean up and replace Feet2Rootv0 with a new script



        //NetworkSeries = new RootSeries(timeSeries);
        //for (int i = 0; i < timeSeries.SampleCount; i++)
        //{
        //    NetworkSeries.SetDirection(i, Vector3.ProjectOnPlane(Vector3.forward, Vector3.up));
        //    NetworkSeries.SetPosition(i, Vector3.zero);
        //    NetworkSeries.SetVelocity(i, Vector3.zero);
        //}




        //RootSeries.DrawScene = DrawTrajectory;
        //FrontPawContacts.DrawGUI = true;

        ////////////////// swap back in for human bvh input
        //// lazy fix for testing action input
        ////HumanInput = GameObject.Find("InputHuman");
        ////InputPlayer = HumanInput.GetComponent<PlayBVH_H>();


        //// implements two bone CCD to correct foot sliding - need to output contacts are calculare whether foot is in contact to use this
        //// if foot is in contact, the TargetPosition and Orientation is fixed until no longer in contact
        //LeftHandIK = UltimateIK.BuildModel(Actor.FindTransform("LeftForeArm"), Actor.FindTransform("LeftHandSite"));
        //RightHandIK = UltimateIK.BuildModel(Actor.FindTransform("RightForeArm"), Actor.FindTransform("RightHandSite"));



        //timeSeries = new TimeSeries(pastKeys, futureKeys, pastWindow, futureWindow, resolution);
        //timeSeriesPast = new TimeSeries(pastKeys, 0, pastWindow, 0.0f, resolution);
        //rootSeriesTarget = new RootSeries(timeSeries);
        //leftFootSeriesTarget = new RootSeries(timeSeriesPast);
        //rightFootSeriesTarget = new RootSeries(timeSeriesPast);
        ////for (int i = 0; i < timeSeries.SampleCount; i++)
        ////{
        ////    rootSeriesTarget.SetDirection(i, Vector3.ProjectOnPlane(inputRoot.forward, Vector3.up));
        ////    rootSeriesTarget.SetPosition(i, Vector3.ProjectOnPlane(inputRoot.position, Vector3.up));
        ////    rootSeriesTarget.SetVelocity(i, Vector3.zero);
        ////}
        //for (int i = 0; i < timeSeriesPast.SampleCount; i++)
        //{
        //    //rootSeriesTarget.SetDirection(i, Vector3.ProjectOnPlane(inputRoot.forward, Vector3.up));
        //    //rootSeriesTarget.SetPosition(i, Vector3.ProjectOnPlane(inputRoot.position, Vector3.up));
        //    //rootSeriesTarget.SetVelocity(i, Vector3.zero);

        //    leftFootSeriesTarget.SetDirection(i, inputLF.forward);
        //    leftFootSeriesTarget.SetPosition(i, inputLF.position);
        //    leftFootSeriesTarget.SetVelocity(i, Vector3.zero);

        //    rightFootSeriesTarget.SetDirection(i, inputRF.forward);
        //    rightFootSeriesTarget.SetPosition(i, inputRF.position);
        //    rightFootSeriesTarget.SetVelocity(i, Vector3.zero);
        //}

        //inputRootPosition = inputRoot.position;
        //inputLFPosition = inputLF.position;
        //inputRFPosition = inputRF.position;

        //fps = (int)((float)(pastKeys * resolution) / pastWindow);
        //dt = 1.0f / (float)fps;
        ////Time.fixedDeltaTime = dt;

        //RootPredictor = GetComponent<Feet2Rootv0>();
        //NetworkSeries = new RootSeries(timeSeries);
        //for (int i = 0; i < timeSeries.SampleCount; i++)
        //{
        //    NetworkSeries.SetDirection(i, Vector3.ProjectOnPlane(Vector3.forward, Vector3.up));
        //    NetworkSeries.SetPosition(i, Vector3.zero);
        //    NetworkSeries.SetVelocity(i, Vector3.zero);
        //}






        //Feet2RootNet = GetComponent<Feet2Rootv0>();





        //// Trying to follow bvh
        //LeftToeVel = Vector3.zero;
        //RightToeVel = Vector3.zero;
        //LastLeftToePos = LeftToeTarget.position;
        //LastRightToePos = RightToeTarget.position;
        //Across = LS.position - RS.position;
        //targetDirection = -Vector3.ProjectOnPlane(Vector3.Cross(Vector3.up, Across), Vector3.up).normalized;

    }

    protected override void Feed()
    {
        Control();

        // Get Root - we are feeding to Network for pose prediction at time t
        // thus this is the root transform at time t-1
        Matrix4x4 root = Actor.GetRoot().GetWorldMatrix(true);

        // we've rolled back trajectory
        // thus these are past and predicted trajectory centred at time t
        // relative to root at t-1
        for (int i = 0; i < TimeSeries.KeyCount; i++)
        {
            int index = TimeSeries.GetKey(i).Index;

            NeuralNetwork.FeedXZ(RootSeries.GetPosition(index).GetRelativePositionTo(root));
            NeuralNetwork.FeedXZ(RootSeries.GetDirection(index).GetRelativeDirectionTo(root));
            NeuralNetwork.FeedXZ(RootSeries.Velocities[index].GetRelativeDirectionTo(root));
            NeuralNetwork.Feed(StyleSeries.Values[index]);
        }

        // Feed in desired FrontFeetContacts and Velocities
        for (int i = 0; i <= TimeSeriesPast.PivotKey; i++)
        {
            // feed in FL & FR ground contacts
            // and velocities relative to roots in each frame
            int index = TimeSeriesPast.GetKey(i).Index;
            NeuralNetwork.Feed(ControlContactSeries.Values[index]); // LF, RF
            NeuralNetwork.Feed(LFSeries.Velocities[index]); // LF
            NeuralNetwork.Feed(RFSeries.Velocities[index]);  // RF
        }

        for (int i = 0; i < Actor.Bones.Length; i++)
        {
            NeuralNetwork.Feed(Actor.Bones[i].Transform.position.GetRelativePositionTo(root));
            NeuralNetwork.Feed(Actor.Bones[i].Transform.forward.GetRelativeDirectionTo(root));
            NeuralNetwork.Feed(Actor.Bones[i].Transform.up.GetRelativeDirectionTo(root));
            NeuralNetwork.Feed(Actor.Bones[i].Velocity.GetRelativeDirectionTo(root));
        }
    }

    protected override void Read()
    {
        Vector3 xz_vel = NeuralNetwork.ReadXZ();
        float a_vel = NeuralNetwork.Read(); // angular velocity into current frame i

        // Starke Lerps this towards Zero via the StyleSeries[TimeSeries.Pivot]["Idle"] value
        // I guess the thinking is that if "idle" is 1, then the root shoudln't update
        // and if "idle" is 0, then the Lerp won't have any effect. . .  . . . . . . but what if human does something "undoglike" like turning on the spot??
        // and should "idle" be based of net prediction or human input?
        //Debug.Log("idle : " + StyleSeries.Values[TimeSeries.Pivot][0].ToString());
        //Debug.Log("move : " + StyleSeries.Values[TimeSeries.Pivot][1].ToString());
        //Debug.Log("sit : " + StyleSeries.Values[TimeSeries.Pivot][2].ToString());
        //Debug.Log("lie : " + StyleSeries.Values[TimeSeries.Pivot][3].ToString());
        //Debug.Log("stand : " + StyleSeries.Values[TimeSeries.Pivot][4].ToString());
        xz_vel = Vector3.Lerp(xz_vel, Vector3.zero, StyleSeries.GetStyle(TimeSeries.Pivot, "idle"));
        a_vel = Mathf.Lerp(a_vel, 0.0f, StyleSeries.GetStyle(TimeSeries.Pivot, "idle"));

        Matrix4x4 root = Actor.GetRoot().GetWorldMatrix(true) * Matrix4x4.TRS(xz_vel, Quaternion.AngleAxis(Mathf.Rad2Deg * a_vel, Vector3.up), Vector3.one);
        RootSeries.Transformations[TimeSeries.Pivot] = root;

        // next line is incorrect? Seems my net should be predicting a velocity as well as in LCP? Check what MANN does?
        RootSeries.Velocities[TimeSeries.Pivot] = xz_vel.GetRelativeDirectionFrom(root) * GetFrameRate();

        //// Need to insert Idle and Move here 
        StyleSeries.Values[TimeSeries.Pivot][0] = (xz_vel.GetRelativeDirectionFrom(root).ZeroY().magnitude * GetFrameRate()) < MoveThreshold ? 1.0f : 0.0f;
        StyleSeries.Values[TimeSeries.Pivot][1] = (xz_vel.GetRelativeDirectionFrom(root).ZeroY().magnitude * GetFrameRate()) < MoveThreshold ? 0.0f : 1.0f;
        //Debug.Log("idle " + StyleSeries.Values[TimeSeries.Pivot][0].ToString());
        //Debug.Log(xz_vel.ToString());
        //Debug.Log(xz_vel.GetRelativeDirectionFrom(root).ZeroY().magnitude * GetFrameRate());


        // update actions for Pivot, based on NN output
        for (int j = 2; j < StyleSeries.Styles.Length; j++) // need to fix this Lerp to be weighted properly
        //for (int j = 0; j < StyleSeries.Styles.Length; j++)
        {
            StyleSeries.Values[TimeSeries.Pivot][j] = Mathf.Lerp(
                StyleSeries.Values[TimeSeries.Pivot][j],
                NeuralNetwork.Read(),
                0.5f//Controller.QueryFunction(StyleSeries.Styles[j] + "Correction", TimeSeries.Pivot)
                );
        }

        // read future states
        for (int i = TimeSeries.PivotKey + 1; i < TimeSeries.KeyCount; i++)
        {
            int index = TimeSeries.GetKey(i).Index;

            Matrix4x4 m = Matrix4x4.TRS(NeuralNetwork.ReadXZ().GetRelativePositionFrom(root), Quaternion.LookRotation(NeuralNetwork.ReadXZ().GetRelativeDirectionFrom(root).normalized, Vector3.up), Vector3.one);
            RootSeries.Transformations[index] = Utility.Interpolate(
                RootSeries.Transformations[index],
                m,
                Controller.QueryFunction("rootPositionCorrection", index),
                Controller.QueryFunction("rootRotationCorrection", index)
                );


            Vector3 v = NeuralNetwork.ReadXZ().GetRelativeDirectionFrom(root);
            RootSeries.Velocities[index] = Vector3.Lerp(
                RootSeries.Velocities[index],
                v,
                Controller.QueryFunction("rootVelocityCorrection", index)
                 );

            // Need to insert Idle and Move here 
            StyleSeries.Values[index][0] = (v.ZeroY().magnitude < MoveThreshold) ? 1.0f : 0.0f;
            StyleSeries.Values[index][1] = (v.ZeroY().magnitude < MoveThreshold) ? 0.0f : 1.0f;
            for (int j = 2; j < StyleSeries.Styles.Length; j++)
            //for (int j = 0; j < StyleSeries.Styles.Length; j++)
            {
                StyleSeries.Values[index][j] = Mathf.Lerp(
                    StyleSeries.Values[index][j],
                    NeuralNetwork.Read(),
                    0.5f //Controller.QueryFunction(StyleSeries.Styles[j] + "Correction", index)
                    );
            }
        }

        // Read pose information 
        Vector3[] positions = new Vector3[Actor.Bones.Length];
        Vector3[] forwards = new Vector3[Actor.Bones.Length];
        Vector3[] upwards = new Vector3[Actor.Bones.Length];
        Vector3[] velocities = new Vector3[Actor.Bones.Length];
        //Vector3[] velocitiesLocal = new Vector3[Actor.Bones.Length];
        for (int i = 0; i < Actor.Bones.Length; i++)
        {
            Vector3 position = NeuralNetwork.ReadVector3().GetRelativePositionFrom(root);
            Vector3 forward = NeuralNetwork.ReadVector3().normalized.GetRelativeDirectionFrom(root);
            Vector3 upward = NeuralNetwork.ReadVector3().normalized.GetRelativeDirectionFrom(root);
            Vector3 velocity = NeuralNetwork.ReadVector3().GetRelativeDirectionFrom(root);                        // broke this down to get local velocity for feet control signals
            velocities[i] = velocity;
            positions[i] = Vector3.Lerp(Actor.Bones[i].Transform.position + velocity / GetFrameRate(), position, 0.5f); // BE CAREFUL SHOULDNT HARDCODE THIS GetFrameRate()
            forwards[i] = forward;
            upwards[i] = upward;
        }

        // interpolate Series
        RootSeries.Interpolate(TimeSeries.Pivot, TimeSeries.SampleCount);
        StyleSeries.Interpolate(TimeSeries.Pivot, TimeSeries.SampleCount);

        // assign joint posture
        transform.position = RootSeries.GetPosition(TimeSeries.Pivot);
        transform.rotation = RootSeries.GetRotation(TimeSeries.Pivot);
        for (int i = 0; i < Actor.Bones.Length; i++)
        {
            // Need to add in here how to deal with the neck and head for VR



            if (Actor.Bones[i].GetName() == "Neck" || Actor.Bones[i].GetName() == "Head" || Actor.Bones[i].GetName() == "HeadSite")
            {
                Actor.Bones[i].Velocity = velocities[i];
                //Actor.Bones[i].Transform.position = positions[i];
            }
            else
            {
                Actor.Bones[i].Velocity = velocities[i];
                Actor.Bones[i].Transform.position = positions[i];
                Actor.Bones[i].Transform.rotation = Quaternion.LookRotation(forwards[i], upwards[i]);
            }
            



        }

        // Starke has this? Does it make a noticable difference???
        // correct twist
        // if a joint's rotation does align the bone with the line from it to its child, fix the rotation to achieve this??
        for (int i = 0; i < Actor.Bones.Length; i++)
        {
            if (Actor.Bones[i].Childs.Length == 1)
            {
                Vector3 position = Actor.Bones[i].Transform.position;
                Quaternion rotation = Actor.Bones[i].Transform.rotation;
                Vector3 childPosition = Actor.Bones[i].GetChild(0).Transform.position;
                Quaternion childRotation = Actor.Bones[i].GetChild(0).Transform.rotation;
                Vector3 aligned = (position - childPosition).normalized;
                float[] angles = new float[] {
                    Vector3.Angle(rotation.GetRight(), aligned),
                    Vector3.Angle(rotation.GetUp(), aligned),
                    Vector3.Angle(rotation.GetForward(), aligned),
                    Vector3.Angle(-rotation.GetRight(), aligned),
                    Vector3.Angle(-rotation.GetUp(), aligned),
                    Vector3.Angle(-rotation.GetForward(), aligned)
                };
                float min = angles.Min();
                if (min == angles[0])
                {
                    Actor.Bones[i].Transform.rotation = Quaternion.FromToRotation(rotation.GetRight(), aligned) * rotation;
                }
                if (min == angles[1])
                {
                    Actor.Bones[i].Transform.rotation = Quaternion.FromToRotation(rotation.GetUp(), aligned) * rotation;
                }
                if (min == angles[2])
                {
                    Actor.Bones[i].Transform.rotation = Quaternion.FromToRotation(rotation.GetForward(), aligned) * rotation;
                }
                if (min == angles[3])
                {
                    Actor.Bones[i].Transform.rotation = Quaternion.FromToRotation(-rotation.GetRight(), aligned) * rotation;
                }
                if (min == angles[4])
                {
                    Actor.Bones[i].Transform.rotation = Quaternion.FromToRotation(-rotation.GetRight(), aligned) * rotation;
                }
                if (min == angles[5])
                {
                    Actor.Bones[i].Transform.rotation = Quaternion.FromToRotation(-rotation.GetForward(), aligned) * rotation;
                }
                Actor.Bones[i].GetChild(0).Transform.position = childPosition;
                Actor.Bones[i].GetChild(0).Transform.rotation = childRotation;
            }
        }

        //// Should I add in IK clean up? Maybe I needed to predcit contacts for this???
        //ProcessFootIK(LeftHandIK, ControlContactSeries.Values[TimeSeriesPast.Pivot][0]);
        //ProcessFootIK(RightHandIK, ControlContactSeries.Values[TimeSeriesPast.Pivot][1]);
    }

    private void Control()
    {
        Controller.UpdateController(); // this gets target velocity, input action, feet velocities and contacts, facing direction, etc.

        // roll back
        RootSeries.Increment(0, TimeSeries.SampleCount - 1);
        StyleSeries.Increment(0, TimeSeries.SampleCount - 1);
        ControlContactSeries.Increment(0, TimeSeriesPast.SampleCount - 1);
        LFSeries.Increment(0, TimeSeriesPast.SampleCount - 1);
        RFSeries.Increment(0, TimeSeriesPast.SampleCount - 1);
        TargetLocalRootVelocitySeries.Increment(0, TimeSeriesPast.SampleCount - 1);

        //// Get contacts input
        LFContact = Controller.QueryLFContact();
        RFContact = Controller.QueryRFContact();
        ControlContactSeries.Values[TimeSeriesPast.Pivot][0] = LFContact; // check if these should be oposite due to dog feet seeming to be mixed up?
        ControlContactSeries.Values[TimeSeriesPast.Pivot][1] = RFContact;
        
        // Get feet velocity inputs
        TargetLFGlobalVelocity = Controller.QueryTargetLFVelocity();
        TargetRFGlobalVelocity = Controller.QueryTargetRFVelocity();
        // if desired contacts are 1.0, numb the TargetVelocity of that foot maybe? Can see how that works.
        if (LFContact == 1.0f)
        {
            TargetLFGlobalVelocity = Vector3.zero;
        }
        if (RFContact == 1.0f)
        {
            TargetRFGlobalVelocity = Vector3.zero;
        }
        TargetDirection = Controller.QueryTargetDirection();
        TargetRotation = Quaternion.LookRotation(TargetDirection, Vector3.up);
        InverseTargetRotation = Quaternion.Inverse(TargetRotation);
        //Debug.DrawLine(Actor.GetBonePosition("Hips"), Actor.GetBonePosition("Hips") + TargetDirection * 20.0f, Color.red, 1.0f);

        // not sure if I should be damping these or what values to use?
        TargetLFLocalVelocity = InverseTargetRotation * TargetLFGlobalVelocity * ForwardScaling;
        Springs.CriticalSpringDamper(
            ref SmoothedTargetLFLocalVelocity,
            ref SmoothedTargetLFLocalVelocityAcceleration,
            TargetLFLocalVelocity,
            HalfLifeFeetVelocity,
            dt
           );
        //if (TargetLFLocalVelocity.z >= 0)
        //{
        //    TargetLFLocalVelocity *= ForwardScaling;
        //}
        //else 
        //{
        //    TargetLFLocalVelocity *= BackwardScaling;
        //}
        //TargetLFLocalVelocity.x = 0.0f;
        TargetRFLocalVelocity = InverseTargetRotation * TargetRFGlobalVelocity * ForwardScaling;
        Springs.CriticalSpringDamper(
            ref SmoothedTargetRFLocalVelocity,
            ref SmoothedTargetRFLocalVelocityAcceleration,
            TargetRFLocalVelocity,
            HalfLifeFeetVelocity,
            dt
           );
        //if (TargetRFLocalVelocity.z >= 0)
        //{
        //    TargetRFLocalVelocity *= ForwardScaling;
        //}
        //else
        //{
        //    TargetRFLocalVelocity *= BackwardScaling;
        //}
        //TargetRFLocalVelocity.x = 0.0f;
        // *** N.B Add in forward, backward, and sideways scaling ***
        // should check if smoothing these with springs makes motion more crisp


        // not sure if I want to do this test
        Vector3 HumanGlobalRootVelocity = Controller.QueryTargetVelocity();
        Vector3 HumanLocalRootVelocity = InverseTargetRotation * HumanGlobalRootVelocity;
        if (HumanLocalRootVelocity.z < 0.0f)
        {
            SmoothedTargetLFLocalVelocity = SmoothedTargetLFLocalVelocity * 0.1f;
            SmoothedTargetRFLocalVelocity = SmoothedTargetRFLocalVelocity * 0.1f;
        }




        LFSeries.SetVelocity(TimeSeriesPast.Pivot, SmoothedTargetLFLocalVelocity);
        RFSeries.SetVelocity(TimeSeriesPast.Pivot, SmoothedTargetRFLocalVelocity);

        // Calculate desired root velocity
        TargetLocalRootVelocity = PredictTargetRootVelocity();
        TargetLocalRootVelocitySeries.SetVelocity(TimeSeriesPast.Pivot, TargetLocalRootVelocity);
        TargetGlobalRootVelocity = TargetRotation * TargetLocalRootVelocity;
        //TargetGlobalRootVelocity = Controller.QueryTargetVelocity() * ForwardScaling;


        
        
        //TargetGlobalRootVelocity = Controller.QueryTargetVelocity();

        // Smooth target velocity and target direction
        // should I do this earlier, i.e. add smoothed values to LFSeries, RFSeries, and TargetLocalRootVelocitySeries?
        Springs.CriticalSpringDamper(
            ref SmoothedTargetDirection,
            ref SmoothedTargetDirectionVelocity, 
            TargetDirection, 
            HalfLifeDirection,
            dt
        );
        Springs.CriticalSpringDamper(
            ref SmoothedTargetGlobalRootVelocity,
            ref SmoothedTargetGlobalRootVelocityAcceleration,
            TargetGlobalRootVelocity,
            HalfLifeVelocity,
            dt
           );

        //Vector3 globalTargetVelocity = Quaternion.LookRotation(rootDirection, Vector3.up) * NetworkVelocity;
        //RootSeries.SetVelocity(TimeSeries.Pivot, globalTargetVelocity);
        //RootSeries.SetDirection(TimeSeries.Pivot, rootDirection);
        //RootSeries.SetPosition(TimeSeries.Pivot, RootSeries.GetPosition(TimeSeries.Pivot - 1) + globalTargetVelocity * dt);

        // Not sure I like this as basically means it doesn't predict achieving input for 3 seconds?
        for (int i = TimeSeries.Pivot; i < TimeSeries.SampleCount; i++)
        {
            RootSeries.SetPosition(i,
                Vector3.Lerp(
                    RootSeries.GetPosition(i),
                    Actor.GetRoot().position + i.Ratio(TimeSeries.Pivot, TimeSeries.Samples.Length - 1) * SmoothedTargetGlobalRootVelocity * FutureWindow, // why not using my original spring damper stuff here??
                    1f//Controller.QueryFunction("rootPositionControl", i)
                )
            ); ; ;

            RootSeries.SetRotation(i,
                Quaternion.Slerp(
                    RootSeries.GetRotation(i),
                    Quaternion.LookRotation(SmoothedTargetDirection, Vector3.up),
                    1f//Controller.QueryFunction("rootRotationControl", i)          // this is very different to LMP paper? again why not use my SpringDamper stuff?
                )
            ); ;


            RootSeries.SetVelocity(i,
                Vector3.Lerp(
                    RootSeries.GetVelocity(i),
                    SmoothedTargetGlobalRootVelocity,                                              // is this definitely what we want here?
                    1f//Controller.QueryFunction("rootVelocityControl", i)
                )
            );
        }




        //// Get Action input
        /// calculate idle and move values
        /// 
        idle = (TargetLocalRootVelocity.ZeroY().magnitude < MoveThreshold) ? 1.0f : 0f; // not sure is TargetLocalRootVelocity what I want here?? Should base of human global velocity and if this is <threshold set idle to 1.0
        move = 1.0f - idle;

        isSitting =  Controller.QuerySit();
        isLieing = 0.0f; Controller.QueryLie();
        isStanding = 0.0f; // Controller.QueryStand();

        //if (move == 1.0f)
        //{
        //    isSitting = 0.0f;
        //    isLieing = 0.0f;
        //    isStanding = 0.0f;
        //}
        if (isSitting == 1.0f)
        {
            isLieing = 0.0f;
            isStanding = 0.0f;
            move = 0.0f;
            idle = 1.0f;
        }
        else if(isLieing == 1.0f)
        {
            isSitting = 0.0f;
            isStanding = 0.0f;
            move = 0.0f;
            idle = 1.0f;
        }
        for (int i = TimeSeries.Pivot; i < TimeSeries.SampleCount; i++)
        {

                StyleSeries.Values[i][0] = Mathf.Lerp(
                    StyleSeries.Values[i][0],
                    idle,
                    1f//Controller.QueryFunction(StyleSeries.Styles[j] + "Control", i)
                    );
                StyleSeries.Values[i][1] = Mathf.Lerp(
                    StyleSeries.Values[i][1],
                    move,
                    1f//Controller.QueryFunction(StyleSeries.Styles[j] + "Control", i)
                    );
                StyleSeries.Values[i][2] = Mathf.Lerp(
                    StyleSeries.Values[i][2],
                    isSitting,
                    1f//Controller.QueryFunction(StyleSeries.Styles[j] + "Control", i)
                    );
                StyleSeries.Values[i][3] = Mathf.Lerp(
                    StyleSeries.Values[i][3],
                    0.0f,
                    1f//Controller.QueryFunction(StyleSeries.Styles[j] + "Control", i)
                    );;
                StyleSeries.Values[i][4] = Mathf.Lerp(
                    StyleSeries.Values[i][4],
                    0.0f,
                    1//Controller.QueryFunction(StyleSeries.Styles[j] + "Control", i)
                    );

                // jump, ideally delete this
                //StyleSeries.Values[i][5] = Mathf.Lerp(
                //    StyleSeries.Values[i][5],
                //    0.0f,
                //    1f//Controller.QueryFunction(StyleSeries.Styles[j] + "Control", i)
                //    );
        }

        // smooth both this and TargetDirection using springs?
        // Q is do I feed in smoothed values to RootVelPredictionNet or just OG values?



        // Trajectory input
        //// Get FeetVelocities and Facing Direction
        //// Predict Root Velocity
        //// Convert to global root velocity
        //// apply correct use of spring dampers to all? (Feel should be damped before feeding in?)
        /// scaling? MaxForward/Backward/Sideways?
        /// Add to RootSeries and StyleSeries


        //// Get Action input
        /// calculate idle and move values




    }


    private Vector3 PredictTargetRootVelocity()
    {
        if (RootVelPredictionNet != null && RootVelPredictionNet.Setup)
        {
            RootVelPredictionNet.ResetPivot();
            FeedRootVelPredictionNet();
            RootVelPredictionNet.Predict();
            RootVelPredictionNet.ResetPivot();
            return RootVelPredictionNet.ReadXZ();
        }
        return Vector3.zero;
    }

    private void FeedRootVelPredictionNet()
    {
        // should we be feeding past target inputs or past values actually achieved by the dog?? As things stand, it's past target inputs. (Should I input spring damped values?)
        // this is fecked now as I've done everything based on a 3 second window and so these series have 3 x 20 frames now rather than 1 x 18
        for (int i = TimeSeriesPast.Pivot - 20; i <= TimeSeriesPast.Pivot; i++)
        {
            RootVelPredictionNet.Feed(LFSeries.GetVelocity(i));
        }
        for (int i = TimeSeriesPast.Pivot - 20; i <= TimeSeriesPast.Pivot; i++)
        {
            RootVelPredictionNet.Feed(RFSeries.GetVelocity(i));
        }
        for (int i = TimeSeriesPast.Pivot - 20; i < TimeSeriesPast.Pivot; i++)
        {
            RootVelPredictionNet.FeedXZ(TargetLocalRootVelocitySeries.GetVelocity(i));
        }
    }



    private void ProcessFootIK(UltimateIK.Model ik, float contact)
    {
        ik.Activation = UltimateIK.ACTIVATION.Constant;
        for (int i = 0; i < ik.Objectives.Length; i++)
        {
            ik.Objectives[i].SetTarget(Vector3.Lerp(ik.Objectives[i].TargetPosition, ik.Bones[ik.Objectives[i].Bone].Transform.position, 1f - contact));
            ik.Objectives[i].SetTarget(ik.Bones[ik.Objectives[i].Bone].Transform.rotation);
        }
        ik.Iterations = 25;
        ik.Solve();
    }

    protected override void OnGUIDerived()
    {

    }

    protected override void OnRenderObjectDerived()
    {
        RootSeries.Draw(GetCamera()); // need to fix up so not just drawing on top of each other
    }
}
