using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerPickupSelector : MonoBehaviour
{
    [SerializeField]
    private Pickup currrentSelection;
    private PlayerController player;
    private List<Pickup> pickupsInSelection = new List<Pickup>(32);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pickup pickup = collision.GetComponent<Pickup>();
        if (pickup == null) return;

        //if (this.pickupsInSelection.Count == 0) pickup.MakeSelected();
        if (this.pickupsInSelection.Contains(pickup)) return;

        this.pickupsInSelection.Add(pickup);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Pickup pickup = collision.GetComponent<Pickup>();
        if (pickup == null) return;
        if (!this.pickupsInSelection.Contains(pickup)) return;

        //if (pickup == this.pickupsInSelection[0])
        //{
        //    pickup.MakeNotSelected();
        //    this.pickupsInSelection.Remove(pickup);
        //    if (this.pickupsInSelection.Count > 0) this.pickupsInSelection[0].MakeSelected();
        //}
        //else
        this.pickupsInSelection.Remove(pickup);
    }

    private void FixedUpdate()
    {
        if (this.pickupsInSelection.Count == 0)
        {
            if (this.currrentSelection != null) this.currrentSelection.MakeNotSelected();
            this.currrentSelection = null;
            return;
        }
        float distance;
        float closest_distance = float.MaxValue;
        Pickup closest_pickup = null;
        foreach (Pickup pickup in this.pickupsInSelection)
        {
            distance = Mathf.Abs(this.transform.position.x - pickup.transform.position.x);
            if (closest_pickup == null || distance < closest_distance)
            {
                closest_pickup = pickup;
                closest_distance = distance;
            }
        }
        if (closest_pickup == this.currrentSelection) return;
        if (this.currrentSelection != null) this.currrentSelection.MakeNotSelected();
        this.currrentSelection = closest_pickup;
        this.currrentSelection.MakeSelected();
    }

    public Pickup PickupSelection()
    {
        if (this.currrentSelection == null) return null;
        Pickup pickup = this.currrentSelection;
        this.currrentSelection.MakeNotSelected();
        this.currrentSelection = null;
        return pickup;
    }

    private void UpdateSelection(Pickup item)
    {
        //if (item != null) this.pickupsInSelection.Push(item);

        //if (this.pickupsInSelection != null) this.pickupsInSelection.MakeNotSelected();
        //this.pickupsInSelection = item;

        //if (item == null) return;

        //this.pickupsInSelection.MakeSelected();
    }
}
