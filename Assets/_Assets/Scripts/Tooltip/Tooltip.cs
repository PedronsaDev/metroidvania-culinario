using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public CanvasGroup CanvasGroup;
    [SerializeField] private GameObject _headerParent;
    [SerializeField] private Image _headerIcon;
    [SerializeField] private TextMeshProUGUI _headerField;
    [SerializeField] private TextMeshProUGUI _contentField;
    [SerializeField] private Transform _elementsParent;
    [SerializeField] private GameObject _elementPrefab;
    [SerializeField] private Transform _actionsParent;
    [SerializeField] private GameObject _actionPrefab;
    [SerializeField] private LayoutElement _layoutElement;
    [SerializeField] private int _characterWrapLimit;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        CanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start() => CanvasGroup.alpha = 0;

    private void LateUpdate()
    {
        Vector2 mousePosition = Mouse.current.position.value;

        float pivotX = mousePosition.x/Screen.width;
        float pivotY = mousePosition.y/Screen.height;

        _rectTransform.pivot = new Vector2(pivotX < 0.50f ? -0.1f : 1.01f, pivotY < 0.75f ? 0f : 1f);
        _rectTransform.position = mousePosition;
    }

    public void SetInfo(TooltipInfo info)
    {
        if (info.IsEmpty)
        {
            Debug.Log("Tooltip info is empty");
            return;
        }

        ReturnInfo();

        if (info.HasHeader)
        {
            _headerParent.gameObject.SetActive(true);
            _headerField.text = info.Header;
        }
        else
        {
            _headerParent.gameObject.SetActive(false);
        }

        if (info.HasIcon)
        {
            _headerIcon.sprite = info.Icon;
            _headerIcon.gameObject.SetActive(true);
        }
        else
        {
            _headerIcon.gameObject.SetActive(false);
        }

        _contentField.text = info.Content;

        int headerLength = _headerField.text.Length;
        int contentLength = _contentField.text.Length;

        if (info.HasElements)
        {
            _elementsParent.gameObject.SetActive(true);
            CreateElements(info.Elements);
        }
        else
        {
            _elementsParent.gameObject.SetActive(false);
        }

        if (info.HasContent)
        {
            _actionsParent.gameObject.SetActive(true);
            CreateActions(info.Actions);
        }
        else
        {
            _actionsParent.gameObject.SetActive(false);
        }

        _layoutElement.enabled = (headerLength > _characterWrapLimit || contentLength > _characterWrapLimit);
    }

    private void CreateElements(List<TooltipElementInfo> infos)
    {
        foreach (TooltipElementInfo info in infos)
        {
            TooltipElement element = ObjectPoolManager.SpawnGameObject(_elementPrefab, _elementsParent, Quaternion.identity).GetComponent<TooltipElement>();
            element.SetElementInfo(info);
        }
    }

    private void CreateActions(List<TooltipActionInfo> infos)
    {
        foreach (TooltipActionInfo info in infos)
        {
            TooltipAction action = ObjectPoolManager.SpawnGameObject(_actionPrefab, _actionsParent, Quaternion.identity).GetComponent<TooltipAction>();
            action.SetActionInfo(info);
        }
    }

    private void ReturnInfo()
    {
        for (int i = _elementsParent.childCount - 1; i >= 0; i--)
            ObjectPoolManager.ReturnObjectToPool(_elementsParent.GetChild(i).gameObject);

        for (int i = _actionsParent.childCount - 1; i >= 0; i--)
            ObjectPoolManager.ReturnObjectToPool(_actionsParent.GetChild(i).gameObject);
    }
}
