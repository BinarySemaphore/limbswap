using System;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public enum AutoLimbState
{
    Disabled,
    Paused,
    Engaged
}

public class AutoLimb : MonoBehaviour
{
    private float bodyToTargetLength;

    [HideInInspector]
    public float clock;
    [HideInInspector]
    public float deltaClock;

    public GameObject parent;
    private Vector3 forward = Vector3.right;

    public AutoLimbShoulder[] shoulderControllers;
    public AutoLimbHip[] hipContollers;

    [Range(0f, 10f)]
    public float clockSpeed = 1f;

    public bool autoFlip = true;
    public bool autoFlipRatios = true;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    private float targetAttachmentSpringiness = 0.7f;

    private void Start()
    {
        this.clock = 0f;
        this.bodyToTargetLength = (this.transform.position - parent.transform.position).magnitude;
    }

    private void FixedUpdate()
    {
        this.deltaClock = Time.deltaTime * this.clockSpeed * Utils.FULL_TURN;
        this.clock += this.deltaClock;
        this.clock = Utils.Mod(this.clock, Utils.FULL_TURN);
        Utils.ApplySpringResolveSingle(
            this.bodyToTargetLength,
            this.targetAttachmentSpringiness,
            this.parent.transform.position,
            this.gameObject
        );
    }

    public Vector3 Forward
    {
        get { return forward; }
        set
        {
            // If changed use scale to flip as needed and reposition for z-depth (this applies to everything)
            if (this.forward != value)
            {
                Vector3 new_scale;
                Vector3 change_normal = (value.normalized - this.forward).normalized;
                this.forward = value.normalized;

                if (!this.autoFlip) return;

                bool flip_x = Mathf.Abs(change_normal.x) >= Utils.SQRT_HALF;
                bool flip_y = Mathf.Abs(change_normal.y) >= Utils.SQRT_HALF;

                new_scale = this.transform.localScale;
                if (flip_x) new_scale.x *= -1f;
                if (flip_y) new_scale.y *= -1f;
                this.transform.localScale = new_scale;

                foreach (AutoLimbShoulder shoulder in this.shoulderControllers)
                {
                    new_scale = shoulder.transform.localScale;
                    if (flip_x) new_scale.x *= -1f;
                    if (flip_y) new_scale.y *= -1f;
                    shoulder.transform.localScale = new_scale;
                    if (flip_x ^ flip_y)
                    {
                        if (this.autoFlipRatios) shoulder.clockRatio *= -1f;
                        shoulder.transform.position = new Vector3(
                            shoulder.transform.position.x,
                            shoulder.transform.position.y,
                            shoulder.transform.position.z * -1f
                        );
                        foreach (Limb limb in shoulder.limbsAndSegments) limb.rightSide = !limb.rightSide;
                    }
                }

                foreach (AutoLimbHip hip in this.hipContollers)
                {
                    new_scale = hip.transform.localScale;
                    if (flip_x) new_scale.x *= -1f;
                    if (flip_y) new_scale.y *= -1f;
                    hip.transform.localScale = new_scale;
                    if (flip_x ^ flip_y)
                    {
                        if (this.autoFlipRatios) hip.clockRatio *= -1f;
                        hip.transform.position = new Vector3(
                            hip.transform.position.x,
                            hip.transform.position.y,
                            hip.transform.position.z * -1f
                        );
                        foreach (Limb limb in hip.limbsAndSegments) limb.rightSide = !limb.rightSide;
                    }
                }
            }
        }
    }
}
