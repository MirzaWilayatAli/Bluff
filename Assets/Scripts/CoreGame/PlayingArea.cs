using System;
using UnityEngine;

public class PlayingArea : MonoBehaviour
{
    public static PlayingArea Instance;

    private void Awake()
    {
        Instance = this;
    }
    
    public bool IsPointInArea(Vector3 point)
    {
        Collider2D collider = GetComponent<Collider2D>();
        return collider.OverlapPoint(new Vector2(point.x, point.y));
    }
}
