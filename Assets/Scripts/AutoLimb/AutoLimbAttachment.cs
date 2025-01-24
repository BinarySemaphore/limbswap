using System;
using UnityEngine;

public class AutoLimbAttachment : MonoBehaviour
{
    protected const float ENDPOINT_EXTENSION = 1.2f;
    protected const float NEAR_ZERO = 0.0000001f;

    protected bool ambulateCalled = false;
    protected float maxExtension;
    protected float anchorToParentLength;
    protected float endpointToAttachmentLength;
    protected AutoLimbEndpoint endpointController;
    protected AutoLimb bodyController;

    protected GameObject debugArrow;
    protected GameObject debugCircle;

    [Tooltip("Hip will lower to surface to resolve gate; distance to parent maintained by spring")]
    public GameObject parent;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    protected float attachmentSpringiness = 0.30f;
    [SerializeField]
    [Tooltip("Leave zero to autoset on spawn")]
    protected Vector3 hipDirection = Vector3.zero;
    [SerializeField]
    [Tooltip("Leave zero to autoset on spawn")]
    protected Vector3 focusPoint = Vector3.zero;
    [SerializeField]
    protected AutoLimbState state = AutoLimbState.Paused;
    [SerializeField]
    protected Limb[] limbsAndSegments;

    private void Start()
    {
        this.bodyController = this.GetBodyController(this.parent);
        this.endpointController = this.GetComponentInChildren<AutoLimbFeet>();

        Vector3 parent_to_hip = this.transform.position - this.parent.transform.position;
        Vector3 hip_to_feet = this.endpointController.transform.position - this.transform.position;

        this.anchorToParentLength = parent_to_hip.magnitude;
        this.endpointToAttachmentLength = hip_to_feet.magnitude;

        if (this.hipDirection == Vector3.zero) this.hipDirection = parent_to_hip.normalized;
        if (this.focusPoint == Vector3.zero) this.focusPoint = hip_to_feet.normalized;

        this.maxExtension = this.endpointToAttachmentLength * ENDPOINT_EXTENSION;

        this.Initialize();
    }

    private void FixedUpdate()
    {
        this.PositionEndpointsAndAttachmentWithParent();

        this.PositionEndpoints();

        if (this.limbsAndSegments.Length > 0 && this.limbsAndSegments[0].segments.Length > 0)
        {
            if (this.limbsAndSegments.Length != this.endpointController.Endpoints.Length)
            {
                throw new Exception("Limbs And Segments, number of limbs needs to match endpoint's number of children");
            }
            ConstructEndpointsAndSegments();
        }
    }
    protected virtual void Initialize()
    {
        throw new NotImplementedException("PositionEndpoints must be implemented in inherited class");
    }

    protected virtual void PositionEndpoints()
    {
        throw new NotImplementedException("PositionEndpoints must be implemented in inherited class");
    }

    private void ConstructEndpointsAndSegments()
    {
        int segment_count = this.limbsAndSegments[0].segments.Length;
        float angle;
        Limb leg;
        GameObject foot, segment;
        Vector3 segment_length, start_point, end_point;

        // TODO: support 1 and 3 or more segment legs
        if (segment_count != 2) throw new NotImplementedException("Only 2 segment legs are supported");

        for (int i = 0; i < this.endpointController.Endpoints.Length; i++)
        {
            foot = this.endpointController.Endpoints[i];
            leg = this.limbsAndSegments[i];
            start_point = this.transform.position;
            end_point = Utils.IkSolveTwoSeg(
                this.transform.position,
                foot.transform.position,
                this.bodyController.forward,
                this.endpointToAttachmentLength * 0.5f
            );

            for (int j = 0; j < segment_count; j++)
            {
                segment = leg.segments[j];
                segment_length = end_point - start_point;
                angle = Vector3.SignedAngle(this.bodyController.forward, segment_length, Vector3.forward) + 90f;

                segment.transform.position = start_point + segment_length * 0.5f;
                segment.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                start_point = end_point;
                end_point = foot.transform.position;
            }
        }
    }

    private void PositionEndpointsAndAttachmentWithParent()
    {
        float half_spring_coef = this.attachmentSpringiness * 0.5f;
        if (this.hipDirection.magnitude > 1 + NEAR_ZERO) this.hipDirection.Normalize();
        if (this.focusPoint.magnitude > 1 + NEAR_ZERO) this.focusPoint.Normalize();

        Utils.ApplySpringResolveSingle(
            this.anchorToParentLength,
            half_spring_coef,
            this.transform.position,
            this.parent
        );
        Utils.ApplySpringResolveSingle(
            0f,
            half_spring_coef,
            this.parent.transform.position + this.hipDirection * this.anchorToParentLength,
            this.gameObject
        );
        Utils.ApplySpringResolveSingle(
            0f,
            half_spring_coef,
            this.transform.position + this.focusPoint * this.endpointToAttachmentLength,
            this.endpointController.gameObject
        );
    }

    public AutoLimbEndpoint EndpointController
    {
        get { return this.endpointController; }
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
