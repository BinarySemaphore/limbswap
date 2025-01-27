using UnityEngine;

public class Pickup : MonoBehaviour
{

    private bool sitting = false;
    private float time = 0f;
    private int sleepCountDown = 25;  // Number of fixed-frames at near-rest before sleeping physics
    private float timeOtherPickupInteractions = 2f;  // Time (seconds) allowed to physics interact with other pickups
    private Vector3 holdPosition = Vector3.zero;
    private Rigidbody2D body;
    private Color originalColor;

    public bool isAttachment = false;
    public GameObject item;

    [SerializeField]
    private float speed = 1f;
    [SerializeField]
    private GameObject background;
    [SerializeField]
    private Color selectedColor = Color.green;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.originalColor = this.background.GetComponent<SpriteRenderer>().color;
        this.body = GetComponent<Rigidbody2D>();
        this.body.simulated = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // TODO: refactor to get rid of list, just keep selected and use enter/exit to update selection based on nearest (jeez)
        if (this.sitting)
        {
            float offset = Utils.LerpBounceBack(0f, 10f, this.time);
            this.transform.position = new Vector3(
                this.holdPosition.x,
                this.holdPosition.y + Utils.LerpBounceBack(0f, 0.25f, this.time),
                this.holdPosition.z
            );

            if (this.background) this.background.transform.Rotate(Vector3.up, Utils.FULL_TURN * Time.deltaTime * this.speed * 10.0f);

            this.time += Time.deltaTime * this.speed;
            if (this.time > 1.0f) this.time = this.time % 1f;
        }
        else if (this.body.linearVelocity.magnitude < Utils.NEAR_ZERO_LOOSE)
        {
            if (this.sleepCountDown <= 0)
            {
                //this.body.simulated = false; <- Don't do this, it disables the collider for triggers later yikes
                this.body.bodyType = RigidbodyType2D.Static;
                this.holdPosition = transform.position;
                this.sitting = true;
            }
            else this.sleepCountDown -= 1;
        }
        
        if (!this.sitting)
        {
            if (this.timeOtherPickupInteractions > 0f) this.timeOtherPickupInteractions -= Time.deltaTime;
            if (this.timeOtherPickupInteractions <= 0f)
            {
                this.body.excludeLayers |= LayerMask.GetMask("Pickup");
            }
        }
    }

    public void MakeSelected()
    {
        if (this.holdPosition != Vector3.zero) this.holdPosition.z = -0.2f;
        else this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -0.2f);
        this.background.GetComponent<SpriteRenderer>().color = this.selectedColor;
    }

    public void MakeNotSelected()
    {
        if (this.holdPosition != Vector3.zero) this.holdPosition.z = 0f;
        else this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 0f);
        this.background.GetComponent<SpriteRenderer>().color = this.originalColor;
    }
}
