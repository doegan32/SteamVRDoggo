using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalUpdate : MonoBehaviour
{

    public Transform Target;

    private Vector3 FacingDirection;
    private Vector3 LastPosition;
    private Vector3 Position;
    private Vector3 Velocity;

    // Start is called before the first frame update
    void Start()
    {
        FacingDirection = Vector3.ProjectOnPlane(Target.right, Vector3.up);
        //Position = Vector3.ProjectOnPlane(Target.position, Vector3.up) - new Vector3(0.0f, 0.0f, 0.5f); ;

        Position = Target.position;

        this.transform.rotation = Quaternion.LookRotation(FacingDirection, Vector3.up);
    }

    // Update is called once per frame
    void Update()
    {
        FacingDirection = Vector3.ProjectOnPlane(Target.right, Vector3.up);

        LastPosition = Position;
        Position = Target.position;
        Velocity = Vector3.Project(Position - LastPosition, Vector3.up);
        //Position = Vector3.ProjectOnPlane(Target.position, Vector3.up) - new Vector3(0.0f, 0.0f, 1.0f);

        //Velocity = Position - LastPosition;
        this.transform.rotation = Quaternion.LookRotation(FacingDirection, Vector3.up);
        this.transform.position = Position + Velocity;
    }
}
