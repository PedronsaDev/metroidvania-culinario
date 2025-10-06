using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Controls")]

    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        if (!Instance)
            Instance = this;
    }

    public void ShakeCamera(float intensity)
    {
        if (_impulseSource)
        {
            _impulseSource.GenerateImpulse(intensity);
        }
    }
}
