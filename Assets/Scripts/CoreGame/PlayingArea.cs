using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayingArea : NetworkBehaviour
{
    public static PlayingArea Instance;
    public List<Card> cardsPlayedList = new List<Card>();
    [SerializeField] private Collider2D playingAreaCollider;
    private List<TurnEntry> turnHistory = new List<TurnEntry>();

    private void Awake()
    {
        Instance = this;
        if (playingAreaCollider == null) 
        { 
            playingAreaCollider = GetComponent<Collider2D>(); 
        }
    }
    
    // Represents the cards played by a single client during one turn.
    [Serializable]
    private class TurnEntry
    {
        public ulong clientId;
        public List<Card> cards = new List<Card>();
        public DateTime timestamp = DateTime.UtcNow;

        public TurnEntry(ulong clientId)
        {
            this.clientId = clientId;
            this.cards = new List<Card>();
            this.timestamp = DateTime.UtcNow;
        }
    }

    public void AddCardToArea(Card card, ulong clientId)
    {
        if (card == null)
        {
            return;
        }
        Collider2D cardCollider = card.GetComponent<Collider2D>();
        if (!playingAreaCollider.bounds.Intersects(cardCollider.bounds))
        {
            return;
        }

        if (!cardsPlayedList.Contains(card))
        {
            cardsPlayedList.Add(card);
        }

        TurnEntry lastEntry;

        if (turnHistory.Count > 0)
        {
            lastEntry = turnHistory[turnHistory.Count - 1];
        }
        else
        {
            lastEntry = null;
        }

        if (lastEntry != null && lastEntry.clientId == clientId)
        {
            if (!lastEntry.cards.Contains(card))
                lastEntry.cards.Add(card);
        }
        else
        {
            TurnEntry newEntry = new TurnEntry(clientId);
            newEntry.cards.Add(card);
            turnHistory.Add(newEntry);
        }
    }
    
    public List<Card> GetLastTurnCardsForClient(ulong clientId)
    {
        for (int i = turnHistory.Count - 1; i >= 0; i--)
        {
            if (turnHistory[i].clientId == clientId)
            {
                return new List<Card>(turnHistory[i].cards);
            }
        }

        return new List<Card>();
    }
}
