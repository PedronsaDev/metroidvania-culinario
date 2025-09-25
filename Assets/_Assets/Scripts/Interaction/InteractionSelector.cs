using System.Collections.Generic;
using UnityEngine;
public static class InteractionSelector
{
    public static IInteractable SelectBest(IReadOnlyList<IInteractable> candidates, Vector2 origin)
    {
        IInteractable best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (!c.IsEnabled) continue;
            float dist = (c.WorldPosition - origin).sqrMagnitude;
            float score = c.Priority * 1000f - dist;
            if (score > bestScore)
            {
                bestScore = score;
                best = c;
            }
        }
        return best;
    }
}
