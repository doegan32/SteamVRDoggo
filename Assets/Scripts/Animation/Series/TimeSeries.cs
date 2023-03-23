using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSeries
{
    public enum ID { None, Root, Style, Contact };

    // In a field declaration, readonly indicates that assignment to the field can only occur as part of the declaration or in a constructor in the same class. A readonly field can be assigned and reassigned multiple times within the field declaration and constructor.
    public readonly int PastKeys = 0;
    public readonly int FutureKeys = 0;
    public readonly float PastWindow = 0.0f;
    public readonly float FutureWindow = 0.0f;
    public readonly int Resolution = 0;

    public readonly Sample[] Samples = new Sample[0];

    public int Pivot // index of sample at time 0
    {
        get { return PastSampleCount; }
    }
    public int SampleCount // total number of samples
    {
        get { return PastSampleCount + FutureSampleCount + 1; }
    }

    public int PastSampleCount // total number of past samples
    {
        get { return PastKeys * Resolution; }
    }
    public int FutureSampleCount // total number of future samples
    {
        get { return FutureKeys * Resolution; }
    }
    public int PivotKey
    {
        get { return PastKeys; }
    }
    public int KeyCount
    {
        get { return PastKeys + FutureKeys + 1; }
    }
    public float Window
    {
        get { return PastWindow + FutureWindow; }
    }
    public float DeltaTime
    {
        get { return Window / SampleCount; }
    }

    public class Sample
    {
        public int Index;
        public float Timestamp;

        public Sample(int index, float timestamp)
        {
            Index = index;
            Timestamp = timestamp;
        }
    }

    // Global constructor
    public TimeSeries(int pastKeys, int futureKeys, float pastWindow, float futureWindow, int resolution)
    {
        PastKeys = pastKeys;
        FutureKeys = futureKeys;
        PastWindow = pastWindow;
        FutureWindow = futureWindow;
        Resolution = resolution;
        Samples = new Sample[SampleCount]; // number of samples is no. past + 1 for present + no.future

        for (int i = 0; i < Pivot; i++)
        {
            Samples[i] = new Sample(i, -PastWindow + i * PastWindow / PastSampleCount);
        }
        Samples[Pivot] = new Sample(Pivot, 0.0f);
        for (int i = Pivot + 1; i < SampleCount; i++)
        {
            Samples[i] = new Sample(i, (i - Pivot) * FutureWindow / FutureSampleCount);
        }
    }

    // Derived constructor -  protected member is accessible within its class and by derived class instances.
    protected TimeSeries(TimeSeries global)
    {
        PastKeys = global.PastKeys;
        FutureKeys = global.FutureKeys;
        PastWindow = global.PastWindow;
        FutureWindow = global.FutureWindow;
        Resolution = global.Resolution;
        Samples = global.Samples;
    }
    public float GetTemporalScale(float value)
    {
        return Window / KeyCount * value;
    }

    public Vector2 GetTemporalScale(Vector2 value)
    {
        return Window / KeyCount * value;
    }

    public Vector3 GetTemporalScale(Vector3 value)
    {
        return Window / KeyCount * value;
    }

    public Sample GetPivot()
    {
        return Samples[Pivot];
    }

    public Sample GetKey(int index)
    {
        if (index < 0 || index >= KeyCount)
        {
            Debug.Log("Given key was " + index + " but must be within 0 and " + (KeyCount - 1) + ".");
            return null;
        }
        return Samples[index * Resolution];


    }

    public Sample GetPreviousKey(int sample)
    // is this that you provide a sample index (i.e. not a key index) and
    // it returns the last key before this sample?
    {
        if (sample < 0 || sample >= SampleCount)
        {
            Debug.Log("Given index was " + sample + " but must be within 0 and " + (Samples.Length - 1) + ".");
            return null;
        }
        return GetKey(sample / Resolution);
    }

    public Sample GetNextKey(int sample)
    {
        if (sample < 0 || sample >= SampleCount)
        {
            Debug.Log("Given index was " + sample + " but must be within 0 and " + (Samples.Length - 1) + ".");
            return null;
        }
        if (sample % Resolution == 0)
        {
            return GetKey(sample / Resolution);
        }
        else
        {
            return GetKey(sample / Resolution + 1);
        }
    }

    // as index goes from Pivot to Samples.Length, this goes from 0 to 1 (or more precisely from min to max)
    // with the effect that
    // at the current frame t (i.e. the Pivot), we use exclusively the output of the NN
    // and as we move to future we gradually use more of the controller input
    public float GetControl(int index, float bias, float min = 0f, float max = 1f)
    {
        return index.Ratio(Pivot, Samples.Length - 1).ActivateCurve(bias, min, max);
    }


    // as index goes from Pivot to Samples.Length, this goes from 1 to 0 (or more precisely from max to min)
    // with the effect that
    // at the current frame t (i.e. the Pivot), we use exclusively NN output
    // and as we move to future we gradually leave the series as is
    public float GetCorrection(int index, float bias, float max = 1f, float min = 0f)
    {
        return index.Ratio(Pivot, Samples.Length - 1).ActivateCurve(bias, max, min);

    }
}