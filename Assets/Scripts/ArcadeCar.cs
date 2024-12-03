using System;
using UnityEngine;
using System.Collections;
using System.Runtime.ConstrainedExecution;
using System.Collections.Generic;
using Assets.Script;
using UnityEngine.UIElements;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;


public class ArcadeCar: MonoBehaviour {

    const int WHEEL_LEFT_INDEX = 0;
    const int WHEEL_RIGHT_INDEX = 1;
    const int WHEEL_FRONT_INDEX = 2;
    int count = 0;

    /*public WheelCollider RightWheel;
    public WheelCollider LeftWheel;*/

    const float wheelWidth = 0.085f;

    private bool isAutoDriving = false;
    private bool isFrozen = true;

    private Queue<Car> movementQueue = new Queue<Car>();
    private bool isExecuting = false;
    private bool IsTrackingLine = false;
    // Start is called before the first frame update

    private float autoDriveStartTime;
    private float autoDriveDuration = 30f;

    private float maxSpeed = 3f;

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


    public Vector3 _centerOfMass;

    public List<Wheel> wheels;

    float moveInput;
    float steerInput;

    private Rigidbody carRb;

    public float P = -300f;
    public float I = 0f;
    public float D = 0f;
    public float BasicSpeed = -300f;
    public float MinSpeed = 100f;
    public float MaxSpeed = 1000f;

    private string data = "";
    private float previousError = 0f;
    private float integral = 0f;
   
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start() {

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;
        /*wheels[WHEEL_FRONT_INDEX].wheelCollider.motorTorque = 0f;
        wheels[WHEEL_FRONT_INDEX].wheelCollider.brakeTorque = 0f;*/
       /* movementQueue.Enqueue(new Car(500f, 500f, 3f));
        movementQueue.Enqueue(new Car(0, 0, 3f));
        StartExecution();*/
        WebGLInput.captureAllKeyboardInput = false;
       

    }

    void Update() {
    }

    void FixedUpdate() {
        CheckSensors();
        ProcessLineData(data, BasicSpeed);

        float currentSpeed = carRb.velocity.magnitude;

        if (currentSpeed > maxSpeed)
        {
            carRb.velocity = carRb.velocity.normalized * maxSpeed;
        }

        // In vận tốc ra console
        Debug.Log("Vận tốc hiện tại của xe: " + currentSpeed + " m/s");
    }

    public void SetWheelSpeeds(float leftSpeed, float rightSpeed) {
        // Set the speed for the left wheel (wheel left index)
        /*wheels[WHEEL_LEFT_INDEX].wheelCollider.motorTorque = leftSpeed;
        wheels[WHEEL_RIGHT_INDEX].wheelCollider.motorTorque = rightSpeed;*/
        /*wheels[WHEEL_LEFT_INDEX].wheelCollider.motorTorque = leftSpeed;
        wheels[WHEEL_RIGHT_INDEX].wheelCollider.motorTorque = rightSpeed;*/
        if(leftSpeed == 0 && rightSpeed == 0)
        {
            wheels[WHEEL_LEFT_INDEX].wheelCollider.brakeTorque = 1000f;
            wheels[WHEEL_RIGHT_INDEX].wheelCollider.brakeTorque = 1000f;
        }else
        {
            wheels[WHEEL_LEFT_INDEX].wheelCollider.motorTorque = leftSpeed;
            wheels[WHEEL_RIGHT_INDEX].wheelCollider.motorTorque = rightSpeed;
            wheels[WHEEL_LEFT_INDEX].wheelCollider.brakeTorque = 0f;
            wheels[WHEEL_RIGHT_INDEX].wheelCollider.brakeTorque = 0f;
        }
    }

    public void ResetCar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        movementQueue.Clear();
        SetWheelSpeeds(0,0);
    }

    /* void WheelEffects() {
         foreach (var wheel in wheels) {
             //var dirtParticleMainSettings = wheel.smokeParticle.main;

             if (Input.GetKey(KeyCode.Space) && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && carRb.velocity.magnitude >= 10.0f) {
                 wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = true;
                 wheel.smokeParticle.Emit(1);
             } else {
                 wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = false;
             }
         }
     }*/

    public void MoveCar(string dataFrontEnd)
    {
        Debug.Log("Received command: " + dataFrontEnd); // Xác nhận lệnh từ WebGL

        string[] parameters = dataFrontEnd.Split(',');
        if (parameters.Length == 3)
        {
            float rightSpeed = float.Parse(parameters[0]);
            float leftSpeed = float.Parse(parameters[1]);
            float duration = float.Parse(parameters[2]);
            if (IsTrackingLine)
            {
                AddQueueLineTracking(rightSpeed, leftSpeed, duration);
            }
            else
            {
                AddQueue(rightSpeed, leftSpeed, duration);
            }
        }
        else
        {
            Debug.LogWarning("Invalid command format");
        }

    }
    public void AddQueue(float rightSpeed, float leftSpeed, float duration)
    {
        movementQueue.Enqueue(new Car(rightSpeed, leftSpeed, duration));
        StartExecution();
    }

    private void AddQueueLineTracking(float rightSpeed, float leftSpeed, float duration)
    {
        movementQueue.Enqueue(new Car(rightSpeed, leftSpeed, 0f));
        ExecuteQueueLineStacking();
    }

    private void ExecuteQueueLineStacking()
    {
        while (movementQueue.Count > 0)
        {
            Car command = movementQueue.Dequeue();
            SetWheelSpeeds(command.RightWheel, command.LeftWheel);
        }
    }

    public void StartExecution()
    {
        if (!isExecuting && movementQueue.Count > 0)
        {
            StartCoroutine(ExecuteMovementQueue());
        }
    }

    private IEnumerator ExecuteMovementQueue()
    {
        isExecuting = true;

        while (movementQueue.Count > 0)
        {
            Car command = movementQueue.Dequeue();
            SetWheelSpeeds(command.RightWheel, command.LeftWheel);
            yield return new WaitForSeconds(command.Duration);
        }
        isExecuting = false;
    }

    private void ChangeLineTrackingState(string dataFrontEnd)
    {

        if (dataFrontEnd.Equals("1"))
        {
            IsTrackingLine = true;
        }
        else
        {
            IsTrackingLine = false;
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

        data = $"{dataLeft}{dataMid}{dataRight}";
        //Debug.Log(data);
        //ProcessLineData(data, BasicSpeed);
        //Debug.Log("Data: " + data);

        InternalNotifyLineSensor(data);

    }

    private void InternalNotifyLineSensor(string dataSensor) {
#if UNITY_WEBGL && !UNITY_EDITOR
        NotifyLineSensor(dataSensor);
#endif
    }

    private void ClearSpeed(float barkTorque)
    {
        wheels[WHEEL_LEFT_INDEX].wheelCollider.brakeTorque = barkTorque;
        wheels[WHEEL_RIGHT_INDEX].wheelCollider.brakeTorque = barkTorque;

    }

    public void ProcessLineData(string dataSensor, float baseSpeed)
    {
        float value = 0f;

        // Xử lý giá trị từ cảm biến
        if (dataSensor == "010")
            value = 0f;
        else if (dataSensor == "110")
            value = -1f;
        else if (dataSensor == "100")
            value = -2f;
        else if (dataSensor == "011")
            value = 1f;
        else if (dataSensor == "001")
            value = 2f;

        // Cập nhật tích phân và đạo hàm
        integral += value;

        float derivative = value - previousError;
        integral = Math.Clamp(integral, -15, 15);
        // Tính giá trị PID
        float pidValue = P * value + I * integral + D * derivative;
        pidValue = Math.Clamp(pidValue, -100, 100);

        // Điều khiển tốc độ động cơ
        float leftMotorSpeed = baseSpeed + pidValue;
        float rightMotorSpeed = baseSpeed - pidValue;
        leftMotorSpeed = Math.Clamp(leftMotorSpeed, MinSpeed, MaxSpeed);
        rightMotorSpeed = Math.Clamp(rightMotorSpeed, MinSpeed, MaxSpeed);
        /*Debug.Log("integral: " + integral);
        Debug.Log("pidValue: " + pidValue);
        Debug.Log("leftMotorSpeed: " + leftMotorSpeed + ",rightMotorSpeed: " + rightMotorSpeed);*/
        // Gửi lệnh điều khiển xe
        SetWheelSpeeds(leftMotorSpeed, rightMotorSpeed);

        // Lưu giá trị lỗi trước đó
        previousError = value;
    }

}

