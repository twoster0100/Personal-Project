using System;
using System.Collections.Generic;

namespace MyGame.Application.Tick
{
    public sealed class TickScheduler
    {
        private readonly List<ISimulationTickable> _sim = new();
        private readonly List<IFrameTickable> _frame = new();
        private readonly List<IUnscaledFrameTickable> _unscaled = new();

        public void Register(object o)
        {
            if (o is ISimulationTickable s && !_sim.Contains(s)) _sim.Add(s);
            if (o is IFrameTickable f && !_frame.Contains(f)) _frame.Add(f);
            if (o is IUnscaledFrameTickable u && !_unscaled.Contains(u)) _unscaled.Add(u);
        }

        public void Unregister(object o)
        {
            if (o is ISimulationTickable s) _sim.Remove(s);
            if (o is IFrameTickable f) _frame.Remove(f);
            if (o is IUnscaledFrameTickable u) _unscaled.Remove(u);
        }

        public void DoFrame(float dt)
        {
            for (int i = 0; i < _frame.Count; i++) _frame[i].FrameTick(dt);
        }

        public void DoUnscaled(float unscaledDt)
        {
            for (int i = 0; i < _unscaled.Count; i++) _unscaled[i].UnscaledFrameTick(unscaledDt);
        }

        public void DoSimulationStep(float fixedDt)
        {
            for (int i = 0; i < _sim.Count; i++) _sim[i].SimulationTick(fixedDt);
        }
    }

    public sealed class SimulationClock
    {
        private readonly TickScheduler _scheduler;
        private readonly float _fixedDt;
        private readonly int _maxStepsPerFrame;
        private float _acc;

        public SimulationClock(TickScheduler scheduler, int tickRate = 30, int maxStepsPerFrame = 5)
        {
            _scheduler = scheduler;
            _fixedDt = 1f / tickRate;
            _maxStepsPerFrame = Math.Max(1, maxStepsPerFrame);
        }

        public void Advance(float dt)
        {
            _acc += dt;
            int steps = 0;

            while (_acc >= _fixedDt && steps < _maxStepsPerFrame)
            {
                _acc -= _fixedDt;
                _scheduler.DoSimulationStep(_fixedDt);
                steps++;
            }

            // 프레임 드랍이 심하면 spiral of death 방지용으로 잔여 누적을 버림(안정 우선)
            if (steps >= _maxStepsPerFrame)
                _acc = 0f;
        }
    }
}
