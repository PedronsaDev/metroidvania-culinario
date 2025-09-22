using System;
using _Assets.Scripts.Drops;
using UnityEngine;

public class Tester : MonoBehaviour
{
    [SerializeField] private Dropper _dropper;

    private void Start()
    {
        _dropper = GetComponent<Dropper>();
    }
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            _dropper.TryDrop();
    }
}
