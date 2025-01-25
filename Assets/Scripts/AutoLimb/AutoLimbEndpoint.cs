using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class AutoLimbTerminal
{
    public GameObject gameObject;
    private float phaseOffset = 0f;
    private Stack<float> stashedPhaseOffset = new Stack<float>(10);

    private void KeepPhaseOffsetInRange()
    {
        if (this.phaseOffset > Utils.FULL_TURN || this.phaseOffset < 0f)
        {
            this.phaseOffset = Utils.Mod(this.phaseOffset, Utils.FULL_TURN);
        }
    }

    public float PhaseOffset
    {
        get { return this.phaseOffset; }
        set
        {
            this.phaseOffset = value;
            this.KeepPhaseOffsetInRange();
        }
    }

    public bool StashPhasePush()
    {
        if (this.stashedPhaseOffset.Count == 10) return false;
        this.stashedPhaseOffset.Push(this.phaseOffset);
        return true;
    }

    public void StashPhasePop()
    {
        if (this.stashedPhaseOffset.Count == 0) return;
        this.phaseOffset = this.stashedPhaseOffset.Pop();
    }
}

public class AutoLimbEndpoint : MonoBehaviour
{
    private bool populatedTerminals = false;
    private AutoLimbTerminal[] terminals;

    private void Start()
    {
        this.populateTerminals();
        this.Initialize();
    }

    private void populateTerminals()
    {
        if (!this.populatedTerminals)
        {
            int endpoint_count = this.transform.childCount;
            this.terminals = new AutoLimbTerminal[endpoint_count];

            for (int i = 0; i < endpoint_count; i++)
            {
                this.terminals[i] = new AutoLimbTerminal();
                this.terminals[i].gameObject = this.transform.GetChild(i).gameObject;
                this.terminals[i].PhaseOffset = 0f;
            }

            this.populatedTerminals = true;
        }
    }

    protected void Initialize()
    {
    }

    public AutoLimbTerminal[] Terminals
    {
        get {
            if (!this.populatedTerminals) this.populateTerminals();
            return this.terminals;
        }
    }

    public override string ToString()
    {
        string output = $"{this.name} (AutoLimbEndpoint | endpoints=[";
        for (int i = 0; i < this.terminals.Length; i++)
        {
            output += this.terminals[i].gameObject.ToString();
            output += $" phase={(Mathf.Rad2Deg * this.terminals[i].PhaseOffset):f2)}";
            if (i < this.terminals.Length - 1) output += ", ";
        }
        output += "])";
        return output;
    }
}
