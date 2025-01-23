using System;
using UnityEngine;
using UnityEngine.Assertions.Must;

[Serializable]
public class Leg
{
    public GameObject[] segments;
}

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
    private AutoLimb bodyController;

    private GameObject debugArrow;
    private GameObject debugCircle;

    [Tooltip("Hip will lower to surface to resolve gate; distance to parent maintained by spring")]
    public GameObject parent;

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
    [SerializeField]
    private Leg[] legsAndSegments;

    private void Start()
    {
        debugArrow = GameObject.Find("/debugArrow");
        debugCircle = GameObject.Find("/debugCircle");

        this.bodyController = this.GetBodyController(this.parent);
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

        // Initialize each foot's state as pushing or lifting and phase.
        // This is very important to develop a cadence for the animation.
        int contact_count_down = this.feetToMaintainContact;
        int feet_until_next_push = 0;
        for (int i = 0; i < this.feetController.Feet.Length; i++)
        {
            if (contact_count_down > 0 && feet_until_next_push == 0)
            {
                this.feetController.SetFootState(i, AutoLimbFootState.Pushing);
                feet_until_next_push = this.feetToNextPush;
                contact_count_down -= 1;
            }
            //this.feetController.SetFootPhase(i, this.)
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
            this.debugArrow.SetActive(true);
            this.debugArrow.transform.position = new Vector3(contact.point.x, contact.point.y, -3f);
            this.debugArrow.transform.LookAt(
                new Vector3(
                    this.debugArrow.transform.position.x + contact.normal.x,
                    this.debugArrow.transform.position.y + contact.normal.y,
                    this.debugArrow.transform.position.z),
                Vector3.forward
            );
        }
        else this.debugArrow.SetActive(false);

        // TODO: Maybe sketchy doing vector2 to vector3 assignment. For safety, probably best to clean up explicitly.
        if (this.lastSurfaceContact) surface_rel_pos_delta = contact.point - this.lastSurfaceContact.point;

        if (contact)
        {

            if (this.state == AutoLimbState.Engaged && surface_rel_pos_delta.magnitude > 0.05f)
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

        if (this.legsAndSegments.Length > 0 && this.legsAndSegments[0].segments.Length > 0)
        {
            if (this.legsAndSegments.Length != this.feetController.Feet.Length)
            {
                throw new Exception("Legs And Segments, number of legs needs to match Hip/Feet number of children");
            }
            ConstructLegsAndSegments();
        }
    }

    private void UpdateFeetStanding(RaycastHit2D contact)
    {
        GameObject foot;
        Vector3 new_foot_position;
        Vector3 contact_point_3d = new Vector3(contact.point.x, contact.point.y, this.transform.position.z);

        for (int i = 0; i < this.feetController.Feet.Length; i++)
        {
            foot = this.feetController.Feet[i];
            new_foot_position = foot.transform.position + 0.1f * (contact_point_3d - foot.transform.position);
            foot.transform.position = new_foot_position;
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

        // Update feet positioning phase
        float phase_direction = Vector3.Cross(contact.normal, delta).z;
        if (phase_direction > 0) this.currentPhase += phase_delta;
        else this.currentPhase -= phase_delta;
        this.currentPhase = Utils.Mod(this.currentPhase, 2f * Mathf.PI);

        // Set individual foot positions by phase; TODO: refactor into single function
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

    private void ConstructLegsAndSegments()
    {
        int segment_count = this.legsAndSegments[0].segments.Length;
        float angle;
        Leg leg;
        GameObject foot, segment;
        Vector3 segment_length, start_point, end_point;

        // TODO: support 1 and 3 or more segment legs
        if (segment_count != 2) throw new NotImplementedException("Only 2 segment legs are supported");

        for (int i = 0; i < this.feetController.Feet.Length; i++)
        {
            foot = this.feetController.Feet[i];
            leg = this.legsAndSegments[i];
            start_point = this.transform.position;
            end_point = Utils.IkSolveTwoSeg(
                this.transform.position,
                foot.transform.position,
                this.bodyController.forward,
                this.feetToHipLength * 0.5f
            );

            for(int j = 0; j < segment_count; j++)
            {
                segment = leg.segments[j];
                segment_length = end_point - start_point;
                angle = Vector3.SignedAngle(this.bodyController.forward, segment_length, Vector3.forward) + 90f;

                segment.transform.position = start_point + segment_length * 0.5f;
                segment.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                //segment.transform.Rotate(Vector3.forward, 90);
                //segment.transform.LookAt(end_point, Vector3.forward);

                start_point = end_point;
                end_point = foot.transform.position;
            }
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

    private AutoLimb GetBodyController(GameObject parent)
    {
        AutoLimb true_parent = parent.GetComponent<AutoLimb>();
        if (true_parent != null)
        {
            return true_parent;
        }
        return this.GetBodyController(parent.GetComponent<AutoLimbHip>().parent);
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
