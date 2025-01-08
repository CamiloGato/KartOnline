using System;
using System.Collections.Generic;
using CarController.Payload;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace CarController
{
    [RequireComponent(typeof(CarSpawns))]
    [RequireComponent(typeof(CarController))]
    public class CarInputNo : NetworkBehaviour
    {
        // Netcode general
        private NetworkTimer _networkTimer;
        private const float ServerTickRate = 60f;
        
        // Netcode Server
        private Queue<InputPayload> _serverInputQueue;

        private CarController _car;
        private CarSpawns _carSpawns;

        private void Start()
        {
            _car = GetComponent<CarController>();
            _carSpawns = GetComponent<CarSpawns>();
            
            _networkTimer = new NetworkTimer(ServerTickRate);
            _serverInputQueue = new Queue<InputPayload>();
        }

        private void Update()
        {
            _networkTimer.Update(Time.deltaTime);
        }

        void FixedUpdate() {
            while (_networkTimer.ShouldTick()) {
                HandleClientTick();
                HandleServerTick();
            }
        }
        
        void HandleServerTick() {
            if (!IsServer) return;

            while (_serverInputQueue.Count > 0) {
                InputPayload inputPayload = _serverInputQueue.Dequeue();
                if (IsHost)
                {
                    StatePayload statePayload = new StatePayload()
                    {
                        Tick = inputPayload.Tick,
                        NetworkObjectId = NetworkObjectId,
                        Position = transform.position,
                        Rotation = transform.rotation,
                        Velocity = _car.CarRigidbody.linearVelocity,
                        AngularVelocity = _car.CarRigidbody.angularVelocity,
                    };
                    SendToClientRpc(statePayload);
                    continue;
                }
                
                ProcessMovement(inputPayload);
            }
        }
        
        [ClientRpc]
        void SendToClientRpc(StatePayload statePayload) {
            if (!IsOwner) return;
        }

        void HandleClientTick() {
            if (!IsClient || !IsOwner) return;

            int currentTick = _networkTimer.CurrentTick;
            
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
            
            SendToServerRpc(inputPayload);
            ProcessMovement(inputPayload);
        }
        
        [ServerRpc]
        void SendToServerRpc(InputPayload input) {
            _serverInputQueue.Enqueue(input);
        }

        void ProcessMovement(InputPayload input) {
            Move(input.Input);
        }

        #region CAR MOVEMENT
        
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