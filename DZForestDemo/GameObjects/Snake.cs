using System.Numerics;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Core;
using NiziKit.Inputs;

namespace DZForestDemo.GameObjects;

public class Snake : GameObject
{
    private readonly List<SnakeSegment> _segments = [];
    private readonly Queue<Vector3> _inputQueue = new();
    private const int MaxQueuedInputs = 2;

    private Vector3 _direction = new(1, 0, 0);
    private float _moveTimer;

    private bool _isPaused;
    private bool _isGameOver;
    private int _score;

    public float BaseMoveInterval { get; set; } = 0.15f;
    public float MinMoveInterval { get; set; } = 0.05f;
    public float SpeedIncreasePerFood { get; set; } = 0.005f;
    public float SegmentSize { get; set; } = 1f;
    public int ArenaSize { get; set; } = 15;

    private float CurrentMoveInterval => MathF.Max(MinMoveInterval, BaseMoveInterval - _score * SpeedIncreasePerFood);

    public Mesh? HeadMesh { get; set; }
    public Material? HeadMaterial { get; set; }
    public Mesh? BodyMesh { get; set; }
    public Material? BodyMaterial { get; set; }

    public event Action<Vector3>? OnAteFood;
    public event Action<string>? OnGameOver;

    public bool IsGameOver => _isGameOver;
    public bool IsPaused => _isPaused;
    public int Score => _score;

    public Snake() : base("Snake")
    {
    }

    public override void Begin()
    {
        if (HeadMesh == null || HeadMaterial == null || BodyMesh == null || BodyMaterial == null)
        {
            throw new InvalidOperationException("Snake meshes and materials must be set before Begin");
        }

        var head = new SnakeSegment("Head", true);
        head.SetMeshAndMaterial(HeadMesh, HeadMaterial);
        head.SetPositionImmediate(Vector3.Zero);
        AddChild(head);
        _segments.Add(head);

        for (var i = 1; i <= 3; i++)
        {
            AddBodySegment(new Vector3(-i * SegmentSize, 0, 0));
        }
    }

    private void AddBodySegment(Vector3 position)
    {
        if (BodyMesh == null || BodyMaterial == null)
        {
            return;
        }

        var segment = new SnakeSegment($"Body_{_segments.Count}", false);
        segment.SetMeshAndMaterial(BodyMesh, BodyMaterial);
        segment.SetPositionImmediate(position);
        AddChild(segment);
        _segments.Add(segment);
    }

    public override void Update()
    {
        HandleInput();

        if (_isGameOver || _isPaused)
        {
            return;
        }

        _moveTimer += Time.DeltaTime;
        if (_moveTimer >= CurrentMoveInterval)
        {
            _moveTimer = 0;
            if (_inputQueue.Count > 0)
            {
                _direction = _inputQueue.Dequeue();
            }
            Move();
        }

        CheckFoodCollisions();
    }

    private void CheckFoodCollisions()
    {
        if (_segments.Count == 0 || _isGameOver || Scene == null)
        {
            return;
        }

        var headPos = _segments[0].LocalPosition;
        var foods = Scene.GetObjectsOfType<Food>();
        foreach (var food in foods)
        {
            if (Vector3.Distance(headPos, food.LocalPosition) < SegmentSize)
            {
                EatFood();
                OnAteFood?.Invoke(food.LocalPosition);
                break;
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }

        if (_isGameOver || _isPaused)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Up))
        {
            TryQueueDirection(new Vector3(0, 0, 1));
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Down))
        {
            TryQueueDirection(new Vector3(0, 0, -1));
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Left))
        {
            TryQueueDirection(new Vector3(-1, 0, 0));
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Right))
        {
            TryQueueDirection(new Vector3(1, 0, 0));
        }
    }

    private void TryQueueDirection(Vector3 newDirection)
    {
        var lastDirection = _inputQueue.Count > 0 ? _inputQueue.ToArray()[^1] : _direction;

        if (Vector3.Dot(newDirection, lastDirection) >= 0 && newDirection != lastDirection)
        {
            if (_inputQueue.Count < MaxQueuedInputs)
            {
                _inputQueue.Enqueue(newDirection);
            }
        }
    }

    private void TogglePause()
    {
        if (_isGameOver)
        {
            return;
        }

        _isPaused = !_isPaused;
        Console.WriteLine(_isPaused ? "PAUSED" : "RESUMED");
    }

    private void Move()
    {
        if (_segments.Count == 0)
        {
            return;
        }

        var head = _segments[0];
        var newHeadPos = head.LocalPosition + _direction * SegmentSize;

        if (MathF.Abs(newHeadPos.X) >= ArenaSize || MathF.Abs(newHeadPos.Z) >= ArenaSize)
        {
            GameOver("Hit wall!");
            return;
        }

        for (var i = 2; i < _segments.Count; i++)
        {
            if (Vector3.Distance(newHeadPos, _segments[i].LocalPosition) < SegmentSize * 0.5f)
            {
                GameOver("Hit yourself!");
                return;
            }
        }

        for (var i = _segments.Count - 1; i > 0; i--)
        {
            _segments[i].SetTargetPosition(_segments[i - 1].LocalPosition);
        }

        head.SetTargetPosition(newHeadPos);
    }

    private void EatFood()
    {
        _score++;
        Console.WriteLine($"Score: {_score}");

        var tailPos = _segments[^1].LocalPosition;
        AddBodySegment(tailPos);
    }

    private void GameOver(string reason)
    {
        _isGameOver = true;
        Console.WriteLine($"GAME OVER! {reason}");
        Console.WriteLine($"Final Score: {_score}");
        Console.WriteLine("Press R to restart");
        OnGameOver?.Invoke(reason);
    }
}
