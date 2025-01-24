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
    protected Vector3 forward;
    protected AutoLimbEndpoint endpointController;
    protected AutoLimb bodyController;

    protected GameObject debugArrow;
    protected GameObject debugCircle;

    [Tooltip("Hip will lower to surface to resolve gate; distance to parent maintained by spring")]
    public GameObject parent;
    public bool influencesBodyPosition = false;

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
    [SerializeField]
    protected Limb[] limbsAndSegments;

    private void Start()
    {
        this.bodyController = this.GetBodyController(this.parent);
        this.endpointController = this.GetComponentInChildren<AutoLimbEndpoint>();

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
        this.forward = this.bodyController.forward;
        this.PositionEndpointsAndAttachmentWithParent();

        this.PositionEndpoints();

        if (this.limbsAndSegments.Length > 0 && this.limbsAndSegments[0].segments.Length > 0)
        {
            if (this.limbsAndSegments.Length != this.endpointController.Terminals.Length)
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
        Limb limb;
        GameObject terminal, segment;
        Vector3 segment_length, start_point, end_point;

        // TODO: support 1 and 3 or more segment legs
        if (segment_count != 2) throw new NotImplementedException("Only 2 segment legs are supported");

        for (int i = 0; i < this.endpointController.Terminals.Length; i++)
        {
            terminal = this.endpointController.Terminals[i].gameObject;
            limb = this.limbsAndSegments[i];
            start_point = this.transform.position;
            end_point = Utils.IkSolveTwoSeg(
                this.transform.position,
                terminal.transform.position,
                this.forward,
                this.endpointToAttachmentLength * 0.5f
            );

            for (int j = 0; j < segment_count; j++)
            {
                segment = limb.segments[j];
                segment_length = end_point - start_point;
                angle = Vector3.SignedAngle(this.forward, segment_length, Vector3.forward) + 90f;

                segment.transform.position = start_point + segment_length * 0.5f;
                segment.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                start_point = end_point;
                end_point = terminal.transform.position;
            }
        }
    }

    private void PositionEndpointsAndAttachmentWithParent()
    {
        float half_spring_coef = this.attachmentSpringiness * 0.5f;
        if (this.attachmentDirection.magnitude > 1 + NEAR_ZERO) this.attachmentDirection.Normalize();
        if (this.focusPoint.magnitude > 1 + NEAR_ZERO) this.focusPoint.Normalize();

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
