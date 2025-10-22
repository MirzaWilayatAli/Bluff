using UnityEngine;

public class Card : MonoBehaviour
{
    [Header("Card Details")]
    public string suit;
    public string rank;
    
    [Header("Card Visuals")]
    public Sprite frontSprite;
    public Sprite backSprite;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ShowBack();
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