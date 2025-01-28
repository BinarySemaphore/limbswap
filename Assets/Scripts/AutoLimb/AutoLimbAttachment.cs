using System;
using UnityEngine;

public class AutoLimbAttachment : MonoBehaviour
{
    protected const float ENDPOINT_EXTENSION = 1.2f;
    protected const float NEAR_ZERO = 0.0000001f;

    protected bool animateCalled = false;
    protected float maxExtension;
    protected float anchorToParentLength;
    protected float endpointToAttachmentLength;
    protected Vector3 ikForward;
    protected AutoLimbEndpoint endpointController;
    protected AutoLimb bodyController;

    protected GameObject debugArrow;
    protected GameObject debugCircle;

    private Quaternion lastParentRotation = Quaternion.identity;


    [HideInInspector]
    public float clock;

    [Tooltip("Hip will lower to surface to resolve gate; distance to parent maintained by spring")]
    public GameObject parent;
    public bool influencesBodyPosition = false;
    [Range(-10f, 10f)]
    public float clockRatio = 1f;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    protected float attachmentSpringiness = 0.30f;
    [SerializeField]
    [Tooltip("Leave zero to autoset on spawn")]
    protected Vector3 attachmentDirection = Vector3.zero;
    [SerializeField]
    [Tooltip("Leave zero to autoset on spawn")]
    protected Vector3 focusPoint = Vector3.zero;
    [SerializeField]
    protected AutoLimbState state = AutoLimbState.Paused;

    public Limb[] limbsAndSegments;

    private void Start()
    {
        this.clock = 0f;
        // TODO: switch to initialize start if any public is called (same way doing for populate controllers)
        this.populateControllers();

        Vector3 parent_to_attachment = this.transform.position - this.parent.transform.position;
        Vector3 attachment_to_endpoint = this.endpointController.transform.position - this.transform.position;

        this.anchorToParentLength = parent_to_attachment.magnitude;
        this.endpointToAttachmentLength = attachment_to_endpoint.magnitude;

        if (this.attachmentDirection == Vector3.zero) this.attachmentDirection = parent_to_attachment.normalized;
        if (this.focusPoint == Vector3.zero) this.focusPoint = attachment_to_endpoint.normalized;

        this.maxExtension = this.endpointToAttachmentLength * ENDPOINT_EXTENSION;

        this.Initialize();
    }

    private void FixedUpdate()
    {
        if (this.state != AutoLimbState.Engaged) return;
        this.clock += this.bodyController.deltaClock * this.clockRatio;
        this.clock = Utils.Mod(this.clock, Utils.FULL_TURN);

        this.ikForward = this.bodyController.Forward;
        this.PositionEndpointsAndAttachmentWithParent();

        this.PositionEndpoints();

        if (this.limbsAndSegments.Length > 0)
        {
            if (this.limbsAndSegments.Length != this.endpointController.Terminals.Length)
            {
                throw new Exception("Limbs And Segments, number of limbs needs to match endpoint's number of children");
            }
            ConstructEndpointsAndSegments();
        }

        if (this.animateCalled)
        {
            this.animateCalled = false;
            this.state = AutoLimbState.Paused;
        }
    }
    private void populateControllers()
    {
        if (this.bodyController == null) this.bodyController = this.GetBodyController(this.parent);
        if (this.endpointController == null) this.endpointController = this.GetComponentInChildren<AutoLimbEndpoint>();
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
        int segment_count;
        float angle;
        float z_depth;
        Limb limb;
        GameObject terminal, segment;
        Vector3 start_to_end, start_point, end_point, z_depth_added;

        for (int i = 0; i < this.endpointController.Terminals.Length; i++)
        {
            if (!this.endpointController.Terminals[i].enabled) continue;

            segment_count = this.limbsAndSegments[i].segments.Count;
            if (segment_count == 0) continue;

            if (this.limbsAndSegments[i].rightSide) z_depth = -0.1f;
            else z_depth = 0.1f;

            // TODO: support 3 or more segment legs
            if (segment_count > 2) throw new NotImplementedException("Only 2 segment are supported");

            terminal = this.endpointController.Terminals[i].gameObject;
            limb = this.limbsAndSegments[i];
            start_point = this.transform.position;
            if (segment_count == 1) end_point = terminal.transform.position;
            else
            {
                end_point = Utils.IkSolveTwoSeg(
                    this.transform.position,
                    terminal.transform.position,
                    this.ikForward,
                    this.endpointToAttachmentLength * 0.5f
                );
            }

            z_depth_added = terminal.transform.position;
            z_depth_added.z = this.transform.position.z + z_depth;
            terminal.transform.position = z_depth_added;

            for (int j = 0; j < segment_count; j++)
            {
                segment = limb.segments[j];
                start_to_end = end_point - start_point;
                angle = Vector3.SignedAngle(this.bodyController.Forward, start_to_end, Vector3.forward);

                segment.transform.position = start_point + start_to_end * 0.5f;
                z_depth_added = segment.transform.position;
                z_depth_added.z = this.transform.position.z + z_depth;
                segment.transform.position = z_depth_added;
                segment.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                start_point = end_point;
                end_point = terminal.transform.position;
            }
        }
    }

    private void PositionEndpointsAndAttachmentWithParent()
    {
        // TODO: fix rel rotational issues when springing to parent
        float half_spring_coef = this.attachmentSpringiness * 0.5f;
        if (this.attachmentDirection.magnitude > 1 + NEAR_ZERO) this.attachmentDirection.Normalize();
        if (this.focusPoint.magnitude > 1 + NEAR_ZERO) this.focusPoint.Normalize();

        //Vector3 attachmentDirectionRotated = this.bodyController.parent.transform.rotation * this.attachmentDirection;

        if (this.lastParentRotation != this.bodyController.transform.rotation)
        {
            Quaternion changeInRotation = this.bodyController.transform.rotation * Quaternion.Inverse(this.lastParentRotation);
            this.attachmentDirection = changeInRotation * this.attachmentDirection;
            this.lastParentRotation = this.bodyController.transform.rotation;
        }

        if (!this.influencesBodyPosition)
        {
            Utils.ApplySpringResolveSingle(
                0f,
                this.attachmentSpringiness,
                this.parent.transform.position + this.attachmentDirection * this.anchorToParentLength,
                this.gameObject
            );
        }
        else
        {
            Utils.ApplySpringResolveSingle(
                this.anchorToParentLength,
                half_spring_coef,
                this.transform.position,
                this.parent
            );
            Utils.ApplySpringResolveSingle(
                0f,
                half_spring_coef,
                this.parent.transform.position + this.attachmentDirection * this.anchorToParentLength,
                this.gameObject
            );
        }
        Utils.ApplySpringResolveSingle(
            0f,
            half_spring_coef,
            this.transform.position + this.focusPoint * this.endpointToAttachmentLength,
            this.endpointController.gameObject
        );
    }

    public void syncClock()
    {
        this.clock = this.bodyController.clock;
    }

    public void NudgeAfterChange()
    {
        this.ConstructEndpointsAndSegments();
    }

    public AutoLimbEndpoint EndpointController
    {
        get
        {
            if ( this.endpointController == null) this.populateControllers();
            return this.endpointController;
        }
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
    /// Directly set Animation <see cref="AutoLimbState"/>.<br/>
    /// </summary>
    /// <remarks>
    /// Cannot Engage if Disabled, must directly set as Paused first.
    /// </remarks>
    public AutoLimbState AnimateState
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
    /// Active call to Engage Animation <see cref="AutoLimbState"/> on this frame.<br/>
    /// Will enter Paused <see cref="AutoLimbState"/> on frame end.<br/>
    /// </summary>
    /// <remarks>
    /// Cannot Engage if Disabled, must directly set as Paused first.<br/>
    /// Use <see cref="AnimateState"/> to toggle state from triggers.
    /// </remarks>
    public void Animate()
    {
        if (this.state != AutoLimbState.Engaged && !this.animateCalled)
        {
            this.animateCalled = true;
            this.state = AutoLimbState.Engaged;
        }
    }
}
