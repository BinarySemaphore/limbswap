using UnityEngine;

public class AutoLimbHip : MonoBehaviour
{

    [SerializeField]
    private AutoLimbState state = AutoLimbState.Paused;
    [SerializeField]
    private int numberFeetToPush = 1;

    private bool ambulateCalled = false;
    private float currentPhase = 0f;
    private float phaseDelta;
    private AutoLimb bodyController;
    private AutoLimbFeet feetController;

    private void Start()
    {
        this.bodyController = this.transform.parent.GetComponent<AutoLimb>();
        this.feetController = this.GetComponentInChildren<AutoLimbFeet>();
        this.phaseDelta = 360f / this.feetController.Feet.Length;
    }

    private void FixedUpdate()
    {
        if (this.state == AutoLimbState.Engaged)
        {
            this.MoveFeet();
            if (this.ambulateCalled)
            {
                this.ambulateCalled = false;
                this.state = AutoLimbState.Paused;
            }
        }
    }

    private void MoveFeet()
    {

    }

    public AutoLimbFeet FeetController
    {
        get { return this.feetController; }
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
        if (this.state != AutoLimbState.Engaged && !this.ambulateCalled)
        {
            this.ambulateCalled = true;
            this.state = AutoLimbState.Engaged;
        }
    }
}
