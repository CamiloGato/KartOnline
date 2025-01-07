using CarController;
using UnityEngine;

namespace Race
{
    public class RaceVoid : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out CarSpawns carSpawns))
            {
                carSpawns.Spawn();
            }
        }
    }
}