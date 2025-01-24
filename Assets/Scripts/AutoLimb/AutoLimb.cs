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

    [HideInInspector]
    public float clock;
    [HideInInspector]
    public float deltaClock;

    public GameObject parent;
    public Vector3 forward = Vector3.right;

    public AutoLimbShoulder[] shoulderControllers;
    public AutoLimbHip[] hipContollers;

    [SerializeField]
    [Range(0f, 10f)]
    private float clockSpeed = 1f;
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
}
