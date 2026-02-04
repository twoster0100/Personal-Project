using System;
using System.Collections.Generic;

namespace MyGame.Application.Tick
{
    /// <summary>
    /// ✅ TickScheduler
    /// - Tick 중(Register/Unregister) 호출이 발생해도 안전하게 처리한다.
    /// - "현재 Tick 루프"가 끝난 뒤에 pending add/remove를 반영
    /// </summary>
    public sealed class TickScheduler
    {
        private readonly List<IFrameTickable> _frame = new();
        private readonly List<ILateFrameTickable> _late = new();
        private readonly List<IUnscaledFrameTickable> _unscaled = new();
        private readonly List<ISimulationTickable> _simulation = new();

        // Tick 도중 컬렉션 변형을 막기 위한 큐
        private readonly List<object> _pendingAdd = new();
        private readonly List<object> _pendingRemove = new();

        // DoXXX가 중첩 호출될 수 있으므로 depth로 관리 (try/finally로 안전)
        private int _iterateDepth;

        public void Register(object tickable)
        {
            if (tickable == null) return;

            if (_iterateDepth > 0)
            {
                // Tick 도중 등록 요청 → 큐에 저장
                _pendingRemove.Remove(tickable);
                if (!_pendingAdd.Contains(tickable))
                    _pendingAdd.Add(tickable);
                return;
            }

            InternalRegister(tickable);
        }

        public void Unregister(object tickable)
        {
            if (tickable == null) return;

            if (_iterateDepth > 0)
            {
                // Tick 도중 해제 요청 → 큐에 저장
                _pendingAdd.Remove(tickable);
                if (!_pendingRemove.Contains(tickable))
                    _pendingRemove.Add(tickable);
                return;
            }

            InternalUnregister(tickable);
        }

        public void DoFrame(float dt)
        {
            BeginIterate();
            try
            {
                for (int i = 0; i < _frame.Count; i++)
                {
                    var t = _frame[i];
                    if (t == null) { _frame.RemoveAt(i); i--; continue; }
                    t.FrameTick(dt);
                }
            }
            finally { EndIterate(); }
        }

        public void DoLateFrame(float dt)
        {
            BeginIterate();
            try
            {
                for (int i = 0; i < _late.Count; i++)
                {
                    var t = _late[i];
                    if (t == null) { _late.RemoveAt(i); i--; continue; }
                    t.LateFrameTick(dt);
                }
            }
            finally { EndIterate(); }
        }

        public void DoUnscaled(float unscaledDt)
        {
            BeginIterate();
            try
            {
                for (int i = 0; i < _unscaled.Count; i++)
                {
                    var t = _unscaled[i];
                    if (t == null) { _unscaled.RemoveAt(i); i--; continue; }
                    t.UnscaledFrameTick(unscaledDt);
                }
            }
            finally { EndIterate(); }
        }

        public void DoSimulationStep(float fixedDt)
        {
            BeginIterate();
            try
            {
                for (int i = 0; i < _simulation.Count; i++)
                {
                    var t = _simulation[i];
                    if (t == null) { _simulation.RemoveAt(i); i--; continue; }
                    t.SimulationTick(fixedDt);
                }
            }
            finally { EndIterate(); }
        }

        private void BeginIterate() => _iterateDepth++;

        private void EndIterate()
        {
            _iterateDepth--;
            if (_iterateDepth != 0) return;

            // Tick 루프가 끝난 시점에 pending 반영
            if (_pendingRemove.Count > 0)
            {
                for (int i = 0; i < _pendingRemove.Count; i++)
                    InternalUnregister(_pendingRemove[i]);
                _pendingRemove.Clear();
            }

            if (_pendingAdd.Count > 0)
            {
                for (int i = 0; i < _pendingAdd.Count; i++)
                    InternalRegister(_pendingAdd[i]);
                _pendingAdd.Clear();
            }
        }

        private void InternalRegister(object o)
        {
            if (o is IFrameTickable f && !_frame.Contains(f)) _frame.Add(f);
            if (o is ILateFrameTickable l && !_late.Contains(l)) _late.Add(l);
            if (o is IUnscaledFrameTickable u && !_unscaled.Contains(u)) _unscaled.Add(u);
            if (o is ISimulationTickable s && !_simulation.Contains(s)) _simulation.Add(s);
        }

        private void InternalUnregister(object o)
        {
            if (o is IFrameTickable f) _frame.Remove(f);
            if (o is ILateFrameTickable l) _late.Remove(l);
            if (o is IUnscaledFrameTickable u) _unscaled.Remove(u);
            if (o is ISimulationTickable s) _simulation.Remove(s);
        }
    }

 
        /// <summary>
        /// 30Hz 같은 "고정 시뮬레이션 Tick"을 프레임 dt로부터 스텝으로 쪼개 실행하는 클럭.
        /// - AppCompositionRoot에서: new SimulationClock(Ticks, tickRate: 30, maxStepsPerFrame: 5)
        /// </summary>
        public sealed class SimulationClock
        {
            private readonly TickScheduler _ticks;
            private readonly float _fixedDt;
            private readonly int _maxSteps;

            private float _accum;

            // ✅ AppCompositionRoot가 기대하는 시그니처/파라미터 이름
            public SimulationClock(TickScheduler ticks, int tickRate = 30, int maxStepsPerFrame = 5)
            {
                _ticks = ticks ?? throw new ArgumentNullException(nameof(ticks));

                tickRate = Math.Max(1, tickRate);
                _fixedDt = 1f / tickRate;

                _maxSteps = Math.Max(1, maxStepsPerFrame);
                _accum = 0f;
            }

            /// <summary>
            /// 프레임 dt를 누적해, 고정 dt 단위로 시뮬레이션 스텝을 수행한다.
            /// </summary>
            public void Advance(float frameDt)
            {
                if (frameDt <= 0f) return;

                _accum += frameDt;

                int steps = 0;
                while (_accum >= _fixedDt && steps < _maxSteps)
                {
                    _accum -= _fixedDt;
                    steps++;

                    // ✅ 30Hz 시뮬레이션 스텝
                    _ticks.DoSimulationStep(_fixedDt);
                }

                // ✅ 너무 느린 프레임(스파이럴)에서 누적 폭주 방지
                if (steps >= _maxSteps)
                    _accum = 0f;
            }
        }
    }

