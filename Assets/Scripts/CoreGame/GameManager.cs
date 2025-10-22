using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public Button playButton;
    public Button increaseBetButton;
    public Button decreaseBetButton;
    public float bet = 500;
    public TextMeshProUGUI displayBet;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        increaseBetButton.onClick.AddListener(OnIncreaseBetButtonClicked);
        decreaseBetButton.onClick.AddListener(OnDecreaseBetButtonClicked);
    }

    public void OnIncreaseBetButtonClicked()
    {
        bet = bet + 500;
        displayBet.text = bet.ToString();
    }

    public void OnDecreaseBetButtonClicked()
    {
        bet = bet - 500;
        if (bet < 0)
        {
            bet = 0;
        }
        displayBet.text = bet.ToString();
    }
}
