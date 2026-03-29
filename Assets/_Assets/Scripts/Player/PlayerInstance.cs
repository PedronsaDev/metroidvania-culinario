using UnityEngine;

public class PlayerInstance : MonoBehaviour
{
    public static PlayerInstance Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);
    }
}
