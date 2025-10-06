using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CameraFollowObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _flipRotationTime;

    private PlayerMovement _playerMovement;
    private bool _isFacingRight;
    private Coroutine _turnCoroutine;


    private void Awake()
    {
        _playerMovement = GetComponentInParent<PlayerMovement>();

        _isFacingRight = _playerMovement.FacingRight;
    }

    private void OnEnable()
    {
        if (_playerMovement)
            _playerMovement.Flipped += CallTurn;
    }

    private void OnDestroy()
    {
        if (_playerMovement)
            _playerMovement.Flipped -= CallTurn;
    }

    private void CallTurn() => transform.DORotate(new Vector3(0f, DetermineEndRotation(), 0f), _flipRotationTime).SetEase(Ease.InOutSine);


    private float DetermineEndRotation()
    {
        _isFacingRight = !_isFacingRight;

        if (_isFacingRight)
            return 0f;

        return 180f;
    }
}
