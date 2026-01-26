namespace MyGame.Application.Tick
{
    // 30Hz 같은 "시뮬레이션" 고정틱
    public interface ISimulationTickable
    {
        void SimulationTick(float dt);
    }

    // 렌더 프레임(30/60)마다 1회
    public interface IFrameTickable
    {
        void FrameTick(float dt);
    }

    // LateUpdate 대체(프레임 Tick이 모두 끝난 뒤 실행)
    public interface ILateFrameTickable
    {
        void LateFrameTick(float dt);
    }

    // UI/연출 등 TimeScale=0에서도 돌아야 하면 이걸 사용
    public interface IUnscaledFrameTickable
    {
        void UnscaledFrameTick(float unscaledDt);
    }
}
