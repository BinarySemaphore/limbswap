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
    private float[] jump_speed_slices;

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

        // Bake a list of jump speeds into list starting smaller to larger by cutting total in half each time.
        // Will be ref-ed in reverse order, so actual jump starts strong and if continued provides less and less velocity.
        //float cur_jump_slice_speed = this.jump_speed;
        //this.jump_speed_slices = new float[this.slice_jump];
        //for (int i = this.slice_jump - 1; i > 3; i--)
        //{
        //    cur_jump_slice_speed *= 0.25f;
        //    this.jump_speed_slices[i] = cur_jump_slice_speed;
        //}
        //// Add last one in to fill out, so sum of array equals original jump_speed.
        //this.jump_speed_slices[0] = cur_jump_slice_speed;
        //this.jump_speed_slices[1] = cur_jump_slice_speed;
        //this.jump_speed_slices[2] = cur_jump_slice_speed;
        //this.jump_speed_slices[3] = cur_jump_slice_speed;
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
            move_delta.y += this.jump_speed;//this.jump_speed_slices[this.extend_jump];
        }
        if (Mathf.Abs(horizontal_intput) > 0.1f)
        {
            if (on_ground) move_delta.x += horizontal_intput * this.speed;
            else move_delta.x += horizontal_intput * this.in_air_speed;
        }

        return move_delta;
    }

    private void UpdateMovement(Vector2 delta)
    {
        // Update velocity (counteract horizontal retained velocity if on ground)
        if (on_ground) delta.x += -this.body.linearVelocity.x;
        this.body.linearVelocity += new Vector2(delta.x, delta.y);

        // Limit velocity
        this.body.linearVelocity = new Vector2(
            Mathf.Clamp(this.body.linearVelocity.x, -this.max_horizontal_speed, this.max_horizontal_speed),
            Mathf.Clamp(this.body.linearVelocity.y, -this.max_vertical_speed * 10f, this.max_vertical_speed)
        );
    }
}
