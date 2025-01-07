using UnityEngine;

namespace CarController
{
    /// <summary>
    /// Manages the car's audio, including the engine sound pitch and tire screech sounds.
    /// Relies on a CarController reference to retrieve speed and drifting information.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class CarAudio : MonoBehaviour
    {
        [SerializeField] private bool useSounds;

        [Header("Audio Sources")]
        public AudioSource carEngineSound;
        public AudioSource tireScreechSound;

        private CarController _car;
        private float _initialCarEnginePitch;

        private void Start()
        {
            _car = GetComponent<CarController>();
            
            if (carEngineSound)
            {
                _initialCarEnginePitch = carEngineSound.pitch;
            }
        }

        private void Update()
        {
            if (!useSounds)
            {
                StopSounds();
                return;
            }

            UpdateCarEngineSound();
            UpdateTireScreechSound();
        }

        /// <summary>
        /// Adjusts the pitch of the engine sound according to the car's velocity.
        /// The faster the car moves, the higher the pitch becomes.
        /// </summary>
        private void UpdateCarEngineSound()
        {
            if (carEngineSound)
            {
                // Assuming 'LinearVelocityMagnitude' is a property of CarController
                // that returns the magnitude of the car's velocity.
                float newPitch = _initialCarEnginePitch + (Mathf.Abs(_car.LinearVelocityMagnitude) / 25f);
                carEngineSound.pitch = newPitch;
            }
        }

        /// <summary>
        /// Controls the tire screech sound. 
        /// It plays when the car is drifting or when traction is locked above a certain speed.
        /// </summary>
        private void UpdateTireScreechSound()
        {
            if (tireScreechSound)
            {
                // 'CarSpeed' is assumed to be in km/h (or a similar unit),
                // while 'IsDrifting' and 'IsTractionLocked' are booleans 
                // from the CarController indicating drifting/handbrake state.
                if (_car.IsDrifting || (_car.IsTractionLocked && Mathf.Abs(_car.CarSpeed) > 12f))
                {
                    if (!tireScreechSound.isPlaying)
                    {
                        tireScreechSound.Play();
                    }
                }
                else
                {
                    tireScreechSound.Stop();
                }
            }
        }

        /// <summary>
        /// Stops both the engine and tire screech sounds if they are currently playing.
        /// Called when useSounds is disabled or when the script needs to reset audio.
        /// </summary>
        private void StopSounds()
        {
            if (carEngineSound && carEngineSound.isPlaying)
            {
                carEngineSound.Stop();
            }

            if (tireScreechSound && tireScreechSound.isPlaying)
            {
                tireScreechSound.Stop();
            }
        }
    }
}
