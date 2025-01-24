using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class AutoLimbTerminal
{
    public GameObject gameObject;
    private float phase;
    private Stack<float> stashedPhase;

    private void KeepPhaseInRange()
    {
        if (this.phase > Utils.FULL_TURN || this.phase < 0f)
        {
            this.phase = Utils.Mod(this.phase, Utils.FULL_TURN);
        }
    }

    public float Phase
    {
        get { return this.phase; }
        set
        {
            this.phase = value;
            this.KeepPhaseInRange();
        }
    }

    public void StashPhasePush()
    {
        this.stashedPhase.Push(this.phase);
    }

    public void StashPhasePop()
    {
        if (this.stashedPhase.Count == 0) return;
        this.phase = this.stashedPhase.Pop();
    }
}

public class AutoLimbEndpoint : MonoBehaviour
{
    private AutoLimbTerminal[] terminals;

    private void Start()
    {
        int endpoint_count = this.transform.childCount;
        this.terminals = new AutoLimbTerminal[endpoint_count];

        for (int i = 0; i < endpoint_count; i++)
        {
            this.terminals[i] = new AutoLimbTerminal();
            this.terminals[i].gameObject = this.transform.GetChild(i).gameObject;
            this.terminals[i].Phase = 0f;
        }

        this.Initialize();
    }

    protected void Initialize()
    {
    }

    public AutoLimbTerminal[] Terminals
    {
        get { return this.terminals; }
    }

    public override string ToString()
    {
        string output = $"{this.name} (AutoLimbEndpoint | endpoints=[";
        for (int i = 0; i < this.terminals.Length; i++)
        {
            output += this.terminals[i].gameObject.ToString();
            output += $" phase={(Mathf.Rad2Deg * this.terminals[i].Phase).ToString("f2")}";
            if (i < this.terminals.Length - 1) output += ", ";
        }
        output += "])";
        return output;
    }
}
