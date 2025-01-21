using System.ComponentModel;
using UnityEngine;

public class AutoLimbHip : MonoBehaviour
{

    [SerializeField]
    [DefaultValue(AutoLimbState.Paused)]
    private AutoLimbState state;

    private bool ambulate_called = false;
    private float phase_change;
    private AutoLimb body_controller;
    private AutoLimbFeet feet_controller;

    private void Start()
    {
        this.body_controller = this.transform.parent.GetComponent<AutoLimb>();
        this.feet_controller = this.GetComponentInChildren<AutoLimbFeet>();
    }

    private void FixedUpdate()
    {
        if (this.state == AutoLimbState.Engaged)
        {
            this.MoveFeet();
            if (this.ambulate_called)
            {
                this.ambulate_called = false;
                this.state = AutoLimbState.Paused;
            }
        }
    }

    private void MoveFeet()
    {

    }

    public AutoLimbFeet FeetController
    {
        get { return this.feet_controller; }
    }

    /// <summary>
    /// Directly set Ambulation <see cref="AutoLimbState"/>.<br/>
    /// </summary>
    /// <remarks>
    /// Cannot Engage if Disabled, must directly set as Paused first.
    /// </remarks>
    public AutoLimbState AmbulationState
    {
        get { return this.state; }
        set
        {
            if (this.state == AutoLimbState.Disabled && value == AutoLimbState.Engaged)
            {
                Debug.LogWarning("Cannot Switch from Disabled to Engaged");
                return;
            }
            this.state = value;
        }
    }

    /// <summary>
    /// Active call to Engage Ambulation <see cref="AutoLimbState"/> on this frame.<br/>
    /// Will enter Paused <see cref="AutoLimbState"/> on frame end.<br/>
    /// </summary>
    /// <remarks>
    /// Cannot Engage if Disabled, must directly set as Paused first.<br/>
    /// Use <see cref="AmbulationState"/> to toggle state from triggers.
    /// </remarks>
    public void Ambulate()
    {
        if (this.state != AutoLimbState.Engaged && !this.ambulate_called)
        {
            this.ambulate_called = true;
            this.state = AutoLimbState.Engaged;
        }
    }
}
