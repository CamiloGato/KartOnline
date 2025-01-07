using UnityEngine;

namespace CarController
{
    [RequireComponent(typeof(CarSpawns))]
    [RequireComponent(typeof(CarController))]
    public class CarInput : MonoBehaviour
    {
        private CarController _car;
        private CarSpawns _carSpawns;

        private void Start()
        {
            _car = GetComponent<CarController>();
            _carSpawns = GetComponent<CarSpawns>();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.R) && _car.CarSpeed < 1f)
            {
                _carSpawns.Spawn();
            }
            
            if (Input.GetKey(KeyCode.W))
            {
                _car.StopDeceleration();
                _car.GoForward();
            }

            if (Input.GetKey(KeyCode.S))
            {
                _car.StopDeceleration();
                _car.GoReverse();
            }

            if (Input.GetKey(KeyCode.A))
            {
                _car.TurnLeft();
            }

            if (Input.GetKey(KeyCode.D))
            {
                _car.TurnRight();
            }

            if (Input.GetKey(KeyCode.Space))
            {
                _car.StopDeceleration();
                _car.Handbrake();
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                _car.RecoverTraction();
            }

            if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
            {
                _car.ThrottleOff();
            }

            if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.Space))
            {
                _car.StartDeceleration();
            }

            if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {
                _car.ResetSteeringAngle();
            }
        }
    }
}