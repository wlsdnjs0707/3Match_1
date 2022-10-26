using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance { get; private set; }

    private int _score;

    public int Score
    {
        get => _score;

        set
        {
            if (_score == value) return;
            _score = value;
            scoreText.SetText($"SCORE : {_score}");
        }
    }

    [SerializeField] private TextMeshProUGUI scoreText;

    private void Awake() => Instance = this;
}
