using System;
using UnityEngine;

public class AutoLimbShoulder : AutoLimbAttachment
{
    private Vector3 lastPosition;

    [Range(0.01f, 0.99f)]
    public float liftPercent = 0.2f;

    protected override void Initialize()
    {
        this.lastPosition = this.bodyController.transform.position;

        float phase_step = Utils.FULL_TURN / this.endpointController.Terminals.Length;
        for (int i = 0; i < this.endpointController.Terminals.Length; i++)
        {
            this.endpointController.Terminals[i].Phase = i * phase_step;
        }
    }

    protected override void PositionEndpoints()
    {
        this.forward = -this.forward;

        Vector3 delta = Vector3.Project(this.bodyController.transform.position - this.lastPosition, this.forward);
        this.UpdateHandsReciprocating(delta);

        this.lastPosition = this.bodyController.transform.position;
    }

    private void UpdateHandsReciprocating(Vector3 delta)
    {
        Vector3 new_hand_position;

        Vector3 normal = this.transform.position - this.bodyController.transform.position;
        float radius = this.endpointToAttachmentLength * 0.5f;
        float circumferance = Utils.FULL_TURN * radius * this.liftPercent;
        // Angle is radians full circle * ratio; ratio is distance over circumferance
        // Simplified from 2f * Mathf.PI * (delta.magnitude / (surface_distance * Mathf.PI))
        float phase_delta = 2f * (delta.magnitude / circumferance);

        float phase_direction = Vector3.Cross(normal, delta).z;
        if (phase_direction < 0) phase_delta = -phase_delta;

        foreach (AutoLimbTerminal terminal in this.endpointController.Terminals)
        {
            new_hand_position = this.endpointController.transform.position;

            terminal.Phase += phase_delta;
            new_hand_position.x += radius * Mathf.Cos(terminal.Phase);
            new_hand_position.y += this.liftPercent * radius * Mathf.Sin(terminal.Phase);

            terminal.gameObject.transform.position = new_hand_position;
        }
    }
}
