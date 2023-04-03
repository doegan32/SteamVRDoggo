using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalUpdate : MonoBehaviour
{

    public Transform Target;

   



    private Vector3 LastPosition;
    private Vector3 Position;
    private Vector3 Velocity;


    [Header("Targets to track")]
    public Transform LeftFoot;
    public Transform RightFoot;
    public Transform Pelvis;

    [Header("User offsets")]
    [SerializeField]
    private float LFDefaultHeight = 0.0f;
    [SerializeField]
    private bool LFDefaultHeightSet = false;
    [SerializeField]
    private float RFDefaultHeight = 0.0f;
    [SerializeField]
    private bool RFDefaultHeightSet = false;
    [SerializeField]
    public float PelvisDefaultHeight = 0.0f;
    [SerializeField]
    private bool PelvisDefaultHeightSet = false;

    [Header("Smoothing")]
    private Vector3 TargetDirection;
    private Vector3 FacingDirection;
    private Vector3 FacingDirectionVelocity;
    private Vector3 FacingDirectionHalfLife;
    [Range(0.0f, 2.0f)]
    public float HalfLifeFacingDirection = 0.2f;
    private float dt;







    // add in smoothing for pelvis rotation??
    // get user height on setup?
    // Then scale taget height to match scaled user height? Might allow sitting??


    // Start is called before the first frame update
    void Start()
    {
        dt = Time.deltaTime;
        FacingDirection = Vector3.forward;
        FacingDirectionVelocity = Vector3.zero;
        TargetDirection = Vector3.ProjectOnPlane(Target.right, Vector3.up);
        Springs.CriticalSpringDamper(
            ref FacingDirection,
            ref FacingDirectionVelocity,
            TargetDirection,
            HalfLifeFacingDirection,
            dt
        );




        //Position = Vector3.ProjectOnPlane(Target.position, Vector3.up) - new Vector3(0.0f, 0.0f, 0.5f); ;

        Position = Target.position;

        this.transform.rotation = Quaternion.LookRotation(FacingDirection, Vector3.up);
    }

    // Update is called once per frame
    void Update()
    {
        dt = Time.deltaTime;

        // Create user based thresholds
        if (!LFDefaultHeightSet)
        {
            LFDefaultHeight = LeftFoot.position.y;
            if (LFDefaultHeight > 0.0f)
            {
                LFDefaultHeightSet = true;
            }
        }
        if (!RFDefaultHeightSet)
        {
            RFDefaultHeight = RightFoot.position.y;
            if (RFDefaultHeight > 0.0f)
            {
                RFDefaultHeightSet = true;
            }
        }
        if (!PelvisDefaultHeightSet)
        {
            PelvisDefaultHeight = Pelvis.position.y;
            if (PelvisDefaultHeight > 0.0f)
            {
                PelvisDefaultHeightSet = true;
            }
        }


        // Update Facing Direction
        TargetDirection = Vector3.ProjectOnPlane(Target.right, Vector3.up);
        Springs.CriticalSpringDamper(
            ref FacingDirection,
            ref FacingDirectionVelocity,
            TargetDirection,
            HalfLifeFacingDirection,
            dt
        );
        this.transform.rotation = Quaternion.LookRotation(FacingDirection, Vector3.up);



        FacingDirection = Vector3.ProjectOnPlane(Target.right, Vector3.up);

        //LastPosition = Position;
        //Position = Target.position;
        //Velocity = Vector3.Project(Position - LastPosition, Vector3.up);
        ////Position = Vector3.ProjectOnPlane(Target.position, Vector3.up) - new Vector3(0.0f, 0.0f, 1.0f);

        ////Velocity = Position - LastPosition;
        
        //this.transform.position = Position + Velocity;
    }
}
