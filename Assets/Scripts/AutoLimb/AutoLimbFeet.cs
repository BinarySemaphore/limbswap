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

    private void Start()
    {
        int i;

        this.feet = new GameObject[this.transform.childCount];
        for (i = 0; i < this.feet.Length; i++)
        {
            this.feet[i] = this.transform.GetChild(i).gameObject;
        }

        this.states = new AutoLimbFootState[this.feet.Length];
        for (i = 0; i < this.feet.Length; i++)
        {
            this.states[i] = AutoLimbFootState.Lifting;
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
