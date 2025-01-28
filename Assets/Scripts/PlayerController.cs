using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{
    private const string GROUND_TAG = "Ground";
    private const string INPUT_JUMP = "Jump";
    private const string INPUT_HORIZONTAL = "Horizontal";

    private bool load_appendages;
    private bool on_ground;
    private bool movingRight;
    private float stopTimer;
    private float stopPercent;
    private float walkTimer;
    private float walkPercent;
    private float runTimer;
    private float runPercent;
    private float velocityToReachJumpHeight;
    private float hangTimer;
    private float hangPercent;
    private float originalGravity;
    private Rigidbody2D body;

    [SerializeField]
    private float timeToStop = 0.1f;
    [SerializeField]
    private float walkSpeed = 5f;
    [SerializeField]
    private float timeToReachWalk = 0.5f;
    [SerializeField]
    private float runSpeed = 10f;
    [SerializeField]
    private float timeToReachRun = 2.0f;
    [SerializeField]
    private float jumpHeight = 3f;
    [SerializeField]
    private float timeToReachJumpHeight = 0.5f;
    [SerializeField]
    private float allowedHangTime = 0.25f;
    [SerializeField]
    private float inAirControlPercent = 0.8f;
    [SerializeField]
    private float maxHorrizontalSpeed = 20f;
    [SerializeField]
    private float maxVerticalSpeed = 20f;
    [SerializeField]
    private AutoLimb procAnimatorBody;
    [SerializeField]
    private PlayerPickupSelector selector;
    [SerializeField]
    private GameObject pickupPrefab;
    [SerializeField]
    private GameObject[] AttachmentPrefabList;

    // Start is called before the first frame update
    void Start()
    {
        this.load_appendages = true;
        this.on_ground = false;
        this.body = this.GetComponent<Rigidbody2D>();
        //this.extend_jump = 0;
        //this.jumpAcceleration /= this.sliceJumpFrames;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!this.on_ground && collision.CompareTag(GROUND_TAG))
        {
            this.on_ground = true;
            //this.extend_jump = 0;
        }
    }

    private void Update()
    {
        //Vector2 move_delta = this.GetMoveDeltaFromInput();
        //this.UpdateMovement(move_delta);

        if (Input.GetKeyUp(KeyCode.Comma))
        {
            AutoLimbAttachment controller;
            int limb_index = Random.Range(0, 2);
            if (Random.Range(0, 2) == 1) controller = this.procAnimatorBody.shoulderControllers[0];
            else controller = this.procAnimatorBody.hipContollers[0];

            if (!controller.EndpointController.Terminals[limb_index].enabled)
            {
                for (int i = 0; i < controller.EndpointController.Terminals.Length; i++)
                {
                    limb_index = (limb_index + 1) % controller.EndpointController.Terminals.Length;
                    if (controller.EndpointController.Terminals[limb_index].enabled) break;
                }
            }
            if (!controller.EndpointController.Terminals[limb_index].enabled)
            {
                if (controller == this.procAnimatorBody.shoulderControllers[0]) controller = this.procAnimatorBody.hipContollers[0];
                else controller = this.procAnimatorBody.shoulderControllers[0];
                for (int i = 0; i < controller.EndpointController.Terminals.Length; i++)
                {
                    limb_index = i;
                    if (controller.EndpointController.Terminals[limb_index].enabled) break;
                }
            }
            if (!controller.EndpointController.Terminals[limb_index].enabled) return;

            this.DropLimb(controller, limb_index);
            controller.limbsAndSegments[limb_index].prefab = null;
        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            int i;
            // pick up pickup...
            Pickup pickup = this.selector.PickupSelection();
            if (pickup != null && pickup.item != null)
            {
                if (pickup.isAttachment)
                {
                    int limb_index = -1;
                    AutoLimbAttachment controller = this.procAnimatorBody.hipContollers[0];
                    for (i = 0; i < controller.EndpointController.Terminals.Length; i++)
                    {
                        if (!controller.EndpointController.Terminals[i].enabled)
                        {
                            limb_index = i;
                            break;
                        }
                    }
                    if (limb_index < 0)
                    {
                        controller = this.procAnimatorBody.shoulderControllers[0];
                        for (i = 0; i < controller.EndpointController.Terminals.Length; i++)
                        {
                            if (!controller.EndpointController.Terminals[i].enabled)
                            {
                                limb_index = i;
                                break;
                            }
                        }
                    }
                    if (limb_index >= 0)
                    {
                        for (i = 0; i < this.AttachmentPrefabList.Length; i++)
                        {
                            if (pickup.item.name.StartsWith(this.AttachmentPrefabList[i].name))
                            {
                                controller.limbsAndSegments[limb_index].prefab = this.AttachmentPrefabList[i];
                                break;
                            }
                        }
                        this.AddLimb(controller, limb_index, pickup.item);
                        Destroy(pickup.gameObject);
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (this.load_appendages)
        {
            this.load_appendages = false;
            int index_count;
            foreach (AutoLimbShoulder shoulder in this.procAnimatorBody.shoulderControllers)
            {
                index_count = 0;
                foreach (Limb limb in shoulder.limbsAndSegments)
                {
                    this.AddLimbFromPrefab(shoulder, index_count, limb.prefab);
                    index_count += 1;
                }
            }
            foreach (AutoLimbHip hip in this.procAnimatorBody.hipContollers)
            {
                index_count = 0;
                foreach (Limb limb in hip.limbsAndSegments)
                {
                    this.AddLimbFromPrefab(hip, index_count, limb.prefab);
                    index_count += 1;
                }
            }
        }

        this.UpdateMovement();

        // Clear/reset triggered bools for next frame
        this.on_ground = false;
    }

    private void GetMoveDeltaFromInput()
    {
        float horizontal_intput = Input.GetAxis(INPUT_HORIZONTAL);
        float vertial_input = Input.GetAxis(INPUT_JUMP);

        if (horizontal_intput > 0f) this.movingRight = true;
        else if (horizontal_intput < 0f) this.movingRight = false;

        if (this.on_ground)
        {
            if (Mathf.Abs(horizontal_intput) > Utils.NEAR_ZERO_LOOSE)
            {
                this.stopTimer = 0f;
                if (this.walkPercent < 1f) this.walkTimer += Time.deltaTime;
                else if (this.runPercent < 1f) this.runTimer += Time.deltaTime;
            }
            else
            {
                this.walkTimer = 0f;
                this.runTimer = 0f;
            }
        }

        //Vector2 move_delta = Vector2.zero;

        //if ((this.on_ground || this.extend_jump > 0) && Input.GetAxis(INPUT_JUMP) > 0)
        //{
        //    if (this.extend_jump > 0) this.extend_jump -= 1;
        //    if (this.on_ground) this.extend_jump = this.sliceJumpFrames;
        //    move_delta.y += this.jumpAcceleration * Time.deltaTime;
        //}
        //if (Mathf.Abs(horizontal_intput) > 0.1f)
        //{
        //    if (this.on_ground) move_delta.x += horizontal_intput * this.acceleration * Time.deltaTime;
        //    else move_delta.x += horizontal_intput * this.inAirAcceleration * Time.deltaTime;
        //}

        //return move_delta;
    }

    private void UpdateMovement()
    {
        // Update velocity (counteract horizontal retained velocity if on ground)
        //if (this.on_ground) delta.x += -this.body.linearVelocity.x;
        //this.body.linearVelocity += new Vector2(delta.x, delta.y);

        // Limit acceleraiton from input
        Vector2 new_velocity = this.body.linearVelocity;
        if (Mathf.Abs(new_velocity.x + delta.x * Time.deltaTime) <= this.maxHorrizontalSpeed) new_velocity.x += delta.x * Time.deltaTime;
        if (Mathf.Abs(new_velocity.y + delta.y * Time.deltaTime) <= this.maxVerticalSpeed) new_velocity.y += delta.y * Time.deltaTime;
        this.body.linearVelocity = new_velocity;

        // Limit velocity
        //this.body.linearVelocity = new Vector2(
        //    Mathf.Clamp(this.body.linearVelocity.x, -this.maxHorrizontalSpeed, this.maxHorrizontalSpeed),
        //    Mathf.Clamp(this.body.linearVelocity.y, -this.maxVerticalSpeed * 10f, this.maxVerticalSpeed)
        //);

        // Trigger animations
        if (this.body.linearVelocity.x > Utils.NEAR_ZERO_LOOSE) this.procAnimatorBody.Forward = Vector3.right;
        else if (this.body.linearVelocity.x < -Utils.NEAR_ZERO_LOOSE) this.procAnimatorBody.Forward = Vector3.left;

        if (this.on_ground)
        {
            // TODO: if want feet to move automatically with ground this is where to do it now
            // How to: calc clock ratio as target over 1 revolution per sec
            // target is angular velocity (radians per sec) from linear velocity of surface speed
            if (Mathf.Abs(this.body.linearVelocity.x) > Utils.NEAR_ZERO_LOOSE)
            {
                this.procAnimatorBody.shoulderControllers[0].Animate();
            }
            this.procAnimatorBody.hipContollers[0].Animate();
        }
    }

    private void AddLimbFromPrefab(AutoLimbAttachment controller, int limb_index, GameObject prefab)
    {
        GameObject spawn = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        this.AddLimb(controller, limb_index, spawn);
    }

    private void AddLimb(AutoLimbAttachment controller, int limb_index, GameObject existing, bool cleanup=true)
    {
        int i;

        GameObject[] children = new GameObject[existing.transform.childCount];
        for (i = 0; i < existing.transform.childCount; i++)
        {
            children[i] = existing.transform.GetChild(i).gameObject;
        }
        existing.transform.position = Vector3.zero;
        existing.transform.DetachChildren();
        if (cleanup) Destroy(existing);

        // TODO: seams reasonable for attaching hand/foot, might need to revist to make explicit hands; maybe by instance name?
        bool add_endpoint = children.Length > 1;
        Vector3 position = Vector3.zero;
        Vector3 scale;
        for (i = 0; i < children.Length; i++)
        {
            scale = children[i].transform.localScale;
            // Endpoint (Hand/Foot)
            if (add_endpoint && i == children.Length - 1)
            {
                children[i].transform.parent = controller.EndpointController.Terminals[limb_index].gameObject.transform;
            }
            // Segment
            else
            {
                children[i].transform.parent = controller.gameObject.transform;
                controller.limbsAndSegments[limb_index].segments.Add(children[i]);
            }
            position.z = children[i].transform.position.z;
            children[i].transform.localPosition = position;
            children[i].transform.localScale = scale;
            children[i].transform.rotation = Quaternion.identity;
        }
        controller.EndpointController.Terminals[limb_index].enabled = true;
        controller.NudgeAfterChange();
    }

    private void DropLimb(AutoLimbAttachment controller, int limb_index)
    {
        GameObject prefab = controller.limbsAndSegments[limb_index].prefab;

        // Remove from body
        controller.EndpointController.Terminals[limb_index].enabled = false;
        if (controller.EndpointController.Terminals[limb_index].gameObject.transform.childCount > 0)
        {
            Destroy(controller.EndpointController.Terminals[limb_index].gameObject.transform.GetChild(0).gameObject);
        }
        foreach (GameObject segment in controller.limbsAndSegments[limb_index].segments) Destroy(segment);
        controller.limbsAndSegments[limb_index].segments.Clear();

        // Drop new pickup
        GameObject pickup = Instantiate(this.pickupPrefab);
        Pickup pickupCtrl = pickup.GetComponent<Pickup>();
        GameObject attachment = Instantiate(prefab, pickup.transform);
        pickupCtrl.item = attachment;
        pickupCtrl.isAttachment = true;
        pickup.transform.position = this.transform.position;
        pickup.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(
            Random.Range(-2f, 2f),
            Random.Range(1f, 3f)
        );
    }
}
