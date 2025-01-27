using System;
using UnityEngine;

public class AutoLimbShoulder : AutoLimbAttachment
{
    private Vector3 lastPosition;

    [Range(0.01f, 0.99f)]
    public float liftPercent = 0.2f;
    public float pathModifier = 0.5f;
    public float speedModifier = 0.5f;

    protected override void Initialize()
    {
        this.lastPosition = this.bodyController.transform.position;

        float phase_step = Utils.FULL_TURN / this.endpointController.Terminals.Length;
        for (int i = 0; i < this.endpointController.Terminals.Length; i++)
        {
            this.endpointController.Terminals[i].PhaseOffset = i * phase_step;
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
        float phase;

        Vector3 normal = this.transform.position - this.bodyController.transform.position;
        float radius = this.endpointToAttachmentLength * 0.5f * this.pathModifier;

        // Move hands in front of body
        this.endpointController.transform.position = new Vector3(
            this.bodyController.forward.x * 0.1f + this.transform.position.x,
            this.endpointController.transform.position.y,
            this.endpointController.transform.position.z
        );

        foreach (AutoLimbTerminal terminal in this.endpointController.Terminals)
        {
            if (!terminal.enabled) continue;

            phase = Utils.Mod(this.clock + terminal.PhaseOffset, Utils.FULL_TURN);

            new_hand_position = this.endpointController.transform.position;
            new_hand_position.x += radius * Mathf.Cos(phase);
            new_hand_position.y += this.liftPercent * radius * Mathf.Sin(phase);

            terminal.gameObject.transform.position = new_hand_position;
        }
    }
}
