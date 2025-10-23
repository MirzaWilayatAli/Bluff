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

    private void Awake()
    {
        // Assign immediately when the object is created (runs before Start or RPCs)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"{name}: SpriteRenderer missing on Awake!");
        }
    }

    private void Start()
    {
        // Start might be delayed or skipped on network spawn; so safe to call again
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void ShowFront()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"{name}: ShowFront() failed — no SpriteRenderer found!");
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
                Debug.LogError($"{name}: ShowBack() failed — no SpriteRenderer found!");
                return;
            }
        }

        spriteRenderer.sprite = backSprite;
    }
}