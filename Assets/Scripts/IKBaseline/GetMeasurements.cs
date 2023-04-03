using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetMeasurements : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Actor actor = GetComponent<Actor>();
        for (int i = 0; i < actor.Bones.Length; i++)
        {
            Debug.Log(actor.Bones[i].GetName() + actor.Bones[i].Transform.position.ToString());
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
