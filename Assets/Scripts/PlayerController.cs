using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private const string GROUND_TAG = "Ground";
    private const string INPUT_JUMP = "Jump";
    private const string INPUT_HORIZONTAL = "Horizontal";

    private Rigidbody2D body;
    private bool on_ground;
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
        Vector2 move_delta = this.GetMoveDeltaFromInput();
        this.UpdateMovement(move_delta);

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

        if (this.on_ground)
        {
            // TODO: if want feet to move automatically with ground this is where to do it now
            // How to: calc clock ratio as target over 1 revolution per sec
            // target is angular velocity (radians per sec) from linear velocity of surface speed
            if (this.body.linearVelocity.x > 0.001f)
            {
                // TODO: remove direction by having controller flip entire body (please for all that is good do this sooner rather than later)
                this.procAnimatorBody.forward = Vector3.right;
                if (this.procAnimatorBody.shoulderControllers[0].clockRatio > 0)
                {
                    this.procAnimatorBody.shoulderControllers[0].clockRatio = -1f * this.procAnimatorBody.shoulderControllers[0].clockRatio;
                    this.procAnimatorBody.hipContollers[0].clockRatio = -1f * this.procAnimatorBody.hipContollers[0].clockRatio;
                }
                this.procAnimatorBody.shoulderControllers[0].Animate();
            }
            else if (this.body.linearVelocity.x < -0.001f)
            {
                // TODO: remove direction by having controller flip entire body (please for all that is good do this sooner rather than later)
                this.procAnimatorBody.forward = Vector3.left;
                if (this.procAnimatorBody.shoulderControllers[0].clockRatio < 0)
                {
                    this.procAnimatorBody.shoulderControllers[0].clockRatio = -1f * this.procAnimatorBody.shoulderControllers[0].clockRatio;
                    this.procAnimatorBody.hipContollers[0].clockRatio = -1f * this.procAnimatorBody.hipContollers[0].clockRatio;
                }
                this.procAnimatorBody.shoulderControllers[0].Animate();
            }
            this.procAnimatorBody.hipContollers[0].Animate();

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
