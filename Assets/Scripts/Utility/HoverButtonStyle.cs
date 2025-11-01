using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class HoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Hover Settings")]
    public Color hoverColor;
    public float hoverScale = 1.2f;
    public float animationDuration = 0.25f;
    
    public TextMeshProUGUI buttonText;
    public Image buttonImage;
    public Sprite hoverSprite;
    
    private Vector3 originalScale;
    private Color originalColor = Color.white;
    private Sprite originalSprite;
    
    private void Start()
    {
        originalScale = transform.localScale;
        
        if (buttonImage != null)
        {
            originalSprite = buttonImage.sprite;
        }
        
        // Ensure text starts white
        if (buttonText != null)
        {
            buttonText.color = originalColor;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateTo(hoverColor, originalScale * hoverScale, hoverSprite));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateTo(originalColor, originalScale, originalSprite));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Immediately reset to normal when pressed
        StopAllCoroutines();
        buttonText.color = originalColor;
        transform.localScale = originalScale;
        if (buttonImage != null && originalSprite != null)
        {
            buttonImage.sprite = originalSprite;
        }
    }
    
    public IEnumerator AnimateTo(Color targetColor, Vector3 targetScale, Sprite targetSprite)
    {
        Color startColor = buttonText.color;
        Vector3 startScale = transform.localScale;
        float time = 0f;
        
        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            
            // Smooth interpolation
            buttonText.color = Color.Lerp(startColor, targetColor, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            yield return null;
        }
        
        // Ensure final values
        buttonText.color = targetColor;
        transform.localScale = targetScale;
        
        // Set sprite after scale animation completes
        if (buttonImage != null && targetSprite != null)
        {
            buttonImage.sprite = targetSprite;
        }
    }
}