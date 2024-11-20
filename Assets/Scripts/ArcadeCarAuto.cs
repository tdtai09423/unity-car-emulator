using System.Collections;
using UnityEngine;

public class ArcadeCarAuto: MonoBehaviour {
    private Rigidbody rb;
    public float accelerationForce = 10f;
    public float steerAngle = 30f;
    public float reverseForce = -5f; 

    private float currentSteerAngle = 0f;

    void Start() {
        rb = GetComponent<Rigidbody>();

        StartCoroutine(AutoDriveSequence());
    }

    IEnumerator AutoDriveSequence() {
        MoveForward(2f);

        yield return new WaitForSeconds(2f);

        TurnRight(1f);

        yield return new WaitForSeconds(1f);

        Reverse(1f);

        yield return new WaitForSeconds(1f);

        StopCar();
    }

    private void MoveForward(float duration) {
        Debug.Log("Moving forward...");
        rb.velocity = transform.forward * accelerationForce;
        Invoke(nameof(StopCar), duration);
    }

    private void TurnRight(float duration) {
        Debug.Log("Turning right...");
        currentSteerAngle = steerAngle;
        rb.velocity = transform.forward * (accelerationForce / 2);
        Invoke(nameof(StopTurning), duration);
    }

    private void Reverse(float duration) {
        Debug.Log("Reversing...");
        rb.velocity = transform.forward * reverseForce;
        Invoke(nameof(StopCar), duration);
    }

    private void StopCar() {
        Debug.Log("Stopping car...");
        rb.velocity = Vector3.zero;
    }

    private void StopTurning() {
        Debug.Log("Stopping turning...");
        currentSteerAngle = 0f;
    }

    void FixedUpdate() {
        if (currentSteerAngle != 0f) {
            Vector3 direction = Quaternion.Euler(0f, currentSteerAngle, 0f) * transform.forward;
            rb.velocity = direction * rb.velocity.magnitude;
        }
    }
}
