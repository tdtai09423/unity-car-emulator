using System;
using UnityEngine;
using System.Collections;
using System.Runtime.ConstrainedExecution;
using System.Collections.Generic;
using Assets.Script;
using UnityEngine.UIElements;
using System.Runtime.InteropServices;


public class ArcadeCar: MonoBehaviour {

    const int WHEEL_LEFT_INDEX = 0;
    const int WHEEL_RIGHT_INDEX = 1;

    int count = 0;

    const float wheelWidth = 0.085f;

    private bool isAutoDriving = false;
    private bool isFrozen = true;

    private Queue<Car> movementQueue = new Queue<Car>();
    private bool isExecuting = false;
    // Start is called before the first frame update

    private float autoDriveStartTime;
    private float autoDriveDuration = 30f;

    private float leftSpeed;
    private float rightSpeed;

    public Transform sensorLeft;
    public Transform sensorCenter;
    public Transform sensorRight;

    [DllImport("__Internal")]
    private static extern void NotifyLineSensor(string dataSensor);

    public enum ControlMode {
        Keyboard,
        Buttons
    };

    public enum Axel {
        Front,
        Rear
    }

    [Serializable]
    public struct Wheel {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public GameObject wheelEffectObj;
        public ParticleSystem smokeParticle;
        public Axel axel;
    }

    public ControlMode control;

    public float maxAcceleration = 80.0f;

    private bool IsTrackingLine = false;

    public Vector3 _centerOfMass;

    public List<Wheel> wheels;

    float moveInput;
    float steerInput;

    private Rigidbody carRb;


    void Start() {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;
        WebGLInput.captureAllKeyboardInput = false;

    }

    void Update() {
    }

    void FixedUpdate() {
        CheckSensors();
        SetWheelSpeeds();
    }

    public void SetWheelSpeeds() {
        wheels[WHEEL_LEFT_INDEX].wheelCollider.motorTorque = leftSpeed;

        wheels[WHEEL_RIGHT_INDEX].wheelCollider.motorTorque = rightSpeed;
    }

    public void SetWheelsSpeed(float left, float right) {
        leftSpeed = left;

        rightSpeed = right;
    }

    public void MoveCar(string dataFrontEnd) {
        Debug.Log("Received command: " + dataFrontEnd); // Xác nhận lệnh từ WebGL

        string[] parameters = dataFrontEnd.Split(',');
        if (parameters.Length == 3) {
            float leftSpeed = float.Parse(parameters[0]);
            float rightSpeed = float.Parse(parameters[1]);
            float duration = float.Parse(parameters[2]);
            if (duration == 0) {
                SetWheelsSpeed(leftSpeed, rightSpeed);
            } else {
                AddQueue(leftSpeed, rightSpeed, duration);
            }
        } else {
            Debug.LogWarning("Invalid command format");
        }

    }
    public void AddQueue(float leftSpeed, float rightSpeed, float duration) {
        movementQueue.Enqueue(new Car(leftSpeed, rightSpeed, duration));
        StartExecution();
    }

    private void AddQueueLineTracking(float leftSpeed, float rightSpeed, float duration) {
        movementQueue.Enqueue(new Car(leftSpeed, rightSpeed, 0f));
        ExecuteQueueLineStacking();
    }

    private void ExecuteQueueLineStacking() {
        while (movementQueue.Count > 0) {
            Car command = movementQueue.Dequeue();
            SetWheelsSpeed(command.LeftWheel, command.RightWheel);
        }
    }

    public void StartExecution() {
        if (!isExecuting && movementQueue.Count > 0) {
            StartCoroutine(ExecuteMovementQueue());
        }
    }

    private IEnumerator ExecuteMovementQueue() {
        isExecuting = true;

        while (movementQueue.Count > 0) {
            Car command = movementQueue.Dequeue();
            SetWheelsSpeed(command.LeftWheel, command.RightWheel);
            yield return new WaitForSeconds(command.Duration);
        }
        isExecuting = false;
    }

    void WheelEffects() {
        foreach (var wheel in wheels) {
            //var dirtParticleMainSettings = wheel.smokeParticle.main;

            if (Input.GetKey(KeyCode.Space) && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && carRb.velocity.magnitude >= 10.0f) {
                wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = true;
                wheel.smokeParticle.Emit(1);
            } else {
                wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = false;
            }
        }
    }

    bool CheckColorUnderSensor(Transform sensor, Color targetColor) {
        Ray ray = new Ray(sensor.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1f)) {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.material.mainTexture is Texture2D texture) {
                Vector2 uv = hit.textureCoord;
                uv.x *= texture.width;
                uv.y *= texture.height;

                Color pixelColor = texture.GetPixel((int) uv.x, (int) uv.y);

                if (Mathf.Abs(pixelColor.r - targetColor.r) < 0.1f &&
                    Mathf.Abs(pixelColor.g - targetColor.g) < 0.1f &&
                    Mathf.Abs(pixelColor.b - targetColor.b) < 0.1f) {
                    return true;
                }
            }
        }
        return false;
    }

    private void CheckSensors() {
        Color targetColor = Color.black;
        string dataLeft = "0", dataMid = "0", dataRight = "0";

        if (CheckColorUnderSensor(sensorLeft, targetColor)) {
            dataLeft = "1";
        }

        if (CheckColorUnderSensor(sensorCenter, targetColor)) {
            dataMid = "1";
        }

        if (CheckColorUnderSensor(sensorRight, targetColor)) {
            dataRight = "1";
        }

        string data = $"{dataLeft}{dataMid}{dataRight}";

        Debug.Log("Data: " + data);

        InternalNotifyLineSensor(data);

    }

    private void InternalNotifyLineSensor(string dataSensor) {
#if UNITY_WEBGL && !UNITY_EDITOR
        NotifyLineSensor(dataSensor);
#endif
    }

}

