using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum WeaponTypes
{
    None,
    Arm,
    Leg,
    Club,
    Knife,
    Sword,
    Staff,
    Pistol,
    SMG,
    MG,
    LMG,
    Rifle,
    RifleHeavy,
    MountedMG,
    RocketLight,
    RocketMedium,
    RocketHeavy,
    RocketCluster,
    Cannon,
    Jet,
    SheildLight,
    ShieldMedium,
    ShieldHeavy
}

[Serializable]
public class AttackHandler
{
    private float time;
    private Vector3 source;
    private Vector3 target;

    public bool cancelAnimation = false;
    [SerializeReference]
    public Limb parentLimb;
    public WeaponTypes type;
    [Tooltip("Prefabs required for animations and/or projectiles")]
    public GameObject[] prefabs;

    public Func<bool> Attack()
    {
        this.time = 0f;
        // TODO: no ideas where to put hit conditions or what to share
        if (this.type == WeaponTypes.Arm)
        {
            // TODO: apply damage...
            bool animationMethod() => this.PunchAnimation();
            return animationMethod;
        }
        return null;
    }

    private bool TemplateAnimation()
    {
        if (this.cancelAnimation || this.parentLimb == null || this.parentLimb.endpoint == null) return true;
        bool complete = true;
        float round_time = Mathf.Round(this.time * 10f) / 10f;
        if (this.time == 0f) this.parentLimb.Decoupled = true;

        if (this.time < 5f) // Set time limit
        {
            // Do something... (use round_time for easier time checks)
            complete = false;
        }

        // Continue for next call
        if (!complete)
        {
            this.time += Time.deltaTime;
            return false;
        }

        // Clean up
        this.parentLimb.Decoupled = false;
        return true;
    }

    private bool PunchAnimation()
    {
        // TODO: make a keyframe system and iterate time through it interpolating between
        if (this.cancelAnimation || this.parentLimb == null || this.parentLimb.endpoint == null) return true;
        bool complete = true;
        float round_time = Mathf.Round(this.time * 10f) / 10f;
        if (this.time == 0f) this.parentLimb.Decoupled = true;

        if (this.time < 2f) // Set time limit
        {
            if (round_time == 0f)
            {
                AutoLimb bodyController = this.parentLimb.endpoint.transform.parent.parent.gameObject.GetComponent<AutoLimbAttachment>().bodyController;
                Vector3 center_point = bodyController.transform.position + Vector3.right;
                Vector3 local_center = center_point - this.parentLimb.endpoint.transform.position;

                this.source = this.parentLimb.endpoint.transform.localPosition;
                this.target = local_center;
            }
            this.parentLimb.endpoint.transform.localPosition = new Vector3(
                Utils.LerpBounceBack(this.source.x, this.target.x, this.time * 0.5f),
                Utils.LerpBounceBack(this.source.y, this.target.y, this.time * 0.5f),
                0f
            );
            complete = false;
        }

        // Continue for next call
        if (!complete)
        {
            this.time += Time.deltaTime;
            return false;
        }

        // Clean up
        this.parentLimb.Decoupled = false;
        return true;
    }
}
