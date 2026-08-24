using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // One tick equals one simulated day. Normal fires every 2 seconds, Fast every
    // 0.25 seconds, Paused never (Design Document 3.2). Three options only.
    //
    // Catches up at most a handful of ticks per frame so a hitch or a long frame
    // cannot turn into a burst of dozens of days the learner never sees.
    public class EcosystemClock
    {
        const int MaxTicksPerFrame = 4;

        public int speed = 1;
        float _accumulator;

        public bool IsPaused => Mathf.Clamp(speed, 0, 2) == 0;

        public float SecondsPerDay =>
            EcosystemSettings.SecondsPerDay[Mathf.Clamp(speed, 0, EcosystemSettings.SecondsPerDay.Length - 1)];

        // Fraction of the way to the next tick, for smoothing the readouts.
        public float TickProgress
        {
            get
            {
                float period = SecondsPerDay;
                return period > 0f ? Mathf.Clamp01(_accumulator / period) : 0f;
            }
        }

        public void Reset()
        {
            _accumulator = 0f;
        }

        // Returns how many days should elapse this frame.
        public int Advance(float deltaTime)
        {
            if (IsPaused) { _accumulator = 0f; return 0; }

            float period = SecondsPerDay;
            if (period <= 0f) return 0;

            _accumulator += deltaTime;

            int ticks = 0;
            while (_accumulator >= period && ticks < MaxTicksPerFrame)
            {
                _accumulator -= period;
                ticks++;
            }

            // Drop any further backlog rather than letting it snowball.
            if (_accumulator > period) _accumulator = 0f;

            return ticks;
        }
    }
}
