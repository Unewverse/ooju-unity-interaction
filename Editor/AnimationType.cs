using UnityEngine;

namespace OojuInteractionPlugin
{
    public enum AnimationType
    {
        None,
        Hover,
        Wobble,
        Spin,
        Shake,
        Bounce,
        Scale
        // Add more as needed
    }

    public enum AnimationCategory
    {
        Independent,
        Relational
    }

    public enum RelationalAnimationType
    {
        Orbit,
        LookAt,
        Follow,
        MoveAlongPath,
        SnapToObject
    }
} 