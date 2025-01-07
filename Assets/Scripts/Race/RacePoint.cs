using UnityEngine;

namespace Race
{
    public class RacePoint : MonoBehaviour
    {
        public Transform spawnPoint;
        public bool isFinishLine;

        private void Awake()
        {
            if (!spawnPoint)
            {
                spawnPoint = transform;
            }
        }
    }
}