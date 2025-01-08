using UnityEngine;

namespace CarController
{
    /// <summary>
    /// Controls the vehicle's physics and behavior.
    /// It does not contain logic for input, UI, or sounds.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("CAR SETUP")]
        [Range(20, 190)] [SerializeField] private int maxSpeed = 90;
        [Range(10, 120)] [SerializeField] private int maxReverseSpeed = 45;
        [Range(1, 10)] [SerializeField] private int accelerationMultiplier = 2;
        [Range(10, 45)] [SerializeField] private int maxSteeringAngle = 27;
        [Range(0.1f, 1f)] [SerializeField] private float steeringSpeed = 0.5f;
        [Range(100, 600)] [SerializeField] private int brakeForce = 350;
        [Range(1, 10)] [SerializeField] private int decelerationMultiplier = 2;
        [Range(1, 10)] [SerializeField] private int handbrakeDriftMultiplier = 5;

        [Tooltip("Rigidbody center of mass.")]
        [SerializeField] private Vector3 bodyMassCenter;

        [Space(10)]
        [Header("DRIFT TWEAKS")]
        [Tooltip("Allow drifting when reversing? If false, reverse won't trigger drifting.")]
        [SerializeField] private bool allowReverseDrift;
        [Tooltip("Sideways velocity threshold for forward drifting. Lower = easier drift.")]
        [SerializeField] private float driftSidewaysThresholdForward = 1.5f;
        [Tooltip("Sideways velocity threshold for reverse drifting. Increase to make reverse drifting harder.")]
        [SerializeField] private float driftSidewaysThresholdReverse = 3f;
        [Tooltip("Minimum forward speed (km/h approx) required to drift going forward.")]
        [SerializeField] private float driftMinForwardSpeed = 5f;
        [Tooltip("Minimum reverse speed (km/h approx) required to drift going backward.")]
        [SerializeField] private float driftMinReverseSpeed = 5f;

        [Space(10)]
        [Header("WHEELS")]
        [SerializeField] private GameObject frontLeftMesh;
        [SerializeField] private WheelCollider frontLeftCollider;
        [SerializeField] private GameObject frontRightMesh;
        [SerializeField] private WheelCollider frontRightCollider;
        [SerializeField] private GameObject rearLeftMesh;
        [SerializeField] private WheelCollider rearLeftCollider;
        [SerializeField] private GameObject rearRightMesh;
        [SerializeField] private WheelCollider rearRightCollider;

        // Properties
        
        /// <summary>
        /// Current approximate speed of the car (km/h). It's based on the front-left wheel's RPM.
        /// </summary>
        public float CarSpeed { get; private set; }
        /// <summary>
        /// True when the car is drifting. It affects friction and tire marks.
        /// </summary>
        public bool IsDrifting { get; private set; }
        /// <summary>
        /// True when handbrake is locked. It also applies braking torque.
        /// </summary>
        public bool IsTractionLocked { get; private set; }
        /// <summary>
        /// Local velocity in Z axis (forward/backward). Positive = forward, negative = reverse.
        /// </summary>
        public float LocalVelocityZ { get; private set; }
        /// <summary>
        /// Local velocity in X axis (side to side). Positive = right, negative = left.
        /// </summary>
        public float LocalVelocityX { get; private set; }
        /// <summary>
        /// Magnitude of linear velocity. Useful for speed comparisons.
        /// </summary>
        public float LinearVelocityMagnitude => _rb.linearVelocity.magnitude;
        /// <summary>
        /// Reference to the car's Rigidbody component.
        /// </summary>
        public Rigidbody CarRigidbody => _rb;

        // Private variables.
        private Rigidbody _rb;
        private float _steeringAxis; // Range -1 to 1.
        private float _throttleAxis; // Range -1 to 1.
        private bool _deceleratingCar;
        private float _driftingAxis;

        // Friction settings (to restore after drifting).
        private WheelFrictionCurve _fLWheelFriction;
        private float _flWheelExtremumSlip;
        private WheelFrictionCurve _frWheelFriction;
        private float _frWheelExtremumSlip;
        private WheelFrictionCurve _rLWheelFriction;
        private float _rlWheelExtremumSlip;
        private WheelFrictionCurve _rrWheelFriction;
        private float _rrWheelExtremumSlip;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = bodyMassCenter;

            // Store the original friction values for drifting logic.
            StoreWheelFriction();
        }

        private void Update()
        {
            // Approximate speed based on front-left wheel's RPM and circumference.
            // This is roughly in km/h.
            CarSpeed = (2f * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60f) / 1000f;

            // Local velocities (X and Z).
            // localVelocityZ > 0 means moving forward, < 0 means reversing.
            LocalVelocityX = transform.InverseTransformDirection(_rb.linearVelocity).x;
            LocalVelocityZ = transform.InverseTransformDirection(_rb.linearVelocity).z;

            // Animate wheel meshes according to WheelColliders.
            AnimateWheelMeshes();
        }

        #region PUBLIC METHODS

        /// <summary>
        /// Resets the car's velocity and angular velocity to zero.
        /// </summary>
        public void ResetCar()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            
            // Reset steering and throttle axis.
            _steeringAxis = 0f;
            _throttleAxis = 0f;
            
            // Reset friction values.
            RestoreWheelFriction();
            
            // Reset flags.
            IsDrifting = false;
            IsTractionLocked = false;
            
            // Stop all InvokeRepeating calls.
            StopAllCoroutines();
        }
        
        /// <summary>
        /// Accelerates the vehicle forward.
        /// </summary>
        public void GoForward()
        {
            CheckIfDrifting();

            // Smoothly increase throttle axis up to +1.
            _throttleAxis += Time.deltaTime * 3f;
            _throttleAxis = Mathf.Clamp(_throttleAxis, -1f, 1f);

            // If the car is moving backward, apply brakes before going forward.
            if (LocalVelocityZ < -1f)
            {
                Brakes();
            }
            else
            {
                if (Mathf.RoundToInt(CarSpeed) < maxSpeed)
                {
                    ApplyMotorTorque(_throttleAxis);
                }
                else
                {
                    StopMotorTorque();
                }
            }
        }

        /// <summary>
        /// Accelerates the vehicle in reverse.
        /// </summary>
        public void GoReverse()
        {
            CheckIfDrifting();

            // Smoothly decrease throttle axis down to -1.
            _throttleAxis -= Time.deltaTime * 3f;
            _throttleAxis = Mathf.Clamp(_throttleAxis, -1f, 1f);

            // If the car is moving forward, apply brakes before going reverse.
            if (LocalVelocityZ > 1f)
            {
                Brakes();
            }
            else
            {
                if (Mathf.Abs(Mathf.RoundToInt(CarSpeed)) < maxReverseSpeed)
                {
                    ApplyMotorTorque(_throttleAxis);
                }
                else
                {
                    StopMotorTorque();
                }
            }
        }

        /// <summary>
        /// Releases the accelerator (no braking).
        /// </summary>
        public void ThrottleOff()
        {
            StopMotorTorque();
        }

        /// <summary>
        /// Starts a progressive deceleration when no throttle or brake is applied.
        /// </summary>
        public void StartDeceleration()
        {
            if (!_deceleratingCar)
            {
                InvokeRepeating(nameof(DecelerateCar), 0f, 0.1f);
                _deceleratingCar = true;
            }
        }

        /// <summary>
        /// Stops the progressive deceleration (e.g. when throttle is pressed again).
        /// </summary>
        public void StopDeceleration()
        {
            CancelInvoke(nameof(DecelerateCar));
            _deceleratingCar = false;
        }

        /// <summary>
        /// Handbrake (drift) action. It also applies braking torque to slow down.
        /// </summary>
        public void Handbrake()
        {
            CancelInvoke(nameof(RecoverTraction));

            // Increase drifting axis up to 1.
            _driftingAxis += Time.deltaTime;
            _driftingAxis = Mathf.Clamp(_driftingAxis, 0f, 1f);

            CheckIfDrifting();

            // Apply drifting friction.
            ApplyDriftFriction(_driftingAxis);

            // Also apply braking so the vehicle will not keep infinite speed.
            Brakes();

            // The traction is locked (sliding).
            IsTractionLocked = true;
        }

        /// <summary>
        /// Recovers normal traction after releasing the handbrake.
        /// </summary>
        public void RecoverTraction()
        {
            IsTractionLocked = false;
            _driftingAxis -= (Time.deltaTime / 1.5f);

            if (_driftingAxis > 0f)
            {
                ApplyDriftFriction(_driftingAxis);
                Invoke(nameof(RecoverTraction), Time.deltaTime);
            }
            else
            {
                // Fully restore friction to original values.
                RestoreWheelFriction();
                _driftingAxis = 0f;
            }
        }

        /// <summary>
        /// Steers the front wheels to the left.
        /// </summary>
        public void TurnLeft()
        {
            _steeringAxis -= (Time.deltaTime * 10f * steeringSpeed);
            _steeringAxis = Mathf.Clamp(_steeringAxis, -1f, 1f);
            SetSteeringAngle(_steeringAxis);
        }

        /// <summary>
        /// Steers the front wheels to the right.
        /// </summary>
        public void TurnRight()
        {
            _steeringAxis += (Time.deltaTime * 10f * steeringSpeed);
            _steeringAxis = Mathf.Clamp(_steeringAxis, -1f, 1f);
            SetSteeringAngle(_steeringAxis);
        }

        /// <summary>
        /// Resets the steering axis to zero (straight wheels).
        /// </summary>
        public void ResetSteeringAngle()
        {
            if (_steeringAxis < 0f)
                _steeringAxis += (Time.deltaTime * 10f * steeringSpeed);
            else if (_steeringAxis > 0f)
                _steeringAxis -= (Time.deltaTime * 10f * steeringSpeed);

            if (Mathf.Abs(_steeringAxis) < 0.01f)
                _steeringAxis = 0f;

            SetSteeringAngle(_steeringAxis);
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Checks if the car is drifting based on its local X velocity.
        /// </summary>
        private void CheckIfDrifting()
        {
            // Default: not drifting
            IsDrifting = false;

            // If we are moving forward (or basically not negative localVelocityZ).
            if (LocalVelocityZ >= 0f)
            {
                // Check if speed is above minimum to drift forward
                IsDrifting = Mathf.Abs(CarSpeed) >= driftMinForwardSpeed &&
                             Mathf.Abs(LocalVelocityX) > driftSidewaysThresholdForward;
            }
            else // localVelocityZ < 0 => reversing
            {
                if (!allowReverseDrift)
                {
                    // If we do not allow drifting in reverse, exit.
                    return;
                }

                // If we do allow reverse drifting, check thresholds
                IsDrifting = Mathf.Abs(CarSpeed) >= driftMinReverseSpeed &&
                             Mathf.Abs(LocalVelocityX) > driftSidewaysThresholdReverse;
            }
        }

        /// <summary>
        /// Decelerates the car progressively when no throttle or brake is applied.
        /// This is invoked repeatedly by InvokeRepeating().
        /// </summary>
        private void DecelerateCar()
        {
            CheckIfDrifting();

            // Smoothly move the throttle axis towards 0.
            if (_throttleAxis > 0f)
                _throttleAxis -= (Time.deltaTime * 10f);
            else if (_throttleAxis < 0f)
                _throttleAxis += (Time.deltaTime * 10f);

            if (Mathf.Abs(_throttleAxis) < 0.15f)
                _throttleAxis = 0f;

            // Reduce velocity to simulate friction/resistance.
            _rb.linearVelocity *= (1f / (1f + (0.025f * decelerationMultiplier)));

            StopMotorTorque();

            // Stop completely if speed is very low.
            if (_rb.linearVelocity.magnitude < 0.25f)
            {
                _rb.linearVelocity = Vector3.zero;
                CancelInvoke(nameof(DecelerateCar));
            }
        }

        /// <summary>
        /// Applies motor torque to all wheels.
        /// </summary>
        private void ApplyMotorTorque(float axis)
        {
            // Ensure brake torque is zero before applying motor torque.
            frontLeftCollider.brakeTorque = 0;
            frontRightCollider.brakeTorque = 0;
            rearLeftCollider.brakeTorque = 0;
            rearRightCollider.brakeTorque = 0;

            float torqueValue = accelerationMultiplier * 50f * axis;

            frontLeftCollider.motorTorque = torqueValue;
            frontRightCollider.motorTorque = torqueValue;
            rearLeftCollider.motorTorque = torqueValue;
            rearRightCollider.motorTorque = torqueValue;
        }

        /// <summary>
        /// Stops motor torque on all wheels (without applying brakes).
        /// </summary>
        private void StopMotorTorque()
        {
            frontLeftCollider.motorTorque = 0;
            frontRightCollider.motorTorque = 0;
            rearLeftCollider.motorTorque = 0;
            rearRightCollider.motorTorque = 0;
        }

        /// <summary>
        /// Applies brake force to all four wheels.
        /// </summary>
        private void Brakes()
        {
            frontLeftCollider.brakeTorque = brakeForce;
            frontRightCollider.brakeTorque = brakeForce;
            rearLeftCollider.brakeTorque = brakeForce;
            rearRightCollider.brakeTorque = brakeForce;
        }

        /// <summary>
        /// Sets the steering angle of the front wheels.
        /// </summary>
        private void SetSteeringAngle(float axis)
        {
            float targetAngle = axis * maxSteeringAngle;
            frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, targetAngle, steeringSpeed);
            frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, targetAngle, steeringSpeed);
        }

        /// <summary>
        /// Stores the original sideways friction of all wheels.
        /// </summary>
        private void StoreWheelFriction()
        {
            _fLWheelFriction = frontLeftCollider.sidewaysFriction;
            _flWheelExtremumSlip = _fLWheelFriction.extremumSlip;

            _frWheelFriction = frontRightCollider.sidewaysFriction;
            _frWheelExtremumSlip = _frWheelFriction.extremumSlip;

            _rLWheelFriction = rearLeftCollider.sidewaysFriction;
            _rlWheelExtremumSlip = _rLWheelFriction.extremumSlip;

            _rrWheelFriction = rearRightCollider.sidewaysFriction;
            _rrWheelExtremumSlip = _rrWheelFriction.extremumSlip;
        }

        /// <summary>
        /// Applies drift friction by multiplying the original extremumSlip.
        /// </summary>
        private void ApplyDriftFriction(float driftingAxis)
        {
            float newSlipFactor = driftingAxis * handbrakeDriftMultiplier;

            // If you want an even more arcade feel, you could skip clamping or clamp higher.
            if (newSlipFactor < 1f)
                newSlipFactor = 1f;

            if (newSlipFactor > handbrakeDriftMultiplier)
                newSlipFactor = handbrakeDriftMultiplier;

            // Front-Left
            WheelFrictionCurve flFriction = _fLWheelFriction;
            flFriction.extremumSlip = _flWheelExtremumSlip * newSlipFactor;
            frontLeftCollider.sidewaysFriction = flFriction;

            // Front-Right
            WheelFrictionCurve frFriction = _frWheelFriction;
            frFriction.extremumSlip = _frWheelExtremumSlip * newSlipFactor;
            frontRightCollider.sidewaysFriction = frFriction;

            // Rear-Left
            WheelFrictionCurve rlFriction = _rLWheelFriction;
            rlFriction.extremumSlip = _rlWheelExtremumSlip * newSlipFactor;
            rearLeftCollider.sidewaysFriction = rlFriction;

            // Rear-Right
            WheelFrictionCurve rrFriction = _rrWheelFriction;
            rrFriction.extremumSlip = _rrWheelExtremumSlip * newSlipFactor;
            rearRightCollider.sidewaysFriction = rrFriction;
        }

        /// <summary>
        /// Restores the original friction values to all wheels (after drifting).
        /// </summary>
        private void RestoreWheelFriction()
        {
            WheelFrictionCurve flFriction = _fLWheelFriction;
            flFriction.extremumSlip = _flWheelExtremumSlip;
            frontLeftCollider.sidewaysFriction = flFriction;

            WheelFrictionCurve frFriction = _frWheelFriction;
            frFriction.extremumSlip = _frWheelExtremumSlip;
            frontRightCollider.sidewaysFriction = frFriction;

            WheelFrictionCurve rlFriction = _rLWheelFriction;
            rlFriction.extremumSlip = _rlWheelExtremumSlip;
            rearLeftCollider.sidewaysFriction = rlFriction;

            WheelFrictionCurve rrFriction = _rrWheelFriction;
            rrFriction.extremumSlip = _rrWheelExtremumSlip;
            rearRightCollider.sidewaysFriction = rrFriction;
        }

        /// <summary>
        /// Updates the mesh position/rotation to match the WheelCollider.
        /// </summary>
        private void AnimateWheelMeshes()
        {
            SyncWheel(frontLeftCollider, frontLeftMesh);
            SyncWheel(frontRightCollider, frontRightMesh);
            SyncWheel(rearLeftCollider, rearLeftMesh);
            SyncWheel(rearRightCollider, rearRightMesh);
        }

        /// <summary>
        /// Syncs a single WheelCollider with its corresponding wheel mesh.
        /// </summary>
        private void SyncWheel(WheelCollider col, GameObject mesh)
        {
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.transform.position = pos;
            mesh.transform.rotation = rot;
        }

        #endregion
    }
}