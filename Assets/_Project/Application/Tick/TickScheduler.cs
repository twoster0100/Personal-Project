using System;
using System.Collections.Generic;

namespace MyGame.Application.Tick
{
    public sealed class TickScheduler
    {
        private readonly List<ISimulationTickable> _sim = new();
        private readonly List<IFrameTickable> _frame = new();
        private readonly List<ILateFrameTickable> _lateFrame = new();
        private readonly List<IUnscaledFrameTickable> _unscaled = new();

        private bool _isTicking;
        private readonly List<object> _pendingAdds = new();
        private readonly List<object> _pendingRemoves = new();

        public void Register(object o)
        {
            if (o == null) return;

            if (_isTicking)
            {
                if (!_pendingAdds.Contains(o)) _pendingAdds.Add(o);
                return;
            }

            RegisterImmediate(o);
        }

        public void Unregister(object o)
        {
            if (o == null) return;

            if (_isTicking)
            {
                if (!_pendingRemoves.Contains(o)) _pendingRemoves.Add(o);
                return;
            }

            UnregisterImmediate(o);
        }

        private void RegisterImmediate(object o)
        {
            if (o is ISimulationTickable s && !_sim.Contains(s)) _sim.Add(s);
            if (o is IFrameTickable f && !_frame.Contains(f)) _frame.Add(f);
            if (o is ILateFrameTickable lf && !_lateFrame.Contains(lf)) _lateFrame.Add(lf);
            if (o is IUnscaledFrameTickable u && !_unscaled.Contains(u)) _unscaled.Add(u);
        }

        private void UnregisterImmediate(object o)
        {
            if (o is ISimulationTickable s) _sim.Remove(s);
            if (o is IFrameTickable f) _frame.Remove(f);
            if (o is ILateFrameTickable lf) _lateFrame.Remove(lf);
            if (o is IUnscaledFrameTickable u) _unscaled.Remove(u);
        }

        private void FlushPending()
        {
            // Unity스러운 동작: 제거가 먼저, 추가가 나중
            if (_pendingRemoves.Count > 0)
            {
                for (int i = 0; i < _pendingRemoves.Count; i++)
                    UnregisterImmediate(_pendingRemoves[i]);
                _pendingRemoves.Clear();
            }

            if (_pendingAdds.Count > 0)
            {
                for (int i = 0; i < _pendingAdds.Count; i++)
                    RegisterImmediate(_pendingAdds[i]);
                _pendingAdds.Clear();
            }
        }

        private void RunTick(Action body)
        {
            _isTicking = true;
            try { body(); }
            finally
            {
                _isTicking = false;
                FlushPending();
            }
        }

        public void DoFrame(float dt)
        {
            RunTick(() =>
            {
                for (int i = 0; i < _frame.Count; i++) _frame[i].FrameTick(dt);
            });
        }

        public void DoLateFrame(float dt)
        {
            RunTick(() =>
            {
                for (int i = 0; i < _lateFrame.Count; i++) _lateFrame[i].LateFrameTick(dt);
            });
        }

        public void DoUnscaled(float unscaledDt)
        {
            RunTick(() =>
            {
                for (int i = 0; i < _unscaled.Count; i++) _unscaled[i].UnscaledFrameTick(unscaledDt);
            });
        }

        public void DoSimulationStep(float fixedDt)
        {
            RunTick(() =>
            {
                for (int i = 0; i < _sim.Count; i++) _sim[i].SimulationTick(fixedDt);
            });
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

            if (steps >= _maxStepsPerFrame)
                _acc = 0f;
        }
    }
}
