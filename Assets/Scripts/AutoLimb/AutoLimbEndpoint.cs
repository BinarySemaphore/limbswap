using System;
using UnityEngine;

public class AutoLimbEndpoint : MonoBehaviour
{
    private GameObject[] endpoints;
    private float[] phases;
    private float[] stashPhases;

    private void Start()
    {
        int endpoint_count = this.transform.childCount;

        this.endpoints = new GameObject[endpoint_count];
        this.phases = new float[endpoint_count];

        for (int i = 0; i < endpoint_count; i++)
        {
            this.endpoints[i] = this.transform.GetChild(i).gameObject;
            this.phases[i] = 0f;
        }

        this.Initialize();
    }

    protected void Initialize()
    {
    }

    private void KeepPhaseInRange(int index)
    {
        if (this.phases[index] > Utils.FULL_TURN || this.phases[index] < 0f)
        {
            this.phases[index] = Utils.Mod(this.phases[index], Utils.FULL_TURN);
        }
    }
    public GameObject[] Endpoints
    {
        get { return this.endpoints; }
    }
    public float GetFootPhase(int index)
    {
        return this.phases[index];
    }
    public void SetPhase(int index, float phase)
    {
        this.phases[index] = phase;
        this.KeepPhaseInRange(index);
    }

    public void AddPhase(int index, float delta)
    {
        this.phases[index] += delta;
        this.KeepPhaseInRange(index);
    }

    public void StashPhasesPush()
    {
        this.stashPhases = this.phases;
        this.phases = new float[this.stashPhases.Length];
    }

    public void StashPhasesPop()
    {
        this.phases = this.stashPhases;
        this.stashPhases = null;
    }

    public override string ToString()
    {
        string output = $"{this.name} (AutoLimbEndpoint | endpoints=[";
        for (int i = 0; i < this.endpoints.Length; i++)
        {
            output += this.endpoints[i].ToString();
            output += $" phase={(Mathf.Rad2Deg * this.phases[i]).ToString("f2")}";
            if (i < this.endpoints.Length - 1) output += ", ";
        }
        output += "])";
        return output;
    }
}
