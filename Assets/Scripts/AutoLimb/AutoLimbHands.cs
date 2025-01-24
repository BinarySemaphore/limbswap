using UnityEngine;

public enum AutoLimbHandState
{
    Idle
}

public class AutoLimbHands : MonoBehaviour
{
    private GameObject[] hands;
    private AutoLimbHandState[] states;
    private float[] phases;

    private void Start()
    {
        int hand_count = this.transform.childCount;

        this.hands = new GameObject[hand_count];
        this.states = new AutoLimbHandState[hand_count];
        this.phases = new float[hand_count];

        for (int i = 0; i < hand_count; i++)
        {
            this.hands[i] = this.transform.GetChild(i).gameObject;
            this.states[i] = AutoLimbHandState.Idle;
            this.phases[i] = 0f;
        }
    }

    public GameObject[] Hands
    {
        get { return this.hands; }
    }

    public AutoLimbHandState GetHandState(int index)
    {
        return this.states[index];
    }

    public void SetHandState(int index, AutoLimbHandState new_state)
    {
        this.states[index] = new_state;
    }
}
