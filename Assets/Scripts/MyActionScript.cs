using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class MyActionScript : MonoBehaviour
{
    //// a reference to the action
    //public SteamVR_Action_Pose ControllerPose;
    //public SteamVR_Action_Boolean SphereOnOff;// a reference to the hand
    //public SteamVR_Input_Sources handType;//reference to the sphere
    //public GameObject Sphere;

    public Transform LF;
    public Transform RF;
    public Transform Waste;


        private void Update()
        {
        Debug.Log("Waste: " + Waste.position.ToString() + " " + Waste.rotation.eulerAngles.ToString());
        }



}
