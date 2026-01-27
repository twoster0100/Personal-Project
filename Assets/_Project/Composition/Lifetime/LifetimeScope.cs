using UnityEngine;
using MyGame.Application;          
using MyGame.Application.Lifetime; 

public sealed class LifetimeScope : MonoBehaviour
{
    public AppLifetime Lifetime { get; private set; }

    private void Awake()
    {
        Lifetime = new AppLifetime();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        App.RegisterDisposable(Lifetime);
    }

    private void OnDestroy()
    {
        Lifetime?.Dispose();
        Lifetime = null;
    }
}
