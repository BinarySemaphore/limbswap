using System;
using UnityEngine;

public class AutoLimbShoulders : MonoBehaviour
{
    private AutoLimb bodyController;

    public GameObject parent;

    [SerializeField]
    private AutoLimbHands handController;
    [SerializeField]
    private Limb[] armsAndSegments;

    private void Start()
    {
    }

    private void FixedUpdate()
    {
        
    }

}
