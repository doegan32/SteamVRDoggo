using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadFollower : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    private Transform DogHeadPosition;

    private Vector3 TargetPosition;
    private Vector3 position;
    private Vector3 velocity;
    [Range(0.0f, 2.0f)]
    public float halflife = 0.2f;
    private float dt;

    void Start()
    {
        dt = Time.deltaTime;
        position = DogHeadPosition.transform.position;
        this.transform.position = position;
    }

    // Update is called once per frame
    void Update()
    {
        dt = Time.deltaTime;

        TargetPosition = DogHeadPosition.transform.position;

        Springs.CriticalSpringDamper(
            ref position,
            ref velocity,
            TargetPosition,
            halflife,
            dt
            );

        this.transform.position = position;
    }
}
