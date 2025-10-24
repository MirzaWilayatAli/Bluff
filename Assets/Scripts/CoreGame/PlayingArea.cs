using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayingArea : NetworkBehaviour
{
    public static PlayingArea Instance;
    public List<Card> cardsPlayedList = new List<Card>();

    private void Awake()
    {
        Instance = this;
    }
    
    public void AddCardToArea(Card card)
    {
        if (!cardsPlayedList.Contains(card))
        {
            cardsPlayedList.Add(card);
        }
    }
}