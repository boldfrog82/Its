using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficVehiclePhysics : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float maxMotorTorque = 1500f;
    [SerializeField] private float maxBrakeTorque = 4000f;
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float downforce = 50f;

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider frontLeftCollider;
    [SerializeField] private WheelCollider frontRightCollider;
    [SerializeField] private WheelCollider rearLeftCollider;
    [SerializeField] private WheelCollider rearRightCollider;

    [Header("Wheel Visuals")]
    [SerializeField] private Transform frontLeftWheel;
    [SerializeField] private Transform frontRightWheel;
    [SerializeField] private Transform rearLeftWheel;
    [SerializeField] private Transform rearRightWheel;

    private Rigidbody rb;
    private float throttleInput;
    private float brakeInput;
    private float steerInput;

    public float SpeedMps => rb != null ? rb.velocity.magnitude : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += Vector3.down * 0.3f;
    }

    public void SetInputs(float throttle, float brake, float steer)
    {
        throttleInput = Mathf.Clamp01(throttle);
        brakeInput = Mathf.Clamp01(brake);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
    }

    private void FixedUpdate()
    {
        ApplyWheelColliderForces();
        UpdateWheelVisuals();

        if (rb != null)
        {
            rb.AddForce(-transform.up * downforce * rb.velocity.magnitude, ForceMode.Force);
        }
    }

    private void ApplyWheelColliderForces()
    {
        if (frontLeftCollider == null || frontRightCollider == null || rearLeftCollider == null || rearRightCollider == null)
        {
            return;
        }

        float steer = steerInput * maxSteerAngle;
        frontLeftCollider.steerAngle = steer;
        frontRightCollider.steerAngle = steer;

        float motorTorque = throttleInput * maxMotorTorque;
        rearLeftCollider.motorTorque = motorTorque;
        rearRightCollider.motorTorque = motorTorque;

        float brakeTorque = brakeInput * maxBrakeTorque;
        frontLeftCollider.brakeTorque = brakeTorque;
        frontRightCollider.brakeTorque = brakeTorque;
        rearLeftCollider.brakeTorque = brakeTorque;
        rearRightCollider.brakeTorque = brakeTorque;
    }

    private void UpdateWheelVisuals()
    {
        SyncWheel(frontLeftCollider, frontLeftWheel);
        SyncWheel(frontRightCollider, frontRightWheel);
        SyncWheel(rearLeftCollider, rearLeftWheel);
        SyncWheel(rearRightCollider, rearRightWheel);
    }

    private static void SyncWheel(WheelCollider collider, Transform wheel)
    {
        if (collider == null || wheel == null)
        {
            return;
        }

        collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        wheel.position = position;
        wheel.rotation = rotation;
    }
}
