using System;
using UnityEngine;

public class AutoLimbHip : AutoLimbAttachment
{
    private RaycastHit2D lastSurfaceContact;
    private float currentPhase;
    private float phaseShift;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    private float gatePercent = 0.3f;

    protected override void Initialize()
    {
        this.debugArrow = GameObject.Find("debugArrow");
        this.currentPhase = 0f;
        this.phaseShift = Utils.FULL_TURN / this.endpointController.Endpoints.Length;
        // TODO: Allow some way for unity ui to specify phases (eg cheetah vs horse vs spider/crab vs robot)

        // Initialize each foot's state as pushing or lifting and phase.
        // This is very important to develop a cadence for the animation.
        // TODO: reimplement
    }

    protected override void PositionEndpoints()
    {
        Vector3 surface_rel_pos_delta = Vector3.zero;

        RaycastHit2D contact = Physics2D.Raycast(
            this.transform.position,
            this.focusPoint,
            this.maxExtension,
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
                new_hip_position = new_hip_position.normalized * this.maxExtension * (1.0f - this.gatePercent);
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

    private void UpdateFeetStanding(RaycastHit2D contact)
    {
        GameObject foot;
        Vector3 new_foot_position;
        Vector3 contact_point_3d = new Vector3(contact.point.x, contact.point.y, this.transform.position.z);

        for (int i = 0; i < this.endpointController.Endpoints.Length; i++)
        {
            foot = this.endpointController.Endpoints[i];
            new_foot_position = foot.transform.position + 0.1f * (contact_point_3d - foot.transform.position);
            foot.transform.position = new_foot_position;
        }
    }

    private void UpdateFeetMoving(Vector3 delta, RaycastHit2D contact)
    {
        float foot_phase;
        Vector3 contact_point_3d = new Vector3(contact.point.x, contact.point.y, this.transform.position.z);
        Vector3 new_foot_position;
        GameObject foot;

        if (delta.magnitude < 0.01f) return;

        // TODO: If/When updating foot travel path to circle/oval make sure this get's updated for percent covered circumference
        float distance_to_hip = (this.transform.position - contact_point_3d).magnitude;
        float surface_distance = 2f * Mathf.Sqrt(
            Mathf.Pow(this.maxExtension, 2f) -
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
        for (int i = 0; i < this.endpointController.Endpoints.Length; i++)
        {
            foot = this.endpointController.Endpoints[i];
            foot_phase = Utils.Mod(this.currentPhase + i * this.phaseShift,  2f * Mathf.PI);

            new_foot_position = new Vector3(
                Mathf.Cos(foot_phase) * surface_distance * 0.5f + contact_point_3d.x,
                Mathf.Sin(foot_phase) * surface_distance * 0.5f + contact_point_3d.y,
                contact_point_3d.z
            );
            if (foot_phase <= 2 * Mathf.PI && foot_phase > Mathf.PI)
            {
                new_foot_position = Vector3.ProjectOnPlane(new_foot_position - contact_point_3d, contact.normal) + contact_point_3d;
            }

            foot.transform.position = new_foot_position;
        }
    }
}
