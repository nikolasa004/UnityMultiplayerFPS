using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreboardText;
    [SerializeField, Range(0.1f, 2f)] private float refreshInterval = 0.25f;

    private readonly List<PlayerManager> sortedPlayers = new List<PlayerManager>();
    private readonly StringBuilder textBuilder = new StringBuilder(256);
    private float nextRefresh;

    private void Awake()
    {
        if (scoreboardText != null)
            scoreboardText.richText = false;
    }

    private void Update()
    {
        if (scoreboardText == null || Time.unscaledTime < nextRefresh)
            return;

        nextRefresh = Time.unscaledTime + refreshInterval;
        sortedPlayers.Clear();

        foreach (PlayerManager player in GameManager.GetPlayers())
        {
            if (player != null)
                sortedPlayers.Add(player);
        }

        sortedPlayers.Sort((a, b) =>
        {
            int compare = b.kills.CompareTo(a.kills);
            return compare != 0 ? compare : string.Compare(a.playerName, b.playerName, StringComparison.OrdinalIgnoreCase);
        });

        textBuilder.Clear();
        textBuilder.AppendLine("SCOREBOARD");
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            PlayerManager player = sortedPlayers[i];
            textBuilder.Append(i + 1).Append(". ")
                       .Append(player.playerName).Append("  -  ")
                       .Append(player.kills).AppendLine();
        }

        scoreboardText.text = textBuilder.ToString();
    }
}
