using UnityEngine;

public enum AutoLimbFootState
{
    Lifting,
    Pushing,
    Dragging
}

public class AutoLimbFeet : MonoBehaviour
{
    private GameObject[] feet;
    private AutoLimbFootState[] states;
    private float[] phases;

    private void Start()
    {
        int feet_count = this.transform.childCount;

        this.feet = new GameObject[feet_count];
        this.states = new AutoLimbFootState[feet_count];
        this.phases = new float[feet_count];

        for (int i = 0; i < feet_count; i++)
        {
            this.feet[i] = this.transform.GetChild(i).gameObject;
            this.states[i] = AutoLimbFootState.Lifting;
            this.phases[i] = 0f;
        }
    }

    public GameObject[] Feet
    {
        get { return this.feet; }
    }

    public AutoLimbFootState GetFootState(int index)
    {
        return this.states[index];
    }

    public void SetFootState(int index, AutoLimbFootState new_state)
    {
        this.states[index] = new_state;
    }

    public float GetFootPhase(int index)
    {
        return this.phases[index];
    }

    public void SetFootPhase(int index, float phase)
    {
        if (phase > Utils.FULL_TURN || phase < 0f) phase = Utils.Mod(phase, Utils.FULL_TURN);
        this.phases[index] = phase;
    }

    public void AddFootPhase(int index, float delta)
    {
        this.phases[index] += delta;
        if (this.phases[index] > Utils.FULL_TURN || this.phases[index] < 0f) this.phases[index] = Utils.Mod(this.phases[index], Utils.FULL_TURN);
    }

    public float LowPoint
    {
        get { return this.transform.position.y; }
    }

    public override string ToString()
    {
        string output = $"{this.name} (AutoLimbFeet | feet=[";
        for (int i = 0; i < this.feet.Length; i++)
        {
            output += this.feet[i].ToString();
            if (i < this.feet.Length - 1) output += ", ";
        }
        output += "])";
        return output;
    }
}
