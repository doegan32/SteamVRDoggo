using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontTarget : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform RearTarget;
 
    public Transform DogFrontEE;
    public Transform DogRearEE;
    private Vector3 EEOffset;
    private float EEOffsetV;


    public Transform Pelvis;
    private Vector3 FacingDirection;
    private Vector3 FacingDirectionVelocity;
    private float HalfLifeFacingDirection = 0.0f;
    private float dt;
    private Quaternion GlobalRotation;


    private Vector3 TargetPosition;

    void Start()
    {
        FacingDirection = Vector3.ProjectOnPlane(Pelvis.right, Vector3.up);
        GlobalRotation = Quaternion.LookRotation(FacingDirection, Vector3.up);

       
        // offset for calculating front target
        EEOffset = (DogFrontEE.position - DogRearEE.position) * 0.8f;
        EEOffset.y = 0.0f;

        EEOffsetV = DogFrontEE.position.y - DogRearEE.position.y;

        TargetPosition = RearTarget.position + GlobalRotation * EEOffset + new Vector3(0.0f, EEOffsetV, 0.0f);
        TargetPosition = new Vector3(TargetPosition.x, Mathf.Max(0.0f, TargetPosition.y), TargetPosition.z);
        this.transform.position = TargetPosition;

    }

    // Update is called once per frame
    void Update()
    {
        dt = Time.deltaTime;
        FacingDirection = Vector3.ProjectOnPlane(Pelvis.right, Vector3.up);
        GlobalRotation = Quaternion.LookRotation(FacingDirection, Vector3.up);

        Vector3 TargetDirection = Vector3.ProjectOnPlane(Pelvis.forward, Vector3.up);
        Springs.CriticalSpringDamper(
            ref FacingDirection,
            ref FacingDirectionVelocity,
            TargetDirection,
            HalfLifeFacingDirection,
            dt
        );


        GlobalRotation = Quaternion.LookRotation( FacingDirection, Vector3.up); //Quaternion.AngleAxis(90.0f, Vector3.up) *


        TargetPosition = RearTarget.position + GlobalRotation * EEOffset +  new Vector3(0.0f, EEOffsetV, 0.0f);
        TargetPosition = new Vector3(TargetPosition.x, Mathf.Max(0.0f, TargetPosition.y), TargetPosition.z);
        this.transform.position = TargetPosition;

    }
}
