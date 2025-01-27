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
    private GameObject pickupPrefab;
    [SerializeField]
    private GameObject[] attchWpnPrefabArms;
    [SerializeField]
    private GameObject[] attchWpnPrefabLegs;

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

    private void FixedUpdate()
    {
        if (this.load_appendages && this.attchWpnPrefabArms.Length > 0)
        {
            this.load_appendages = false;
            for (int i = 0; i < this.attchWpnPrefabArms.Length; i++) this.AddArmFromPrefab(i, this.attchWpnPrefabArms[i]);
        }

        // TODO: move all input to Update()
        Vector2 move_delta = this.GetMoveDeltaFromInput();
        this.UpdateMovement(move_delta);

        if (Input.GetKeyUp(KeyCode.Comma))
        {
            int arm_index = 0;//Random.Range(0, this.attchWpnPrefabArms.Length);
            AutoLimbShoulder shoulder = this.procAnimatorBody.shoulderControllers[0];

            if (!shoulder.EndpointController.Terminals[arm_index].enabled)
            {
                arm_index = (arm_index + 1) % shoulder.EndpointController.Terminals.Length;
                if (!shoulder.EndpointController.Terminals[arm_index].enabled) return;
            }
            this.DropArm(arm_index);
            this.attchWpnPrefabArms[arm_index] = null;
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
                    int arm_index = -1;
                    for (i = 0; i < this.procAnimatorBody.shoulderControllers[0].EndpointController.Terminals.Length; i++)
                    {
                        if (!this.procAnimatorBody.shoulderControllers[0].EndpointController.Terminals[i].enabled)
                        {
                            arm_index = i;
                            break;
                        }
                    }
                    if (arm_index >= 0)
                    {
                        for (i = 0; i < this.AttachmentPrefabList.Length; i++)
                        {
                            if (pickup.item.name.StartsWith(this.AttachmentPrefabList[i].name))
                            {
                                this.attchWpnPrefabArms[arm_index] = this.AttachmentPrefabList[i];
                                break;
                            }
                        }
                        this.AddArm(arm_index, pickup.item);
                        Destroy(pickup.gameObject);
                    }
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

    private void AddArmFromPrefab(int arm_index, GameObject prefab)
    {
        GameObject spawn = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        this.AddArm(arm_index, spawn);
    }
    private void AddArm(int arm_index, GameObject existing, bool cleanup=true)
    {
        int i = 0;
        AutoLimbShoulder shoulder = this.procAnimatorBody.shoulderControllers[0];

        GameObject[] children = new GameObject[existing.transform.childCount];
        for (i = 0; i < existing.transform.childCount; i++)
        {
            children[i] = existing.transform.GetChild(i).gameObject;
        }
        existing.transform.DetachChildren();
        if (cleanup) Destroy(existing);

        // TODO: seams reasonable for attaching hand, might need to revist to make explicit hands; maybe by instance name?
        bool add_hand = children.Length > 1;
        Vector3 position = Vector3.zero;
        for (i = 0; i < children.Length; i++)
        {
            // Hand
            if (add_hand && i == children.Length - 1)
            {
                children[i].transform.parent = shoulder.EndpointController.Terminals[arm_index].gameObject.transform;
            }
            // Segment
            else
            {
                children[i].transform.parent = shoulder.gameObject.transform;
                shoulder.limbsAndSegments[arm_index].segments.Add(children[i]);
            }
            position.z = children[i].transform.position.z;
            children[i].transform.localPosition = position;
        }
        shoulder.EndpointController.Terminals[arm_index].enabled = true;
    }

    private void DropArm(int arm_index)
    {
        AutoLimbShoulder shoulder = this.procAnimatorBody.shoulderControllers[0];

        GameObject weaponPrefab = this.attchWpnPrefabArms[arm_index];

        // Remove
        shoulder.EndpointController.Terminals[arm_index].enabled = false;
        Destroy(shoulder.EndpointController.Terminals[arm_index].gameObject.transform.GetChild(0).gameObject);
        foreach (GameObject segment in shoulder.limbsAndSegments[arm_index].segments) Destroy(segment);
        shoulder.limbsAndSegments[arm_index].segments.Clear();

        // Drop
        GameObject pickup = Instantiate(this.pickupPrefab);
        Pickup pickupCtrl = pickup.GetComponent<Pickup>();
        GameObject attachment = Instantiate(weaponPrefab, pickup.transform);
        pickupCtrl.item = attachment;
        pickupCtrl.isAttachment = true;

        Vector2 initialVelocity = new Vector2(
            Random.Range(-2f, 2f),
            Random.Range(1f, 3f)
        );

        pickup.transform.position = this.transform.position;
        pickup.GetComponent<Rigidbody2D>().linearVelocity = initialVelocity;
    }
}
