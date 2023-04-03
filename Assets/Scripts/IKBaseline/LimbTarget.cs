using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbTarget : MonoBehaviour
{

    public Transform DogEndEffector;
    public Transform Target;

    [SerializeField]
    private float DefaultHeight = 0.0f;
    [SerializeField]
    private bool DefaultHeightSet = false;

    private Vector3 Positiom;
    private Vector3 Velocity;
    private Vector3 Acceleratiom;
    private Vector3 TargetPosition;
    private Vector3 LastTargetPosition;
    private Vector3 TargetVelocity;
    private float dt;
    private float halflife = 0.0f;

    private float DogEndEffectorHeight;


    // Start is called before the first frame update
    void Start()
    {
        dt = Time.deltaTime;

        //this.transform.position = DogEndEffector.position;

        this.transform.position = Target.position;
        TargetPosition = Target.position;
        LastTargetPosition = Target.position;
        TargetVelocity = Vector3.zero;

        DogEndEffectorHeight = DogEndEffector.transform.position.y; 

    }

    // Update is called once per frame
    void Update()
    {
        dt = Time.deltaTime;

        // Create user based thresholds
        if (!DefaultHeightSet)
        {
            DefaultHeight = Target.position.y;
            if (DefaultHeight > 0.0f)
            {
                DefaultHeightSet = true;
            }
        }

        if (DefaultHeightSet)
        {
            //LastTargetPosition = TargetPosition;
            //TargetPosition = Target.position;
            //Springs.CriticalSpringDamper(
            //    ref Positiom,
            //    ref Velocity,
            //    ref Acceleratiom,
            //    TargetVelocity,
            //    halflife,
            //    dt
            //    );
            //this.transform.position = new Vector3(Positiom.x, Mathf.Clamp(Positiom.y, 0.0f, 999.0f), Positiom.z);

            TargetPosition = Target.position;

            TargetPosition = new Vector3(TargetPosition.x, TargetPosition.y - DefaultHeight + DogEndEffectorHeight, TargetPosition.z);
            TargetPosition = new Vector3(TargetPosition.x, Mathf.Max(0.0f, TargetPosition.y), TargetPosition.z);
            this.transform.position = TargetPosition;

        }





    }
}
