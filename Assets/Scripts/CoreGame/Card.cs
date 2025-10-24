using Unity.Netcode;
using UnityEngine;

public class Card : NetworkBehaviour
{
    [Header("Card Details")]
    public string suit;
    public string rank;

    [Header("Card Visuals")]
    public Sprite frontSprite;
    public Sprite backSprite;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void ShowFront()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }
        }
        spriteRenderer.sprite = frontSprite;
    }

    public void ShowBack()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }
        }
        spriteRenderer.sprite = backSprite;
    }
}