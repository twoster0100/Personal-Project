namespace MyGame.Application.Tick
{
    public interface ISimulationTickable
    {
        void SimulationTick(float dt);
    }

    public interface IFrameTickable
    {
        void FrameTick(float dt);
    }

    // ✅ LateUpdate 대체 단계
    public interface ILateFrameTickable
    {
        void LateFrameTick(float dt);
    }

    public interface IUnscaledFrameTickable
    {
        void UnscaledFrameTick(float unscaledDt);
    }
}
