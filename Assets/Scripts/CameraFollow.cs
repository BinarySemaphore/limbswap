using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraFollow : MonoBehaviour
{
    private float x_min, x_max;
    private float y_min, y_max;
    private Camera cam;

    public TextMeshProUGUI textFPS;

    [SerializeField]
    private GameObject target;
    [SerializeField]
    private float stiffness = 5f;
    [SerializeField]
    [Tooltip("Horizontal limit to edge before camera moves on x axis to keep target in frame.")]
    private float x_limit = 0.1f;
    [SerializeField]
    [Tooltip("Vertical limit to edge before camera moves on y axis to keep target in frame.")]
    private float y_limit = 0.1f;

    void Start()
    {
        this.x_min = 0f + this.x_limit;
        this.x_max = 1f - this.x_limit;
        this.y_min = 0f + this.y_limit;
        this.y_max = 1f - this.y_limit;
        this.cam = this.GetComponent<Camera>();
    }

    private void Update()
    {
        if (this.textFPS != null)
        {
            float fps = 1f / Time.smoothDeltaTime;
            this.textFPS.text = $"FPS: {fps:f1}";
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (this.target == null) return;

        Vector3 delta_position = Vector3.zero;
        Vector3 target_location = this.cam.WorldToViewportPoint(this.target.transform.position);

        if (target_location.x < this.x_min) delta_position.x -= (this.x_min - target_location.x) * stiffness;
        else if (target_location.x > this.x_max) delta_position.x += (target_location.x - this.x_max) * stiffness;

        if (target_location.y < this.y_min) delta_position.y -= (this.y_min - target_location.y) * stiffness;
        else if (target_location.y > this.y_max) delta_position.y += (target_location.y - this.y_max) * stiffness;

        this.transform.position += delta_position;
    }
}
