using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private const string GROUND_TAG = "Ground";
    private const string INPUT_JUMP = "Jump";
    private const string INPUT_HORIZONTAL = "Horizontal";

    private Rigidbody2D body;
    private bool on_ground;
    private bool needSetFeetPhase;
    private int extend_jump;

    [SerializeField]
    private SpriteRenderer sprite;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private AutoLimb procAnimatorBody;
    [SerializeField]
    private float speed = 20f;
    [SerializeField]
    private float in_air_speed = 5f;
    [SerializeField]
    private float jump_speed = 20f;
    [SerializeField]
    private int slice_jump = 5;
    [SerializeField]
    private float max_horizontal_speed = 10f;
    [SerializeField]
    private float max_vertical_speed = 10f;

    // Start is called before the first frame update
    void Start()
    {
        this.body = this.GetComponent<Rigidbody2D>();
        this.on_ground = false;
        this.extend_jump = 0;
        this.jump_speed /= this.slice_jump;
        this.needSetFeetPhase = true;
        //this.procAnimatorBody.hipContollers[0].EndpointController.Terminals[1].PhaseOffset = Utils.QUARTER_TURN * 0.25f;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!this.on_ground && collision.CompareTag(GROUND_TAG))
        {
            this.on_ground = true;
            this.extend_jump = 0;
        }
    }

    private void FixedUpdate()
    {
        if (this.needSetFeetPhase)
        {
            this.needSetFeetPhase = false;
            // For front
            this.procAnimatorBody.hipContollers[1].EndpointController.Terminals[0].PhaseOffset = Utils.HALF_TURN;
            this.procAnimatorBody.hipContollers[1].EndpointController.Terminals[1].PhaseOffset = Utils.HALF_TURN + Utils.QUARTER_TURN * 0.5f;
            // For back
            this.procAnimatorBody.hipContollers[0].EndpointController.Terminals[1].PhaseOffset = Utils.QUARTER_TURN * 0.25f;
        }

        Vector2 move_delta = this.GetMoveDeltaFromInput();
        this.UpdateMovement(move_delta);

        float target_angle = 90f;
        RaycastHit2D contact = Physics2D.Raycast(this.transform.position, Vector2.down, 2f, LayerMask.GetMask("Surface"));
        if (contact)
        {
            target_angle += Vector2.SignedAngle(Vector2.up, contact.normal);
            if (Mathf.Abs(this.transform.rotation.eulerAngles.z - target_angle) > 0.01f)
            {
                this.transform.rotation = Quaternion.Euler(
                    this.transform.rotation.eulerAngles.x,
                    this.transform.rotation.eulerAngles.y,
                    Utils.Mod(Utils.SpringResolve(0.15f, this.transform.rotation.eulerAngles.z, target_angle), 360f)
                );
            }
        }
        else if (Mathf.Abs(this.transform.rotation.eulerAngles.z - target_angle) > 0.01f)
        {
            this.transform.rotation = Quaternion.Euler(
                this.transform.rotation.eulerAngles.x,
                this.transform.rotation.eulerAngles.y,
                Utils.Mod(Utils.SpringResolve(0.15f, this.transform.rotation.eulerAngles.z, target_angle), 360f)
            );
        }
        //this.transform.Rotate(new Vector3(0f, 0f, 0.1f));
        //this.transform.rotation = Quaternion.Euler(0f, 0f, this.transform.rotation.eulerAngles.z + 0.1f);


        // Clear/reset triggered bools for next frame
        this.on_ground = false;
    }

    private Vector2 GetMoveDeltaFromInput()
    {
        float horizontal_intput = Input.GetAxis(INPUT_HORIZONTAL);
        Vector2 move_delta = Vector2.zero;

        if ((this.on_ground || this.extend_jump > 0) && Input.GetAxis(INPUT_JUMP) > 0)
        {
            if (this.extend_jump > 0) this.extend_jump -= 1;
            if (this.on_ground) this.extend_jump = this.slice_jump;
            move_delta.y += this.jump_speed;
        }
        if (Mathf.Abs(horizontal_intput) > 0.1f)
        {
            if (this.on_ground) move_delta.x += horizontal_intput * this.speed;
            else move_delta.x += horizontal_intput * this.in_air_speed;
        }

        return move_delta;
    }

    private void UpdateMovement(Vector2 delta)
    {
        // Update velocity (counteract horizontal retained velocity if on ground)
        if (this.on_ground) delta.x += -this.body.linearVelocity.x;
        this.body.linearVelocity += new Vector2(delta.x, delta.y);

        // Limit velocity
        this.body.linearVelocity = new Vector2(
            Mathf.Clamp(this.body.linearVelocity.x, -this.max_horizontal_speed, this.max_horizontal_speed),
            Mathf.Clamp(this.body.linearVelocity.y, -this.max_vertical_speed * 10f, this.max_vertical_speed)
        );

        // Trigger animations
        if (this.sprite)
        {
            if (this.body.linearVelocity.x > 0.001f) this.sprite.flipX = true;
            else if (this.body.linearVelocity.x < -0.001f) this.sprite.flipX = false;
        }

        if (this.on_ground && this.procAnimatorBody != null)
        {
            if (this.body.linearVelocity.x > 0.001f)
            {
                this.procAnimatorBody.Forward = Vector3.right;
                if (this.procAnimatorBody.clockSpeed > 0)
                {
                    this.procAnimatorBody.clockSpeed *= -1f;
                    // For front
                    this.procAnimatorBody.hipContollers[1].gatePercent = 0.25f;
                    this.procAnimatorBody.hipContollers[1].EndpointController.Terminals[0].PhaseOffset = Utils.HALF_TURN;
                    this.procAnimatorBody.hipContollers[1].EndpointController.Terminals[1].PhaseOffset = Utils.HALF_TURN + Utils.QUARTER_TURN * 0.5f;
                    // For back
                    this.procAnimatorBody.hipContollers[0].gatePercent = 0.4f;
                    this.procAnimatorBody.hipContollers[0].EndpointController.Terminals[0].PhaseOffset = 0f;
                    this.procAnimatorBody.hipContollers[0].EndpointController.Terminals[1].PhaseOffset = Utils.QUARTER_TURN * 0.25f;
                }
            }
            else if (this.body.linearVelocity.x < -0.001f)
            {
                this.procAnimatorBody.Forward = Vector3.left;
                if (this.procAnimatorBody.clockSpeed < 0)
                {
                    this.procAnimatorBody.clockSpeed *= -1f;
                    // For front
                    this.procAnimatorBody.hipContollers[1].gatePercent = 0.4f;
                    this.procAnimatorBody.hipContollers[1].EndpointController.Terminals[0].PhaseOffset = 0f;
                    this.procAnimatorBody.hipContollers[1].EndpointController.Terminals[1].PhaseOffset = Utils.QUARTER_TURN * 0.25f;
                    // For back
                    this.procAnimatorBody.hipContollers[0].gatePercent = 0.25f;
                    this.procAnimatorBody.hipContollers[0].EndpointController.Terminals[0].PhaseOffset = Utils.HALF_TURN;
                    this.procAnimatorBody.hipContollers[0].EndpointController.Terminals[1].PhaseOffset = Utils.HALF_TURN + Utils.QUARTER_TURN * 0.5f;
                }
            }
            //this.procAnimatorBody.hipContollers[0].Animate();
            //this.procAnimatorBody.hipContollers[1].Animate();

            if (animator)
            {
                float ground_speed = Mathf.Abs(this.body.linearVelocity.x);
                if (ground_speed < 0.001f) this.animator.SetTrigger("Idle");
                else if (ground_speed < this.max_horizontal_speed * 0.75f) this.animator.SetTrigger("Walking");
                else this.animator.SetTrigger("Running");
            }
        }
        else
        {
            if (animator) this.animator.SetTrigger("InAir");
        }
    }
}
