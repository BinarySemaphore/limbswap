using System;
using System.Net.Mime;
using UnityEngine;

public class AutoLimbHip : AutoLimbAttachment
{
    private RaycastHit2D lastSurfaceContact;

    [Range(0.01f, 0.99f)]
    public float gatePercent = 0.3f;
    [Range(0.01f, 0.99f)]
    public float liftPercent = 0.1f;

    protected override void Initialize()
    {
        float phase_step = Utils.FULL_TURN / this.endpointController.Terminals.Length;
        for (int i = 0; i < this.endpointController.Terminals.Length; i++)
        {
            this.endpointController.Terminals[i].PhaseOffset = i * phase_step;
        }
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

        // TODO: Maybe sketchy doing vector2 to vector3 assignment. For safety, probably best to clean up explicitly.
        if (this.lastSurfaceContact) surface_rel_pos_delta = contact.point - this.lastSurfaceContact.point;

        // Walking / Running
        if (contact)
        {
            if (surface_rel_pos_delta.magnitude > 0.05f)
            {

                Vector3 new_hip_position = this.transform.position - new Vector3(contact.point.x, contact.point.y, 0.0f);
                new_hip_position = new_hip_position.normalized * this.maxExtension * (1.0f - this.gatePercent);
                this.transform.position = new Vector3(contact.point.x, contact.point.y, 0.0f) + new_hip_position;

                surface_rel_pos_delta = Vector3.ProjectOnPlane(surface_rel_pos_delta, contact.normal + this.lastSurfaceContact.normal);
                this.UpdateFeetMoving(surface_rel_pos_delta, contact);
            }
            // Standing / Idle
            else
            {
                this.UpdateFeetStanding(contact);
            }
        }

        this.lastSurfaceContact = contact;
    }

    private void UpdateFeetStanding(RaycastHit2D contact)
    {
        GameObject foot;
        Vector3 new_foot_position;
        Vector3 contact_point_3d = new Vector3(contact.point.x, contact.point.y, this.endpointController.transform.position.z);

        for (int i = 0; i < this.endpointController.Terminals.Length; i++)
        {
            foot = this.endpointController.Terminals[i].gameObject;
            new_foot_position = foot.transform.position + 0.1f * (contact_point_3d - foot.transform.position);
            foot.transform.position = new_foot_position;
        }
    }

    private void UpdateFeetMoving(Vector3 delta, RaycastHit2D contact)
    {

        if (delta.magnitude < 0.01f) return;

        float phase;
        Vector3 new_foot_position;
        Vector3 contact_point_3d = new Vector3(contact.point.x, contact.point.y, this.endpointController.transform.position.z);
        Vector3 contact_normal_3d = new Vector3(contact.normal.x, contact.normal.y, this.endpointController.transform.position.z);
        Vector3 contact_tangent = Quaternion.AngleAxis(-90f, Vector3.forward) * contact_normal_3d;

        // Set to half line intersection of circle with diameter maxExtension
        // Need to calc distance to ground as gatePercent has changed it (could save time by estimating with maxExtension * gatePercent)
        float distance_to_ground = (this.transform.position - contact_point_3d).magnitude;
        float radius = Mathf.Sqrt(Mathf.Pow(this.maxExtension, 2f) - Mathf.Pow(distance_to_ground, 2f));

        foreach (AutoLimbTerminal terminal in this.endpointController.Terminals)
        {
            phase = Utils.Mod(this.clock + terminal.PhaseOffset, Utils.FULL_TURN);

            new_foot_position = contact_point_3d;
            new_foot_position += contact_tangent * (radius * Mathf.Cos(phase));
            if (phase <= Utils.HALF_TURN) new_foot_position += contact_normal_3d * (radius * this.liftPercent * Mathf.Sin(phase));

            terminal.gameObject.transform.position = new_foot_position;
        }
    }
}
