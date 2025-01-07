using System;
using Race;
using UnityEngine;

namespace CarController
{
    [RequireComponent(typeof(CarController))]
    public class CarSpawns : MonoBehaviour
    {
        public RacePoint currentRacePoint;
        private CarController _car;

        private void Start()
        {
            _car = GetComponent<CarController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out RacePoint racePoint))
            {
                currentRacePoint = racePoint;
            }
        }

        public void Spawn()
        {
            _car.ResetCar();
            transform.position = currentRacePoint.spawnPoint.position;
            transform.rotation = currentRacePoint.spawnPoint.rotation;
        }
        
    }
}