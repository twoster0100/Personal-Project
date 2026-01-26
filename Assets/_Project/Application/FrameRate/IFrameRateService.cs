namespace MyGame.Infrastructure.FrameRate
{
    public enum FrameRateMode
    {
        Idle30,
        High60
    }

    public interface IFrameRateService
    {
        FrameRateMode Current { get; }
        void SetMode(FrameRateMode mode);
    }
}
