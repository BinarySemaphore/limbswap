using UnityEngine;

public class AutoLimbFeet : MonoBehaviour
{
    private GameObject[] feet;

    private void Start()
    {
        this.feet = new GameObject[this.transform.childCount];
        for (int i = 0; i < this.feet.Length; i++)
        {
            this.feet[i] = this.transform.GetChild(i).gameObject;
        }
    }

    public GameObject[] Feet
    {
        get { return this.feet; }
    }

    public float LowPoint
    {
        get { return this.transform.position.y; }
    }

    public override string ToString()
    {
        string output = $"{this.name} (AutoLimbFeet | feet=[";
        for (int i = 0; i < this.feet.Length; i++)
        {
            output += this.feet[i].ToString();
            if (i < this.feet.Length - 1) output += ", ";
        }
        output += "])";
        return output;
    }
}
