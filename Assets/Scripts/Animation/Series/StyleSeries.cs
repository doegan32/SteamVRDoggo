using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StyleSeries : ComponentSeries
{
    public string[] Styles;
    public float[][] Values;
    public StyleSeries(TimeSeries global, params string[] styles) : base(global) // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/params
    {
        Styles = styles;
        Values = new float[SampleCount][];
        for (int i = 0; i < Values.Length; i++)
        {
            Values[i] = new float[Styles.Length];
        }
    } 

    public StyleSeries(TimeSeries global, string[] styles, float[] seed) : base(global)
    {
        Styles = styles;
        Values = new float[SampleCount][];
        for (int i = 0; i < Values.Length; i++)
        {
            Values[i] = new float[Styles.Length];
        }
        if (styles.Length != seed.Length)
        {
            Debug.Log("Given number of styles and seed do not match.");
            return;
        }
        for (int i = 0; i < Values.Length; i++)
        {
            for (int j = 0; j < Styles.Length; j++)
            {
                Values[i][j] = seed[j];
            }
        }
    }

    public override void Increment(int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            for (int j = 0; j < Styles.Length; j++)
            {
                Values[i][j] = Values[i + 1][j];
            }
        }
    }

    public override void Interpolate(int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            float weight = (float)(i % Resolution) / (float)Resolution;
            int prevIndex = GetPreviousKey(i).Index;
            int nextIndex = GetNextKey(i).Index;
            for (int j = 0; j < Styles.Length; j++)
            {
                Values[i][j] = Mathf.Lerp(Values[prevIndex][j], Values[nextIndex][j], weight);
            }
        }
    }

    public override void GUI(Camera canvas = null)
    {
        return;
    }
    public override void Draw(Camera canvas = null)
    {
        return;
    } 

    public void SetStyle(int index, string name, float value)
    {
        int idx = ArrayExtensions.FindIndex(ref Styles, name);
        if (idx == -1)
        {
            return;
        }
        Values[index][idx] = value;
    }

    public float GetStyle(int index, string name)
    {
        int idx = ArrayExtensions.FindIndex(ref Styles, name);
        if (idx == -1)
        {
            return 0.0f;
        }
        return Values[index][idx];
    }

    public float[] GetStyles(int index, params string[] names)
    {
        float[] values = new float[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            values[i] = GetStyle(index, names[i]);
        }
        return values;
    }

}
