using System;
using System.Collections;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 _lastCheckpointPosition;

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

    private void OnEnable()
    {
        SceneManager.OnSceneLoaded += MovePlayer;
        PlayerInstance.Instance.GetComponent<PlayerHealth>().Death += ResetToCheckpoint;
    }

    private void OnDestroy()
    {
        SceneManager.OnSceneLoaded -= MovePlayer;
        PlayerInstance.Instance.GetComponent<PlayerHealth>().Death -= ResetToCheckpoint;
    }

    public void ResetToCheckpoint()
    {
        SceneManager.Instance.ReloadCurrentScene();
    }

    private void MovePlayer()
    {
        StartCoroutine(MovePlayerToCheckpoint());
    }

    private IEnumerator MovePlayerToCheckpoint()
    {
        PlayerInstance.Instance.transform.position = _lastCheckpointPosition;

        yield return null;

        PlayerInstance.Instance.GetComponent<PlayerHealth>().Reset();
    }

    public void SetCheckpoint(Transform checkpointTransform)
    {
        _lastCheckpointPosition = checkpointTransform.position;
    }
}
