using System;
using UnityEngine;

public enum AutoLimbState
{
    Disabled,
    Paused,
    Engaged
}

[Serializable]
public class Limb
{
    public GameObject[] segments;
}

public class AutoLimb : MonoBehaviour
{
    private float bodyToTargetLength;

    public GameObject parent;
    public Vector3 forward = Vector3.right;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    private float targetAttachmentSpringiness = 0.7f;
    [SerializeField]
    private AutoLimbHip[] hipContollers;

    private void Start()
    {
        this.bodyToTargetLength = (this.transform.position - parent.transform.position).magnitude;
    }

    private void FixedUpdate()
    {
        Utils.ApplySpringResolveSingle(
            this.bodyToTargetLength,
            this.targetAttachmentSpringiness,
            this.parent.transform.position,
            this.gameObject
        );
    }

    /// <summary>
    /// Directly set Ambulation <see cref="AutoLimbState"/> for all hips on body.<br/>
    /// </summary>
    /// <remarks>
    /// Cannot Engage if Disabled, must directly set as Paused first.
    /// </remarks>
    public AutoLimbState AmbulationState
    {
        get {
            // Returns Disabled if all disabled, Paused if some hips paused, Engaged if any hips engaged.
            AutoLimbState overall_state = AutoLimbState.Disabled;
            foreach (AutoLimbHip hip in this.hipContollers)
            {
                if (hip.AmbulationState == AutoLimbState.Engaged)
                {
                    overall_state = AutoLimbState.Engaged;
                    break;
                }
                if (hip.AmbulationState == AutoLimbState.Paused)
                {
                    overall_state = AutoLimbState.Paused;
                }
            }
            return overall_state;
        }
        set
        {
            foreach (AutoLimbHip hip in this.hipContollers)
            {
                hip.AmbulationState = value;
            }
        }
    }

    /// <summary>
    /// Active call to Engage Ambulation <see cref="AutoLimbState"/> on this frame for all hips on body.<br/>
    /// Will enter Paused <see cref="AutoLimbState"/> on frame end.<br/>
    /// </summary>
    /// <remarks>
    /// Cannot Engage if Disabled, must directly set as Paused first.<br/>
    /// Use <see cref="AmbulationState"/> to toggle state from triggers.
    /// </remarks>
    public void Ambulate()
    {
        foreach (AutoLimbHip hip in this.hipContollers)
        {
            hip.Ambulate();
        }
    }
}
