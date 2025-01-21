using UnityEngine;

public enum AutoLimbState
{
    Disabled,
    Paused,
    Engaged
}

public class AutoLimb : MonoBehaviour
{
    public GameObject target;

    [SerializeField]
    private AutoLimbHip[] hipContollers;

    private void Update()
    {
    }
}
