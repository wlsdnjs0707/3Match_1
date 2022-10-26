using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public sealed class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip shuffleSound;
    [SerializeField] private AudioSource _audioSource;

    public GameObject shuffleText;

    public Button restartButton;

    public GameObject gameoverUI;

    public GameObject timeBar;

    public Row[] rows;

    public Tile[,] Tiles { get; private set; }

    private int[] numbers = new int[25];

    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);

    private readonly List<Tile> _selection = new List<Tile>();

    private const float TweenDuration = 0.25f;

    public bool canSwap = false;

    private void Awake() => Instance = this;

    private void Start()
    {
        canSwap = true;

        shuffleText.SetActive(false);

        restartButton.onClick.AddListener(restart);

        Tiles = new Tile[rows.Max(rows => rows.tiles.Length), rows.Length];

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var tile = rows[y].tiles[x];

                tile.x = x;
                tile.y = y;

                tile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];

                Tiles[x, y] = tile;
            }
        }
    }

    private async void Reset()
    {
        timeBar.GetComponent<Timer>().timeLeft = timeBar.GetComponent<Timer>().maxTime;

        var connected = new List<Tile> { };

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                connected.Add(Tiles[x, y]);
            }
        }

        // 삭제
        var deflateSequence = DOTween.Sequence();

        foreach (var connectedTile in connected)
        {
            deflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
        }

        await deflateSequence.Play().AsyncWaitForCompletion();

        // 재생성
        var inflateSequence = DOTween.Sequence();

        foreach (var connectedTile in connected)
        {
            connectedTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];

            inflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await inflateSequence.Play().AsyncWaitForCompletion();

    }

    void showShuffleText()
    {
        shuffleText.SetActive(false);
    }

    public async void Select(Tile tile)
    {
        if (!_selection.Contains(tile))
        {
            // Already Selected One Tile
            if (_selection.Count > 0)
            {
                // If Select Adjacent Tile
                if(Array.IndexOf(_selection[0].Neighbours,tile) != -1)
                {
                    Image _image = tile.background;
                    _image.color = Color.red;
                    _selection.Add(tile);
                }
                // If Select Wrong Tile (Not Adjacent)
                else
                {
                    Image originalImage = _selection[0].background;
                    originalImage.color = Color.white;
                    _selection.RemoveAt(0);
                }
            }
            // First Tile Select
            else
            {
                Image _image = tile.background;
                _image.color = Color.red;
                _selection.Add(tile);
            }
        }

        if (_selection.Count < 2) return;

        await Swap(_selection[0], _selection[1]);

        Image tile1Image = _selection[0].background;
        Image tile2Image = _selection[1].background;
        tile1Image.color = Color.white;
        tile2Image.color = Color.white;

        if (CanPop())
        {
            Pop();
        }
        else
        {
            await Swap(_selection[0], _selection[1]);
        }

        _selection.Clear();

    }

    public async Task Swap(Tile tile1, Tile tile2)
    {
        if (canSwap == true)
        {
            canSwap = false;

            var icon1 = tile1.icon;
            var icon2 = tile2.icon;

            var icon1Transform = icon1.transform;
            var icon2Transform = icon2.transform;

            var sequence = DOTween.Sequence();

            sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration)).Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));

            await sequence.Play().AsyncWaitForCompletion();

            icon1Transform.SetParent(tile2.transform);
            icon2Transform.SetParent(tile1.transform);

            tile1.icon = icon2;
            tile2.icon = icon1;

            var tile1Item = tile1.Item;

            tile1.Item = tile2.Item;
            tile2.Item = tile1Item;

            canSwap = true;
        }
    }

    private void fakeSwap(Tile tile1, Tile tile2)
    {
        var tile1Item = tile1.Item;

        tile1.Item = tile2.Item;
        tile2.Item = tile1Item;
    }

    // 더이상 게임 진행 불가능하면 True 반환
    private bool Check()
    {
        var tile = rows[0].tiles[0];

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                tile = rows[y].tiles[x];

                // 1. 아래랑 교환
                if ( y <= 3 )
                {
                    var down = rows[y + 1].tiles[x];
                    fakeSwap(tile, down);
                    if (CanPop() == true)
                    {
                        fakeSwap(tile, down);
                        return false;
                    }
                    else
                    {
                        fakeSwap(tile, down);
                    }
                }

                // 2. 오른쪽이랑 교환
                if (x <= 3)
                {
                    var right = rows[y].tiles[x + 1];
                    fakeSwap(tile, right);
                    if (CanPop() == true)
                    {
                        fakeSwap(tile, right);
                        return false;
                    }
                    else
                    {
                        fakeSwap(tile, right);
                    }
                }
            }
        }
        return true;
    }

    private bool CanPop()
    {
        for (var y=0; y<Height; y++)
        {
            for (var x=0; x<Width; x++)
            {
                if (Tiles[x, y].GetConnectedTiles().Skip(1).Count() >= 2) return true;
            }    
        }

        return false;
    }

    private async void Pop()
    {
        for (var y=0; y<Height; y++)
        {
            for(var x=0; x<Width; x++)
            {
                timeBar.GetComponent<Timer>().timeLeft = timeBar.GetComponent<Timer>().maxTime;

                var tile = Tiles[x, y];

                var connectedTiles = tile.GetConnectedTiles();

                if (connectedTiles.Skip(1).Count() < 2) continue;

                var deflateSequence = DOTween.Sequence();

                foreach(var connectedTile in connectedTiles)
                {
                    deflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
                }

                _audioSource.PlayOneShot(collectSound);

                ScoreCounter.Instance.Score += connectedTiles.Count;

                await deflateSequence.Play().AsyncWaitForCompletion();

                var inflateSequence = DOTween.Sequence();

                foreach(var connectedTile in connectedTiles)
                {
                    connectedTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];

                    inflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
                }

                await inflateSequence.Play().AsyncWaitForCompletion();

                x = 0;
                y = 0;
            }
        }

        // 진행 불가능시 타일 리셋
        if (Check()==true)
        {
            shuffleText.SetActive(true);
            Invoke("showShuffleText", 3f);
            _audioSource.PlayOneShot(shuffleSound);
            Reset();
        }
    }

    void restart()
    {
        Time.timeScale = 1f;
        ScoreCounter.Instance.Score = 0;
        gameoverUI.SetActive(false);
        timeBar.GetComponent<Timer>().timeLeft = timeBar.GetComponent<Timer>().maxTime;
        Reset();
    }
}
