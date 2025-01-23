using UnityEngine;

public static class Utils
{
    public const float FULL_TURN = 2f * Mathf.PI;
    public const float THREE_QUARTER_TURN = 1.5f * Mathf.PI;
    public const float HALF_TURN = Mathf.PI;
    public const float QUARTER_TURN = 0.5f * Mathf.PI;

    public static float Mod(float number, float divisor)
    {
        float value = number % divisor;
        if (value < 0f) value += divisor;
        return value;
    }

    public static float ShortestAngle(float start, float target)
    {
        float delta_angle = target - start;
        if (Mathf.Abs(delta_angle) > Mathf.PI) delta_angle += 2f * Mathf.PI;
        return delta_angle;
    }

    public static void ApplySpringResolveSingle(float target_length, float spring_coef, Vector3 attachment_point, GameObject obj_a)
    {
        Vector3 to_attch_point_diff = attachment_point - obj_a.transform.position;
        if (to_attch_point_diff.magnitude > target_length)
        {
            to_attch_point_diff = to_attch_point_diff.normalized * spring_coef * (to_attch_point_diff.magnitude - target_length);
            obj_a.transform.position += to_attch_point_diff;
        }
    }

    public static void ApplySpringResolveDual(float target_length, float spring_coef, GameObject obj_a, GameObject obj_b)
    {
        Vector3 a_to_b_diff = obj_b.transform.position - obj_a.transform.position;
        if (a_to_b_diff.magnitude > target_length)
        {
            a_to_b_diff = a_to_b_diff.normalized *  0.5f * spring_coef * (a_to_b_diff.magnitude - target_length);
            obj_a.transform.position += a_to_b_diff;
            obj_b.transform.position -= a_to_b_diff;
        }
    }

    public static Vector3 IkSolveTwoSeg(Vector3 start, Vector3 end, Vector3 forward, float seg_length)
    {
        float half_distance = (end - start).magnitude * 0.5f;
        float adj_over_hypo = half_distance / seg_length;
        Vector3 joint_point = (end - start).normalized;
        joint_point *= seg_length;

        // Skip trig if no angle
        if (adj_over_hypo >= 1f) return joint_point + start;

        float deflect_angle = Mathf.Rad2Deg * Mathf.Acos(adj_over_hypo);
        Vector3 rotation_axis = Vector3.Cross(joint_point, forward);
        Quaternion rotation = Quaternion.AngleAxis(deflect_angle, rotation_axis);
        joint_point = rotation * joint_point;

        return joint_point + start;
    }
}
