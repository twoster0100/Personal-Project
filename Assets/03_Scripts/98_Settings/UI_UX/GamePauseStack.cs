using UnityEngine;
/// <summary>
/// 게임중 일시정지 하는데 쓰이는 스크립트
/// </summary>
public static class GamePauseStack
{
    private static int _count;
    private static float _savedTimeScale = 1f;

    private static bool _savedAudioPause;
    private static bool _audioTouchedByUs;

    public static bool IsPaused => _count > 0;

    public static void Push(bool pauseAudio = false)
    {
        _count++;

        if (_count == 1)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (pauseAudio)
            {
                _savedAudioPause = AudioListener.pause;
                AudioListener.pause = true;
                _audioTouchedByUs = true;
            }
        }
    }

    public static void Pop()
    {
        if (_count <= 0) return;

        _count--;

        if (_count == 0)
        {
            Time.timeScale = (_savedTimeScale <= 0f) ? 1f : _savedTimeScale;

            if (_audioTouchedByUs)
            {
                AudioListener.pause = _savedAudioPause;
                _audioTouchedByUs = false;
            }
        }
    }

    public static void ForceClear()
    {
        _count = 0;
        Time.timeScale = 1f;

        if (_audioTouchedByUs)
        {
            AudioListener.pause = _savedAudioPause;
            _audioTouchedByUs = false;
        }
    }
}
