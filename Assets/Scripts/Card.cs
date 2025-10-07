using Unity.Netcode;

public enum CardSuit { Hearts, Diamonds, Clubs, Spades }
public enum CardRank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

[System.Serializable]
public struct Card : INetworkSerializable
{
    public CardSuit Suit;
    public CardRank Rank;
    public int SpriteSheetIndex; // 0-3 for the 4 suits
    public int SpriteIndexInSheet; // 0-12 for the 13 ranks

    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
        SpriteSheetIndex = (int)suit;
        SpriteIndexInSheet = (int)rank - 1; // Ace=0, King=12
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int suitInt = (int)Suit;
        int rankInt = (int)Rank;
        serializer.SerializeValue(ref suitInt);
        serializer.SerializeValue(ref rankInt);

        Suit = (CardSuit)suitInt;
        Rank = (CardRank)rankInt;

        // Recalculate sprite indices (no need to network them!)
        if (serializer.IsReader) // Only when receiving data
        {
            SpriteSheetIndex = (int)Suit;
            SpriteIndexInSheet = (int)Rank - 1;
        }
    }
}
