using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayingArea : NetworkBehaviour
{
    public static PlayingArea Instance;
    public List<Card> cardsPlayedList = new List<Card>();
    public Dictionary<ulong, Card> clientCards = new Dictionary<ulong, Card>();

    private void Awake()
    {
        Instance = this;
    }
    
    public void AddCardToArea(Card card, ulong clientId)
    {
        if (!cardsPlayedList.Contains(card))
        {
            cardsPlayedList.Add(card);
            
            // Track which client played this card
            if (!clientCards.ContainsKey(clientId))
            {
                clientCards.Add(clientId, card);
            }
            else
            {
                // If client already played a card, replace it
                clientCards[clientId] = card;
            }
        }
    }
    
    // New method: Get cards played by a specific client
    public List<Card> GetCardsByClient(ulong clientId)
    {
        List<Card> clientCardsList = new List<Card>();
        
        foreach (var card in cardsPlayedList)
        {
            if (clientCards.ContainsKey(clientId) && clientCards[clientId] == card)
            {
                clientCardsList.Add(card);
            }
        }
        
        return clientCardsList;
    }
}