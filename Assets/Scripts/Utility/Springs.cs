using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// everything in this class is based on
// // https://theorangeduck.com/page/spring-roll-call
public static class Springs
{

    // for velocity
    // takes x, v, and a at time t
    // and updates them to x, v and a at time t+dt
    // for a given halflife and target velicty
    public static void CriticalSpringDamper(
        ref Vector3 x,  // position at time t
        ref Vector3 v,  // velocity at time t
        ref Vector3 a,  // acceleration at time t,
        Vector3 v_goal, // target velocity at time t,
        float halfife,
        float dt
        )
    {
        float y = Halflife2Damping(halfife) / 2.0f;
        Vector3 j0 = v - v_goal;
        Vector3 j1 = a + j0 * y;
        float eydt = FastNegExp(y * dt);

        x = eydt * ((-j1 / (y * y)) + ((-j0 - j1 * dt) / y)) + (j1 / (y * y)) + j0 / y + v_goal * dt + x;
        v = eydt * (j0 + j1 * dt) + v_goal;
        a = eydt * (a - j1 * y * dt);
    }



    // for facing direction? Is this a possibility?
    // takes facing direction x at time t
    // and updates to facing direction x at time t + dt
    // for a given halflife and target facing direction dt
    public static void CriticalSpringDamper(
        ref Vector3 x,  // velocity at time t
        ref Vector3 v,  // acceleration at time t,
        Vector3 x_goal, // target velocity at time x_goal,
        float halfife,
        float dt
        )
    {
        float y = Halflife2Damping(halfife) / 2.0f;
        Vector3 j0 = x - x_goal;
        Vector3 j1 = v + j0 * y;
        float eydt = FastNegExp(y * dt);


        x = eydt * (j0 + j1 * dt) + x_goal;
        v = eydt * (v - j1 * y * dt);
    }



    // move to spring script
    private static float Halflife2Damping(float halflife, float eps = 1e-5f)
    {
        return (4.0f * 0.69314718056f) / (halflife + eps);
    }
    private static float Damping2Halflife(float damping, float eps = 1e-5f)
    {
        return (4.0f * 0.69314718056f) / (damping + eps);
    }
    private static float FastNegExp(float x)
    {
        return 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
    }
}
