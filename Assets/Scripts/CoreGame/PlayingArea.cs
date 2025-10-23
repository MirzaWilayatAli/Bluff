using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayingArea : MonoBehaviour
{
    public static PlayingArea Instance;
    public List<Card> cardsInArea = new List<Card>();

    private void Awake()
    {
        Instance = this;
    }
    
    public void AddCardToArea(Card card)
    {
        if (!cardsInArea.Contains(card))
        {
            cardsInArea.Add(card);
        }
    }
    
    public void ClearArea()
    {
        cardsInArea.Clear();
    }
}