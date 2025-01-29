using UnityEngine;

public class GroundMonitor : MonoBehaviour
{
    // TODO: update with generic controller when added
    public PlayerController controllerToInform;
    private int contacts = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Utils.GROUND_TAG))
        {
            this.contacts += 1;
            this.controllerToInform.onGround = true;
            //this.controllerToInform.extendJump = 0;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(Utils.GROUND_TAG))
        {
            this.contacts -= 1;
            if (this.contacts == 0) this.controllerToInform.onGround = false;
        }
    }
}
