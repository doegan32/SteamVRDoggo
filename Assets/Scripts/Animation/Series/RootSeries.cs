using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR // what happens if I get rid of this?
using UnityEditor;
#endif


public class RootSeries : ComponentSeries
{
    public Matrix4x4[] Transformations;
    public Vector3[] Velocities;

    // public bool CollisionsResolved;

    public RootSeries(TimeSeries global) : base(global)
    {
        Transformations = new Matrix4x4[SampleCount];
        Velocities = new Vector3[SampleCount];
        for (int i=0; i < SampleCount; i++)
        {
            Transformations[i] = Matrix4x4.identity; // these will be global transformations. This makes incrementing easy as don't need to apply any transformations. Can just use "GetRelativeTo" functions later to get inputs
            Velocities[i] = Vector3.zero;
        }
    }

    // set all transformations to be the current global root transformation
    // i guess we want all trajectory info to be at the current root at time 0
    public RootSeries(TimeSeries global, Transform transform) : base(global)
    {
        Transformations = new Matrix4x4[Samples.Length];
        Velocities = new Vector3[Samples.Length];
        Matrix4x4 root = transform.GetWorldMatrix(true);
        for (int i = 0; i < Samples.Length; i++)
        {
            Transformations[i] = root;
            Velocities[i] = Vector3.zero;
        }
    }

    public void SetTransformation(int index, Matrix4x4 transformation)
    {
        Transformations[index] = transformation;
    }

    public Matrix4x4 GetTransformation(int index)
    {
        return Transformations[index];
    }

    public void SetPosition(int index, Vector3 position)
    {
        Matrix4x4Extensions.SetPosition(ref Transformations[index], position);
    }

    public Vector3 GetPosition(int index)
    {
        return Transformations[index].GetPosition();
    }

    public void SetRotation(int index, Quaternion rotation)
    {
        Matrix4x4Extensions.SetRotation(ref Transformations[index], rotation);
    }

    public Quaternion GetRotation(int index)
    {
        return Transformations[index].GetRotation();
    }

    public void SetDirection(int index, Vector3 direction)
    {
        Matrix4x4Extensions.SetRotation(ref Transformations[index], Quaternion.LookRotation(direction == Vector3.zero ? Vector3.forward : direction, Vector3.up));
    }

    public Vector3 GetDirection(int index)
    {
        return Transformations[index].GetForward();
    }

    public void SetVelocity(int index, Vector3 velocity)
    {
        Velocities[index] = velocity;
    }

    public Vector3 GetVelocity(int index)
    {
        return Velocities[index];
    }

    public void Translate(int index, Vector3 delta)
    {
        SetPosition(index, GetPosition(index) + delta);
    }

    public void Rotate(int index, Quaternion delta)
    {
        SetRotation(index, GetRotation(index) * delta);
    }

    public void Rotate(int index, float angle, Vector3 axis)
    {
        Rotate(index, Quaternion.AngleAxis(angle, axis));
    }

    //public void ResolveCollisions(float safety, LayerMask mask)
    //{

    //}

    public override void Increment(int start, int end)
    {
        for (int i=start; i<end; i++)
        {
            Transformations[i] = Transformations[i + 1];
            Velocities[i] = Velocities[i + 1];
        }
    }

    public override void Interpolate(int start, int end)
    {
        for (int i=start; i < end; i++)
        {
            float weight = (float)(i % Resolution) / (float)Resolution;
            int prevIndex = GetPreviousKey(i).Index;
            int nextIndex = GetNextKey(i).Index;
            if (prevIndex != nextIndex)
            {
                SetPosition(i, Vector3.Lerp(GetPosition(prevIndex), GetPosition(nextIndex), weight));
                SetDirection(i, Vector3.Lerp(GetDirection(prevIndex), GetDirection(nextIndex), weight).normalized);
                SetVelocity(i, Vector3.Lerp(GetVelocity(prevIndex), GetVelocity(nextIndex), weight));
            }
        }
    }

    public override void GUI(Camera canvas = null)
    {
     
    }

    public override void Draw(Camera canvas = null)
    {
        if (DrawScene)
        {
            UltiDraw.Begin(canvas);

            float size = 2f;
            int step = Resolution;

            // connections
            for (int i = 0; i < SampleCount - step; i += step)
            {
                UltiDraw.DrawLine(Transformations[i].GetPosition(), Transformations[i + step].GetPosition(), Transformations[i].GetUp(), size * 0.01f, UltiDraw.Black);
            }


            // positions
            for (int i = 0; i < SampleCount; i += step)
            {
                UltiDraw.DrawCircle(Transformations[i].GetPosition(), size * 0.025f, i % Resolution == 0 ? UltiDraw.Black : UltiDraw.Red.Opacity(0.5f));
            }

            // directions
            for (int i = 0; i < SampleCount; i += step)
            {
                UltiDraw.DrawLine(Transformations[i].GetPosition(), Transformations[i].GetPosition() + 0.25f*Transformations[i].GetForward(), Transformations[i].GetUp(), size * 0.025f, 0.0f, UltiDraw.Orange.Opacity(0.75f));
            }

            // velocities
            for (int i=0; i<SampleCount; i += step)
            {
                UltiDraw.DrawLine(Transformations[i].GetPosition(), Transformations[i].GetPosition() + GetTemporalScale(Velocities[i]), Transformations[i].GetUp(), size * 0.0125f, 0.0f, UltiDraw.DarkGreen.Opacity(0.25f));
            }
            UltiDraw.End();
        }
    }
}
