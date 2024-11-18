using System;
using UnityEngine;


public static class Vector2IntExtensions
{
    public static Vector3 AsVector3(Vector2Int v)
    {
        return new Vector3(v.x, v.y, 0f);
    }

    public static bool IsSameLine(Vector2Int p1, Vector2Int p2, Vector2Int p3)
    {
        return (p2.x - p1.x) * (p3.y - p1.y) == (p2.y - p1.y) * (p3.x - p1.x);
    }

    public static bool IsCloser(Vector2Int p1, Vector2Int p2, Vector2Int p3)
    {
        return Vector2Int.Distance(p1, p2) < Vector2Int.Distance(p1, p3);
    }
}

