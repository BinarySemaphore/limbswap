using UnityEngine;
using UnityEngine.Assertions.Must;

public class AutoLimbHip : MonoBehaviour
{
    private const float LEG_FOOT_EXTENSION = 1.2f;
    private const float NEAR_ZERO = 0.0000001f;

    private bool ambulateCalled = false;
    private float maxLegExtension;
    private float hipToParentLength;
    private float feetToHipLength;
    private float currentPhase;
    private float phaseShift;
    private RaycastHit2D lastSurfaceContact;
    private AutoLimbFeet feetController;

    public GameObject debugCube;

    [SerializeField]
    [Tooltip("Hip will lower to surface to resolve gate; it will maintain distance to parent by pulling on it")]
    private GameObject parent;
    [SerializeField]
    [Range(0.01f, 0.99f)]
    private float attachmentSpringiness = 0.30f;
    [SerializeField]
    [Tooltip("Leave zero to autoset on spawn")]
    private Vector3 hipDirection = Vector3.zero;
    [SerializeField]
    [Tooltip("Leave zero to autoset on spawn")]
    private Vector3 feetDirection = Vector3.zero;
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
    [SerializeField]
    [Range(0.01f, 0.99f)]
    [Tooltip("Gate as percet of diameter using leg length from hip. Near 100% would be doing the splits; Near 0% would be tiny tiptoeing")]
    private float gatePercent = 0.5f;

    private void Start()
    {
        this.feetController = this.GetComponentInChildren<AutoLimbFeet>();

        Vector3 parent_to_hip = this.transform.position - this.parent.transform.position;
        Vector3 hip_to_feet = this.feetController.transform.position - this.transform.position;

        this.hipToParentLength = parent_to_hip.magnitude;
        this.feetToHipLength = hip_to_feet.magnitude;

        if (this.hipDirection == Vector3.zero) this.hipDirection = parent_to_hip.normalized;
        if (this.feetDirection == Vector3.zero) this.feetDirection = hip_to_feet.normalized;

        this.maxLegExtension = this.feetToHipLength * LEG_FOOT_EXTENSION;
        //this.neutralPosition = this.feetDirection * this.feetToHipLength;

        this.currentPhase = 0f;
        // TODO: Allow some way for unity ui to specify phases (eg cheetah vs horse vs spider/crab vs robot)
        this.phaseShift = 2f * Mathf.PI / this.feetController.Feet.Length;

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
        Vector3 surface_rel_pos_delta = Vector3.zero;

        this.PositionFeetHipAndParent();

        RaycastHit2D contact = Physics2D.Raycast(
            this.transform.position,
            this.feetDirection,
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

        // TODO: Maybe sketchy doing vector2 to vector3 assignment. For safety, probably best to clean up explicitly.
        if (this.lastSurfaceContact) surface_rel_pos_delta = contact.point - this.lastSurfaceContact.point;

        if (contact)
        {

            if (this.state == AutoLimbState.Engaged && surface_rel_pos_delta.magnitude > NEAR_ZERO)
            {

                Vector3 new_hip_position = this.transform.position - new Vector3(contact.point.x, contact.point.y, 0.0f);
                new_hip_position = new_hip_position.normalized * this.maxLegExtension * (1.0f - this.gatePercent);
                this.transform.position = new Vector3(contact.point.x, contact.point.y, 0.0f) + new_hip_position;

                surface_rel_pos_delta = Vector3.ProjectOnPlane(surface_rel_pos_delta, contact.normal + this.lastSurfaceContact.normal);
                this.UpdateFeetMoving(surface_rel_pos_delta, contact);

                if (this.ambulateCalled)
                {
                    this.ambulateCalled = false;
                    this.state = AutoLimbState.Paused;
                }
            }
            else
            {
                this.UpdateFeetStanding(contact);
            }

            this.lastSurfaceContact = contact;
        }
    }

    private void UpdateFeetMoving(Vector3 delta, RaycastHit2D contact)
    {
        float foot_phase;
        AutoLimbFootState foot_state;
        Vector3 contact_point_3d = new Vector3(contact.point.x, contact.point.y, this.transform.position.z);
        Vector3 new_foot_position;
        GameObject foot;
        RaycastHit2D foot_contact;

        if (delta.magnitude < 0.01f) return;

        // TODO: If/When updating foot travel path to circle/oval make sure this get's updated for percent covered circumference
        float distance_to_hip = (this.transform.position - contact_point_3d).magnitude;
        float surface_distance = 2f * Mathf.Sqrt(
            Mathf.Pow(this.maxLegExtension, 2f) -
            Mathf.Pow(distance_to_hip, 2f)
        );
        float available_distance = Mathf.PI * surface_distance + surface_distance;
        // Angle is radians full circle * ratio; ratio is distance over circumferance
        // Simplified from 2f * Mathf.PI * (delta.magnitude / (surface_distance * Mathf.PI))
        float phase_delta = 2f * (delta.magnitude / surface_distance);

        if (delta.x + delta.y > 0) this.currentPhase -= phase_delta;
        else this.currentPhase += phase_delta;
        this.currentPhase = Utils.Mod(this.currentPhase, 2f * Mathf.PI);

        for (int i = 0; i < this.feetController.Feet.Length; i++)
        {
            foot = this.feetController.Feet[i];
            foot_state = this.feetController.GetFootState(i);
            foot_phase = Utils.Mod(this.currentPhase + i * this.phaseShift,  2f * Mathf.PI);

            if (foot_phase <= 2 * Mathf.PI && foot_phase > Mathf.PI) this.feetController.SetFootState(i, AutoLimbFootState.Pushing);
            else this.feetController.SetFootState(i, AutoLimbFootState.Lifting);

            new_foot_position = new Vector3(
                Mathf.Cos(foot_phase) * surface_distance * 0.5f + contact_point_3d.x,
                Mathf.Sin(foot_phase) * surface_distance * 0.5f + contact_point_3d.y,
                contact_point_3d.z
            );
            if (foot_state == AutoLimbFootState.Pushing)
            {
                new_foot_position = Vector3.ProjectOnPlane(new_foot_position - contact_point_3d, contact.normal) + contact_point_3d;
            }

            foot.transform.position = new_foot_position;
        }
    }

    private void UpdateFeetStanding(RaycastHit2D contact)
    {
        GameObject foot;
        float foot_phase;
        AutoLimbFootState foot_state;
        float phase_delta;
        Vector3 new_foot_position;
        Vector3 contact_point_3d = new Vector3(contact.point.x, contact.point.y, this.transform.position.z);

        float distance_to_hip = (this.transform.position - contact_point_3d).magnitude;
        float surface_distance = 2f * Mathf.Sqrt(
            Mathf.Pow(this.maxLegExtension, 2f) -
            Mathf.Pow(distance_to_hip, 2f)
        );

        for (int i = 0; i < this.feetController.Feet.Length; i++)
        {
            foot = this.feetController.Feet[i];
            foot_state = this.feetController.GetFootState(i);
            // TODO: foot_phase isn't stored, only derived rn, so springing phase_delta won't work atm
            // Maybe have a currentPhase per foot?
            foot_phase = Utils.Mod(this.currentPhase + i * this.phaseShift, 2f * Mathf.PI);

            if (foot_phase <= 2 * Mathf.PI && foot_phase > Mathf.PI) this.feetController.SetFootState(i, AutoLimbFootState.Pushing);
            else this.feetController.SetFootState(i, AutoLimbFootState.Lifting);

            phase_delta = Utils.ShortestAngle(foot_phase, Mathf.PI * 1.5f);
            foot_phase += phase_delta;

            new_foot_position = new Vector3(
                Mathf.Cos(foot_phase) * surface_distance * 0.5f + contact_point_3d.x,
                Mathf.Sin(foot_phase) * surface_distance * 0.5f + contact_point_3d.y,
                contact_point_3d.z
            );
            if (foot_state == AutoLimbFootState.Pushing)
            {
                new_foot_position = Vector3.ProjectOnPlane(new_foot_position - contact_point_3d, contact.normal) + contact_point_3d;
            }

            foot.transform.position = new_foot_position;
        }
    }

    private void PositionFeetHipAndParent()
    {
        float half_spring_coef = this.attachmentSpringiness * 0.5f;
        if (this.hipDirection.magnitude > 1 + NEAR_ZERO) this.hipDirection.Normalize();
        if (this.feetDirection.magnitude > 1 + NEAR_ZERO) this.feetDirection.Normalize();

        Utils.ApplySpringResolveSingle(
            this.hipToParentLength,
            half_spring_coef,
            this.transform.position,
            this.parent
        );
        Utils.ApplySpringResolveSingle(
            0f,
            half_spring_coef,
            this.parent.transform.position + this.hipDirection * this.hipToParentLength,
            this.gameObject
        );
        Utils.ApplySpringResolveSingle(
            0f,
            half_spring_coef,
            this.transform.position + this.feetDirection * this.feetToHipLength,
            this.feetController.gameObject
        );
        //Utils.ApplySpringResolveDual(this.hipToParentLength, this.attachmentSpringiness, this.gameObject, this.hipParent);
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
