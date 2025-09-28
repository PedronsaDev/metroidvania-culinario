using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[Serializable]
public class TooltipInfo
{
    public Sprite Icon;
    public string Header;
    [TextArea]
    public string Content;

    [ReorderableList]
    public List<TooltipElementInfo> Elements;
    [ReorderableList]
    public List<TooltipActionInfo> Actions;

    public bool HasIcon => Icon;
    public bool HasHeader => !string.IsNullOrEmpty(Header);
    public bool HasContent => !string.IsNullOrEmpty(Content);
    public bool HasElements => Elements.Count > 0;
    public bool HasActions => Actions.Count > 0;

    public bool IsEmpty => !HasIcon && !HasHeader && !HasContent && !HasElements && !HasActions;

    public TooltipInfo(Sprite icon, string header, string content, List<TooltipElementInfo> elements, List<TooltipActionInfo> actions)
    {
        Icon = icon;
        Header = header;
        Content = content;
        Elements = elements ?? new List<TooltipElementInfo>();
        Actions = actions ?? new List<TooltipActionInfo>();
    }
}

[Serializable]
public struct TooltipElementInfo
{
    public string Name;
    public string Value;
}

[Serializable]
public struct TooltipActionInfo
{
    public string Action;
    public Sprite Prompt;
}