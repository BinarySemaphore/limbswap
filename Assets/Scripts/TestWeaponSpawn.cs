using UnityEngine;

public class TestWeaponSpawn : MonoBehaviour
{
    public GameObject[] weapons;
    public GameObject pickupPrefab;
    public float despenseFrequency = 1f;
    public int limit = 10;
    public Vector2 maxVelocity = Vector2.up;

    private float timer = 0f;

    private void Update()
    {
        this.timer += Time.deltaTime;
        if (this.limit > 0 && this.timer > this.despenseFrequency)
        {
            this.limit -= 1;
            this.timer = 0f;
            this.Dispense();
        }
    }

    private void Dispense()
    {
        Vector2 initialVelocity = new Vector2(
            Random.Range(-this.maxVelocity.x, this.maxVelocity.x),
            Random.Range(this.maxVelocity.y * 0.25f, this.maxVelocity.y)
        );

        GameObject weaponPrefab = this.weapons[Random.Range(0, this.weapons.Length)];
        GameObject pickup = Instantiate(this.pickupPrefab);
        Pickup pickupCtrl = pickup.GetComponent<Pickup>();
        GameObject weapon = Instantiate(weaponPrefab, pickup.transform);
        pickupCtrl.item = weapon;
        pickupCtrl.isAttachment = true;


        pickup.transform.position = this.transform.position + Vector3.up * this.transform.localScale.y;
        pickup.GetComponent<Rigidbody2D>().linearVelocity = initialVelocity;
    }
}
