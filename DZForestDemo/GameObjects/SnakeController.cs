using System.Numerics;
using DenOfIz;
using DZForestDemo.Scenes;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Inputs;
using NiziKit.Physics;

namespace DZForestDemo.GameObjects;

public class SnakeController : IComponent
{
    public GameObject? Owner { get; set; }

    private readonly List<SnakeSegment> _segments = [];
    private readonly Queue<Vector3> _inputQueue = new();
    private const int MaxQueuedInputs = 2;

    private Vector3 _direction = new(1, 0, 0);
    private float _moveTimer;

    private bool _isPaused;
    private bool _isGameOver;
    private int _score;

    public float BaseMoveInterval { get; set; } = 0.05f;
    public float MinMoveInterval { get; set; } = 0.00005f;
    public float SpeedIncreasePerFood { get; set; } = 0.0005f;

    [JsonProperty("segmentSize")]
    public float SegmentSize { get; set; } = 1f;

    [JsonProperty("arenaSize")]
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

    private Scene? Scene => Owner?.Scene;

    public void Begin()
    {
        if (Owner == null)
        {
            return;
        }

        EnsureAssetsCreated();

        var headSegment = CreateSegment("Head", true, HeadMesh!, HeadMaterial!);
        headSegment.SetPositionImmediate(Vector3.Zero);
        _segments.Add(headSegment);

        for (var i = 1; i <= 3; i++)
        {
            AddBodySegment(new Vector3(-i * SegmentSize, 0, 0));
        }
    }

    private void EnsureAssetsCreated()
    {
        var cubeMesh = Assets.CreateBox(SegmentSize, SegmentSize, SegmentSize);
        HeadMesh ??= cubeMesh;
        BodyMesh ??= cubeMesh;

        if (HeadMaterial == null)
        {
            var existing = Assets.GetMaterial("SnakeHead");
            if (existing != null)
            {
                HeadMaterial = existing;
            }
            else
            {
                HeadMaterial = new AnimatedSnakeMaterial("SnakeHead", 50, 200, 50);
                Assets.RegisterMaterial(HeadMaterial);
            }
        }

        if (BodyMaterial == null)
        {
            var existing = Assets.GetMaterial("SnakeBody");
            if (existing != null)
            {
                BodyMaterial = existing;
            }
            else
            {
                BodyMaterial = new AnimatedSnakeMaterial("SnakeBody", 30, 150, 30);
                Assets.RegisterMaterial(BodyMaterial);
            }
        }
    }

    private SnakeSegment CreateSegment(string name, bool isHead, Mesh mesh, Material material)
    {
        var go = new GameObject(name);

        var segment = new SnakeSegment { IsHead = isHead };
        go.AddComponent(segment);
        go.AddComponent(new MeshComponent { Mesh = mesh });
        go.AddComponent(new MaterialComponent { Material = material });
        go.AddComponent(RigidbodyComponent.Kinematic(PhysicsShape.Cube(1f)));

        Owner?.AddChild(go);
        return segment;
    }

    private void AddBodySegment(Vector3 position)
    {
        if (Owner == null || BodyMesh == null || BodyMaterial == null)
        {
            return;
        }

        var segment = CreateSegment($"Body_{_segments.Count}", false, BodyMesh, BodyMaterial);
        segment.SetPositionImmediate(position);
        _segments.Add(segment);
    }

    public void Update()
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

        var headPos = _segments[0].Owner?.LocalPosition ?? Vector3.Zero;
        foreach (var foodComp in Scene.FindComponents<Food>())
        {
            if (foodComp.Owner == null)
            {
                continue;
            }

            if (Vector3.Distance(headPos, foodComp.Owner.LocalPosition) < SegmentSize)
            {
                EatFood();
                OnAteFood?.Invoke(foodComp.Owner.LocalPosition);
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
    }

    private void Move()
    {
        if (_segments.Count == 0)
        {
            return;
        }

        var head = _segments[0];
        var headPos = head.Owner?.LocalPosition ?? Vector3.Zero;
        var newHeadPos = headPos + _direction * SegmentSize;

        if (MathF.Abs(newHeadPos.X) >= ArenaSize || MathF.Abs(newHeadPos.Z) >= ArenaSize)
        {
            GameOver("Hit wall!");
            return;
        }

        for (var i = 2; i < _segments.Count; i++)
        {
            var segPos = _segments[i].Owner?.LocalPosition ?? Vector3.Zero;
            if (Vector3.Distance(newHeadPos, segPos) < SegmentSize * 0.5f)
            {
                GameOver("Hit yourself!");
                return;
            }
        }

        for (var i = _segments.Count - 1; i > 0; i--)
        {
            var prevPos = _segments[i - 1].Owner?.LocalPosition ?? Vector3.Zero;
            _segments[i].SetTargetPosition(prevPos);
        }

        head.SetTargetPosition(newHeadPos);
    }

    private void EatFood()
    {
        _score++;
        var tailPos = _segments[^1].Owner?.LocalPosition ?? Vector3.Zero;
        AddBodySegment(tailPos);
    }

    private void GameOver(string reason)
    {
        _isGameOver = true;
        OnGameOver?.Invoke(reason);
    }
}
