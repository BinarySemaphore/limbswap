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
    private int extend_jump;
    private Rigidbody2D body;

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

    private void Update()
    {
        Vector2 move_delta = this.GetMoveDeltaFromInput();
        this.UpdateMovement(move_delta);

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
                
                this.procAnimatorBody.Forward = Vector3.right;
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
                this.procAnimatorBody.Forward = Vector3.left;
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
