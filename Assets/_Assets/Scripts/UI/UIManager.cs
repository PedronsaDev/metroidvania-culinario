using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-50)]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private readonly List<BaseUIWindow> _openStack = new();
    public bool IsGameplayBlocked
    {
        get
        {
            for (int i = 0; i < _openStack.Count; i++)
                if (_openStack[i] && _openStack[i].BlocksGameplay)
                    return true;
            return false;
        }
    }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    internal void NotifyWindowOpened(BaseUIWindow w)
    {
        if (!_openStack.Contains(w))
            _openStack.Add(w);
    }

    internal void NotifyWindowClosed(BaseUIWindow w)
    {
        _openStack.Remove(w);
    }

    public void CloseTop()
    {
        for (int i = _openStack.Count - 1; i >= 0; i--)
        {
            var w = _openStack[i];
            if (!w) { _openStack.RemoveAt(i); continue; }
            w.Hide();
            return;
        }
    }

    public bool HasAnyOpen => _openStack.Count > 0;

    public T FindWindow<T>() where T : BaseUIWindow => FindFirstObjectByType<T>();

    public BaseUIWindow FindById(string id)
    {
        var all = FindObjectsByType<BaseUIWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (BaseUIWindow window in all)
            if (window.WindowId == id)
                return window;

        return null;
    }
}