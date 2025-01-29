using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{
    private const string INPUT_JUMP = "Jump";
    private const string INPUT_HORIZONTAL = "Horizontal";

    private bool loadAppendages;
    private int extendJump;
    private Vector2 collisionAccumulatedNormal;

    [SerializeField]
    private float acceleration = 20f;
    [SerializeField]
    private float inAirAcceleration = 5f;
    [SerializeField]
    private float jumpAcceleration = 20f;
    [SerializeField]
    private int sliceJump = 5;
    [SerializeField]
    private float maxPropelHorizontalSpeed = 10f;
    [SerializeField]
    private float maxPropelVerticalSpeed = 10f;
    [SerializeField]
    private float maxHorizontalSpeed = 20f;
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

    public bool onGround;
    public bool stopping;
    public bool groundContact;
    public float calculatedPlayerHeight;
    public Vector2 groundContactPoint;
    public Vector2 groundContactNormal;
    public Vector2 velocity;


    // Start is called before the first frame update
    void Start()
    {
        this.loadAppendages = true;
        this.onGround = false;
        this.extendJump = 0;
        this.jumpAcceleration /= this.sliceJump;
        this.collisionAccumulatedNormal = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Vector2 col_point = collision.ClosestPoint(this.transform.position);
        Vector2 col_normal = new Vector2(this.transform.position.x - col_point.x, this.transform.position.y - col_point.y).normalized;
        this.collisionAccumulatedNormal += col_normal;
        //this.velocity -= Vector3.Project(this.velocity, col_normal);
        //this.collisionVelocity += Vector3.Project(this.velocity, col_normal);
        //RaycastHit2D test = new RaycastHit2D();
        //test.normal = col_normal;
        //test.point = col_point;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Vector2 col_point = collision.ClosestPoint(this.transform.position);
        Vector2 col_normal = new Vector2(this.transform.position.x - col_point.x, this.transform.position.y - col_point.y).normalized;
        this.collisionAccumulatedNormal += col_normal;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
    }

    private void Update()
    {
        this.HandleInput();
    }

    private void FixedUpdate()
    {
        this.calculatedPlayerHeight = Mathf.Abs(
            this.procAnimatorBody.hipContollers[0].EndpointController.transform.position.y -
            this.transform.position.y
        );

        RaycastHit2D contact = Physics2D.Raycast(
            this.transform.position,
            Vector2.down,
            this.calculatedPlayerHeight + 5f,
            LayerMask.GetMask("Surface")
        );

        if (contact)
        {
            this.groundContact = true;
            this.groundContactPoint = contact.point;
            this.groundContactNormal = contact.normal;
        }
        else
        {
            this.groundContact = false;
        }

        if (this.loadAppendages)
        {
            this.loadAppendages = false;
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
    }

    private void HandleInput()
    {
        float horizontal_input = Input.GetAxis(INPUT_HORIZONTAL);
        float jump_input = Input.GetAxis(INPUT_JUMP);
        float acceleration_timeslice = this.acceleration * Time.deltaTime;

        // Jump
        if (this.onGround && jump_input > 0f)
        {
            this.velocity.y = this.jumpAcceleration;
        }
        //if ((this.onGround || this.extendJump > 0) && Input.GetAxis(INPUT_JUMP) > 0)
        //{
        //    if (this.extendJump > 0) this.extendJump -= 1;
        //    if (this.onGround) this.extendJump = this.sliceJump;
        //    move_delta.y += this.jumpAcceleration;
        //}

        this.stopping = true;

        // Move right
        if (horizontal_input > 0f)
        {
            if (this.velocity.x + acceleration_timeslice < this.maxPropelHorizontalSpeed) this.velocity.x += acceleration_timeslice;
            this.procAnimatorBody.Forward = Vector3.right;
            this.stopping = false;
        }
        //if (Mathf.Abs(horizontal_intput) > 0.1f)
        //{
        //    if (this.onGround) move_delta.x += horizontal_intput * this.acceleration;
        //    else move_delta.x += horizontal_intput * this.inAirAcceleration;
        //}

        // Move left
        if (horizontal_input < 0f)
        {
            if (this.velocity.x - acceleration_timeslice > -this.maxPropelHorizontalSpeed) this.velocity.x -= acceleration_timeslice;
            this.procAnimatorBody.Forward = Vector3.left;
            this.stopping = false;
        }

        // Drop Limb (test only)
        // TODO: Remove-ish relocate to a taking-damage handler
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

        // Pick up limb
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

    private void UpdateMovement()
    {
        Vector3 vel_lookahead = this.velocity * Time.fixedDeltaTime;

        if (this.groundContact && this.transform.position.y - this.calculatedPlayerHeight + vel_lookahead.y <= this.groundContactPoint.y)
        {
            this.transform.position = new Vector3(
                this.transform.position.x,
                this.groundContactPoint.y + this.calculatedPlayerHeight,
                this.transform.position.z
            );
            this.velocity.y = 0f;
        }

        if (this.onGround)
        {
            // Friction
            if (this.stopping)
            {
                this.velocity.x -= 0.9f * this.velocity.x;
                if (Mathf.Abs(this.velocity.x) < Utils.NEAR_ZERO) this.velocity.x = 0f;
            }
        }
        else
        {
            this.velocity.y -= 10f * Time.fixedDeltaTime;
        }

        if (this.collisionAccumulatedNormal != Vector2.zero)
        {
            //this.velocity -= this.collisionVelocity * 1.1f;
            //Vector3.Project(this.velocity, col_normal);
            Vector2 test = this.velocity * 1.1f;
            test.x = Mathf.Abs(test.x) * this.collisionAccumulatedNormal.x;
            test.y = Mathf.Abs(test.y) * this.collisionAccumulatedNormal.y;
            this.velocity += test;
            this.collisionAccumulatedNormal = Vector2.zero;
        }

        // Limit velocity
        this.velocity.x = Mathf.Clamp(this.velocity.x, -this.maxHorizontalSpeed, this.maxHorizontalSpeed);
        this.velocity.y = Mathf.Clamp(this.velocity.y, -this.maxVerticalSpeed, this.maxVerticalSpeed);

        Vector3 new_position = this.transform.position;
        new_position.x += this.velocity.x * Time.fixedDeltaTime;
        new_position.y += this.velocity.y * Time.fixedDeltaTime;
        this.transform.position = new_position;
        // Update velocity (counteract horizontal retained velocity if on ground)
        //if (this.onGround) delta.x += -this.body.linearVelocity.x;
        //this.body.linearVelocity += new Vector2(delta.x, delta.y);

        //this.body.linearVelocity = new Vector2(
        //    Mathf.Clamp(this.body.linearVelocity.x, -this.maxPropelHorizontalSpeed, this.maxPropelHorizontalSpeed),
        //    Mathf.Clamp(this.body.linearVelocity.y, -this.maxPropelVerticalSpeed * 10f, this.maxPropelVerticalSpeed)
        //);

        if (this.onGround)
        {
            // TODO: if want feet to move automatically with ground this is where to do it now
            // How to: calc clock ratio as target over 1 revolution per sec
            // target is angular velocity (radians per sec) from linear velocity of surface speed
            //if (Mathf.Abs(this.body.linearVelocity.x) > Utils.NEAR_ZERO_LOOSE) this.procAnimatorBody.shoulderControllers[0].Animate();
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
