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

    public GameObject parent;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    private float targetAttachmentSpringiness = 0.7f;
    [SerializeField]
    private AutoLimbHip[] hipContollers;

    private void Start()
    {
        this.bodyToTargetLength = (this.transform.position - parent.transform.position).magnitude;
    }

    private void FixedUpdate()
    {
        Utils.ApplySpringResolveSingle(
            this.bodyToTargetLength,
            this.targetAttachmentSpringiness,
            this.parent.transform.position,
            this.gameObject
        );
    }
}
