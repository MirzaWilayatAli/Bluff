using UnityEngine;

public class Card : MonoBehaviour
{
    public string rank;
    
    // Card visuals
    public Sprite frontSprite;
    public Sprite backSprite;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ShowBack(); // Start with back shown by default
    }
    
    public void ShowFront()
    {
        spriteRenderer.sprite = frontSprite;
    }
    
    public void ShowBack()
    {
        spriteRenderer.sprite = backSprite;
    }
}