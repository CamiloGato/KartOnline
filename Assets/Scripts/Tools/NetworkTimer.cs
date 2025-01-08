namespace Tools {
    public class NetworkTimer {
        private float _timer;
        public float MinTimeBetweenTicks { get; }
        public int CurrentTick { get; private set; }
        
        public NetworkTimer(float serverTickRate) {
            MinTimeBetweenTicks = 1f / serverTickRate;
        }

        public void Update(float deltaTime) {
            _timer += deltaTime;
        }
        
        public bool ShouldTick() {
            if (_timer >= MinTimeBetweenTicks) {
                _timer -= MinTimeBetweenTicks;
                CurrentTick++;
                return true;
            }

            return false;
        }
    }
}