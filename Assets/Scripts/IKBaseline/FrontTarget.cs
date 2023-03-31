using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontTarget : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform RearTarget;
    public Vector3 LocalOffset;

    public Transform Pelvis;
    private Vector3 FacingDirection;
    private Quaternion GlobalRotation;

    void Start()
    {
        FacingDirection = Vector3.ProjectOnPlane(Pelvis.right, Vector3.up);
        GlobalRotation = Quaternion.LookRotation(FacingDirection, Vector3.up);

        this.transform.position = RearTarget.position + GlobalRotation * LocalOffset;
    }

    // Update is called once per frame
    void Update()
    {
        FacingDirection = Vector3.ProjectOnPlane(Pelvis.right, Vector3.up);
        GlobalRotation = Quaternion.LookRotation(FacingDirection, Vector3.up);

        this.transform.position = RearTarget.position + GlobalRotation * LocalOffset;
    }
}
