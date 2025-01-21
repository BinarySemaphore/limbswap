using UnityEngine;

public class AutoLimbHip : MonoBehaviour
{
    private const float LEG_FOOT_EXTENSION = 1.2f;
    private const float NEAR_ZERO = 0.0000001f;

    private bool ambulateCalled = false;
    private float maxLegExtension;
    private Vector3 neutralPosition;
    private Vector3 lastPosition;
    private AutoLimbFeet feetController;

    public GameObject debugCube;

    [SerializeField]
    private AutoLimbState state = AutoLimbState.Paused;
    [SerializeField]
    private int feetToMaintainContact = 1;
    [SerializeField]
    [Tooltip("When initializing, assigning feet as pushing, these are the feet lifting before next push")]
    private int feetToNextPush = 1;
    [SerializeField]
    [Range(0.01f, 0.99f)]
    private float liftPercent = 0.1f;

    private void Start()
    {
        this.feetController = this.GetComponentInChildren<AutoLimbFeet>();

        this.maxLegExtension = (this.transform.position - this.feetController.transform.position).magnitude * LEG_FOOT_EXTENSION;
        this.neutralPosition = this.feetController.Feet[0].transform.position;

        // Initialize each foot's state as pushing or lifting
        // This is very important to develop a cadence for the animation.
        int contact_count_down = this.feetToMaintainContact;
        int feet_until_next_push = 0;
        for (int i = 0; i < this.feetController.Feet.Length; i++)
        {
            if (contact_count_down <= 0) break;
            if (feet_until_next_push == 0)
            {
                this.feetController.SetFootState(i, AutoLimbFootState.Pushing);
                feet_until_next_push = this.feetToNextPush;
                contact_count_down -= 1;
            }
        }
    }

    private void FixedUpdate()
    {
        Vector3 surface_rel_pos_delta;

        RaycastHit2D contact = Physics2D.Raycast(
            this.transform.position,
            this.feetController.transform.position - this.transform.position,
            this.maxLegExtension,
            LayerMask.GetMask("Surface")
        );
        if (contact)
        {
            this.debugCube.SetActive(true);
            this.debugCube.transform.position = new Vector3(contact.point.x, contact.point.y, -3f);
            this.debugCube.transform.LookAt(
                new Vector3(
                    this.debugCube.transform.position.x + contact.normal.x,
                    this.debugCube.transform.position.y + contact.normal.y,
                    this.debugCube.transform.position.z),
                Vector3.forward
            );
        }
        else this.debugCube.SetActive(false);

        if (contact && this.state == AutoLimbState.Engaged)
        {
            surface_rel_pos_delta = this.transform.position - this.lastPosition;
            surface_rel_pos_delta = Vector3.ProjectOnPlane(surface_rel_pos_delta, contact.normal);

            this.UpdateFeetMoving(surface_rel_pos_delta, contact.normal);
            if (this.ambulateCalled)
            {
                this.ambulateCalled = false;
                this.state = AutoLimbState.Paused;
            }
        }
        this.lastPosition = this.transform.position;
    }

    private void UpdateFeetMoving(Vector3 delta, Vector3 normal)
    {
        AutoLimbFootState foot_state;
        Vector3 new_foot_position;
        GameObject foot;
        RaycastHit2D contact;

        if (delta.magnitude < NEAR_ZERO) return;

        for (int i = 0; i < this.feetController.Feet.Length; i++)
        {
            foot = this.feetController.Feet[i];
            foot_state = this.feetController.GetFootState(i);
            new_foot_position = foot.transform.position;
            if (foot_state == AutoLimbFootState.Lifting)
            {
                new_foot_position += delta;
                //contact = Physics2D.Raycast(
                //    new_foot_position,
                //    normal * -1f,
                //    this.maxLegExtension,
                //    LayerMask.GetMask("Surface")
                //);
                //if (contact)
                //{
                //    new_foot_position = new Vector3(contact.point.x, contact.point.y, new_foot_position.z);
                //    new_foot_position += normal * 0.1f; //TODO: replace with actual lift percent to hip
                //}

                if ((new_foot_position - this.transform.position).magnitude > this.maxLegExtension)
                {
                    // TODO: attach back to ground
                    this.feetController.SetFootState(i, AutoLimbFootState.Pushing);
                    continue;
                }
            }
            else
            {
                new_foot_position -= delta;
                //contact = Physics2D.Raycast(
                //    new_foot_position,
                //    normal * -1f,
                //    this.maxLegExtension,
                //    LayerMask.GetMask("Surface")
                //);
                //if (contact) new_foot_position = new Vector3(contact.point.x, contact.point.y, new_foot_position.z);

                // TODO: if dragging prevent -delta if at max extension
                if ((new_foot_position - this.transform.position).magnitude > this.maxLegExtension)
                {
                    // TODO: lift from ground
                    this.feetController.SetFootState(i, AutoLimbFootState.Lifting);
                    continue;
                }
            }
            foot.transform.position = new_foot_position;
        }
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
