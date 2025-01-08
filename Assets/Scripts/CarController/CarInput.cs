using System;
using System.Collections.Generic;
using CarController.Payload;
using Tools;
using Tools.Extensions;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace CarController
{
    [RequireComponent(typeof(CarSpawns))]
    [RequireComponent(typeof(CarController))]
    public class CarInput : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachineCamera lookCamera;
        [SerializeField] private AudioListener audioListener;
        
        [Header("Configuration")] 
        [SerializeField] float reconciliationCooldownTime = 1f;
        [SerializeField] float reconciliationThreshold = 10f;
        [SerializeField] float extrapolationLimit = 0.5f;
        [SerializeField] float extrapolationMultiplier = 1.2f;
        
        // Netcode general
        private NetworkTimer _networkTimer;
        private const float ServerTickRate = 60f; // 60 FPS
        private const int BufferSize = 1024;
        
        // Netcode client specific
        private CircularBuffer<StatePayload> _clientStateBuffer;
        private CircularBuffer<InputPayload> _clientInputBuffer;
        private StatePayload _lastServerState;
        private StatePayload _lastProcessedState;

        private ClientNetworkTransform _clientNetworkTransform;
        
        // Netcode server specific
        private CircularBuffer<StatePayload> _serverStateBuffer;
        private Queue<InputPayload> _serverInputQueue;
        
        private CountdownTimer _reconciliationTimer;
        private CountdownTimer _extrapolationTimer;
        private StatePayload _extrapolationState;
        
        private CarController _car;
        private CarSpawns _carSpawns;

        private void Start()
        {
            if (!IsOwner)
            {
                lookCamera.Priority = 0;
                audioListener.enabled = false;
            }
            else
            {
                lookCamera.Priority = 100;
                audioListener.enabled = true;
            }
            
            _car = GetComponent<CarController>();
            _carSpawns = GetComponent<CarSpawns>();
            _clientNetworkTransform = GetComponent<ClientNetworkTransform>();
            
            _networkTimer = new NetworkTimer(ServerTickRate);
            _clientStateBuffer = new CircularBuffer<StatePayload>(BufferSize);
            _clientInputBuffer = new CircularBuffer<InputPayload>(BufferSize);
            
            _serverStateBuffer = new CircularBuffer<StatePayload>(BufferSize);
            _serverInputQueue = new Queue<InputPayload>();
            
            _reconciliationTimer = new CountdownTimer(reconciliationCooldownTime);
            _extrapolationTimer = new CountdownTimer(extrapolationLimit);
            
            _reconciliationTimer.OnTimerStart += () => {
                _extrapolationTimer.Stop();
            };
            
            _extrapolationTimer.OnTimerStart += () => {
                _reconciliationTimer.Stop();
                SwitchAuthorityMode(AuthorityMode.Server);
            };
            _extrapolationTimer.OnTimerStop += () => {
                _extrapolationState = default;
                SwitchAuthorityMode(AuthorityMode.Client);
            };
        }

        private void Update()
        {
            _networkTimer.Update(Time.deltaTime);
            _reconciliationTimer.Tick(Time.deltaTime);
            _extrapolationTimer.Tick(Time.deltaTime);
            Extraplolate();
        }

        void FixedUpdate()
        {
            while (_networkTimer.ShouldTick()) {
                HandleClientTick();
                HandleServerTick();
            }
            
            Extraplolate();
        }
        
        void SwitchAuthorityMode(AuthorityMode mode) {
            _clientNetworkTransform.authorityMode = mode;
            bool shouldSync = mode == AuthorityMode.Client;
            _clientNetworkTransform.SyncPositionX = shouldSync;
            _clientNetworkTransform.SyncPositionY = shouldSync;
            _clientNetworkTransform.SyncPositionZ = shouldSync;
            _car.CarRigidbody.isKinematic = !shouldSync;
        }

        void HandleServerTick() {
            if (!IsServer) return;
            
            Move(new InputPayload.InputData());
            
            var bufferIndex = -1;
            while (_serverInputQueue.Count > 0) {
                InputPayload inputPayload = _serverInputQueue.Dequeue();
                bufferIndex = inputPayload.Tick % BufferSize;

                StatePayload statePayload;
                
                if (IsHost)
                {
                    statePayload = new StatePayload()
                    {
                        Tick = inputPayload.Tick,
                        NetworkObjectId = NetworkObjectId,
                        Position = transform.position,
                        Rotation = transform.rotation,
                        Velocity = _car.CarRigidbody.linearVelocity,
                        AngularVelocity = _car.CarRigidbody.angularVelocity,
                    };
                    _serverStateBuffer.Add(statePayload, bufferIndex);
                    SendToClientRpc(statePayload);
                    continue;
                }
                
                statePayload = ProcessMovement(inputPayload);
                _serverStateBuffer.Add(statePayload, bufferIndex);
            }
            
            if (bufferIndex == -1) return;
            SendToClientRpc(_serverStateBuffer.Get(bufferIndex));
            float latency = (float) (NetworkManager.ServerTime.Time - NetworkManager.LocalTime.Time);
            HandleExtrapolation(_serverStateBuffer.Get(bufferIndex), latency);
        }

        void Extraplolate() {
            if (IsServer && _extrapolationTimer.IsRunning) {
                transform.position += _extrapolationState.Position.With(y: 0);
            }
        }
        
        private float _smoothedLatency;
        void HandleExtrapolation(StatePayload latest, float latency) {
            _smoothedLatency = Mathf.Lerp(_smoothedLatency, latency, 0.1f);
            if (ShouldExtrapolate(latency)) {
                float axisLength = latency * latest.AngularVelocity.magnitude * Mathf.Rad2Deg;
                Quaternion angularRotation = Quaternion.AngleAxis(axisLength, latest.AngularVelocity);
                
                if (_extrapolationState.Position != default) {
                    latest = _extrapolationState;
                }
                
                var posAdjustment = latest.Velocity * (1 + latency * extrapolationMultiplier);
                _extrapolationState.Position = posAdjustment;
                _extrapolationState.Rotation = angularRotation * transform.rotation;
                _extrapolationState.Velocity = latest.Velocity;
                _extrapolationState.AngularVelocity = latest.AngularVelocity;
                _extrapolationTimer.Start();
            } else {
                _extrapolationTimer.Stop();
            }
        }

        bool ShouldExtrapolate(float latency) => latency < extrapolationLimit && latency > Time.fixedDeltaTime;

        [ClientRpc]
        void SendToClientRpc(StatePayload statePayload) {
            if (!IsOwner) return;
            _lastServerState = statePayload;
        }

        void HandleClientTick() {
            if (!IsClient || !IsOwner) return;

            var currentTick = _networkTimer.CurrentTick;
            var bufferIndex = currentTick % BufferSize;
            
            InputPayload inputPayload = new InputPayload() {
                Tick = currentTick,
                Timestamp = DateTime.Now,
                NetworkObjectId = NetworkObjectId,
                Input = new InputPayload.InputData()
                {
                    forward = Input.GetKey(KeyCode.W),
                    backward = Input.GetKey(KeyCode.S),
                    left = Input.GetKey(KeyCode.A),
                    right = Input.GetKey(KeyCode.D),
                    brake = Input.GetKey(KeyCode.Space),
                    respawn = Input.GetKey(KeyCode.R)
                },
                Position = transform.position
            };
            
            _clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRpc(inputPayload);
            
            StatePayload statePayload = ProcessMovement(inputPayload);
            _clientStateBuffer.Add(statePayload, bufferIndex);
            
            HandleServerReconciliation();
        }

        bool ShouldReconcile() {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = _lastProcessedState.Equals(default) 
                                                   || !_lastProcessedState.Equals(_lastServerState);
            
            return isNewServerState && isLastStateUndefinedOrDifferent && !_reconciliationTimer.IsRunning && !_extrapolationTimer.IsRunning;
        }

        void HandleServerReconciliation() {
            if (!ShouldReconcile()) return;

            var bufferIndex = _lastServerState.Tick % BufferSize;
            if (bufferIndex - 1 < 0) return;
            
            StatePayload rewindState = IsHost ? _serverStateBuffer.Get(bufferIndex - 1) : _lastServerState;
            StatePayload clientState = IsHost ? _clientStateBuffer.Get(bufferIndex - 1) : _clientStateBuffer.Get(bufferIndex);
            var positionError = Vector3.Distance(rewindState.Position, clientState.Position);

            if (positionError > reconciliationThreshold) {
                ReconcileState(rewindState);
                _reconciliationTimer.Start();
            }

            _lastProcessedState = rewindState;
        }

        void ReconcileState(StatePayload rewindState) {
            transform.position = rewindState.Position;
            transform.rotation = rewindState.Rotation;
            _car.CarRigidbody.linearVelocity = rewindState.Velocity;
            _car.CarRigidbody.angularVelocity = rewindState.AngularVelocity;

            if (!rewindState.Equals(_lastServerState)) return;
            
            _clientStateBuffer.Add(rewindState, rewindState.Tick % BufferSize);
            
            // Replay all inputs from the rewind state to the current state
            int tickToReplay = _lastServerState.Tick;

            while (tickToReplay < _networkTimer.CurrentTick) {
                int bufferIndex = tickToReplay % BufferSize;
                StatePayload statePayload = ProcessMovement(_clientInputBuffer.Get(bufferIndex));
                _clientStateBuffer.Add(statePayload, bufferIndex);
                tickToReplay++;
            }
        }
        
        [ServerRpc]
        void SendToServerRpc(InputPayload input) {
            _serverInputQueue.Enqueue(input);
        }

        StatePayload ProcessMovement(InputPayload input) {
            Move(input.Input);
            
            return new StatePayload() {
                Tick = input.Tick,
                NetworkObjectId = NetworkObjectId,
                Position = transform.position,
                Rotation = transform.rotation,
                Velocity = _car.CarRigidbody.linearVelocity,
                AngularVelocity = _car.CarRigidbody.angularVelocity
            };
        }

        #region MOVE METHODS
        
        private void Move(InputPayload.InputData input)
        {
            HandleMovement(input);
            HandleRecover(input);
        }

        private void HandleRecover(InputPayload.InputData input)
        {
            if (!input.brake)
            {
                _car.RecoverTraction();
            }

            if (input is { forward: false, backward: false })
            {
                _car.ThrottleOff();
            }

            if (input is { forward: false, backward: false, brake: false })
            {
                _car.StartDeceleration();
            }

            if (input is { left: false, right: false })
            {
                _car.ResetSteeringAngle();
            }
        }

        private void HandleMovement(InputPayload.InputData input)
        {
            if (input.respawn && _car.CarSpeed < 1f)
            {
                _carSpawns.Spawn();
            }
            
            if (input.forward)
            {
                _car.StopDeceleration();
                _car.GoForward();
            }

            if (input.backward)
            {
                _car.StopDeceleration();
                _car.GoReverse();
            }

            if (input.left)
            {
                _car.TurnLeft();
            }

            if (input.right)
            {
                _car.TurnRight();
            }

            if (input.brake)
            {
                _car.StopDeceleration();
                _car.Handbrake();
            }
        }
        
        #endregion
    }
}