using System.Numerics;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.GLTF;
using NiziKit.Inputs;
using NiziKit.Physics;

namespace DZForestDemo.GameObjects;

public enum DriveType { FrontWheelDrive, RearWheelDrive, AllWheelDrive }

public class RaceCar() : GameObject("RaceCar")
{
    private RigidbodyComponent? _rigidbody;
    private readonly WheelColliderComponent[] _wheels = new WheelColliderComponent[4];
    private readonly GameObject[] _wheelObjects = new GameObject[4];
    private readonly GameObject[] _visualWheels = new GameObject[4];
    private float _wheelSpinAngle;
    private float _currentSteerAngle;

    public float CarLength { get; set; } = 4.5f;
    public float CarWidth { get; set; } = 2f;
    public float CarHeight { get; set; } = 1.2f;
    public float WheelRadius { get; set; } = 0.4f;
    public float TrackWidth { get; set; } = 1.5f;
    public float WheelBase { get; set; } = 2.4f;
    public float ModelScale { get; set; } = 0.01f;

    public float Mass { get; set; } = 600f;
    public float MaxMotorTorque { get; set; } = 900f;
    public float MaxBrakeTorque { get; set; } = 1500f;
    public float MaxSteerAngle { get; set; } = MathF.PI * 0.27f;
    public float SteerSpeed { get; set; } = 8f;
    public DriveType DriveType { get; set; } = DriveType.AllWheelDrive;

    public string CarModelPath { get; set; } = "Racing/SM_Veh_Sports_01.glb";
    public string WheelModelPath { get; set; } = "Racing/SM_Veh_Attach_Wheel_01.glb";
    public string TexturePath { get; set; } = "Racing/PolygonStreetRacer_Texture_01_A.png";

    public float SpeedKmh => _rigidbody?.IsRegistered == true ? GetVelocity().Length() * 3.6f : 0f;
    public float ForwardSpeed => GetForwardSpeed();

    public override void Begin()
    {
        SetupCarBody();
        SetupWheelColliders();
        SetupVisuals();
    }

    private void SetupCarBody()
    {
        _rigidbody = RigidbodyComponent.Dynamic(
            PhysicsShape.Box(CarWidth, CarHeight * 0.6f, CarLength),
            Mass);

        AddComponent(_rigidbody);
    }

    private void SetupWheelColliders()
    {
        var wheelPositions = new[]
        {
            new Vector3(-TrackWidth / 2f, 0, WheelBase / 2f),
            new Vector3(TrackWidth / 2f, 0, WheelBase / 2f),
            new Vector3(-TrackWidth / 2f, 0, -WheelBase / 2f),
            new Vector3(TrackWidth / 2f, 0, -WheelBase / 2f)
        };

        for (var i = 0; i < 4; i++)
        {
            var wheelGo = new GameObject($"WheelCollider_{i}")
            {
                LocalPosition = wheelPositions[i]
            };
            AddChild(wheelGo);

            var wheel = WheelColliderComponent.Create(WheelRadius, 0.25f, 8f);
            wheel.Suspension = new SuspensionSpring
            {
                Frequency = 5f,
                Damping = 0.7f,
                RestLength = 0.3f
            };

            wheelGo.AddComponent(wheel);

            _wheelObjects[i] = wheelGo;
            _wheels[i] = wheel;
        }
    }

    private void SetupVisuals()
    {
        var carTexture = Assets.LoadTexture(TexturePath);
        var carMaterial = new SyntyCarMaterial("CarMaterial", carTexture);
        Assets.RegisterMaterial(carMaterial);

        var carModel = GltfModel.Load(CarModelPath);

        var body = new GameObject("CarBody")
        {
            LocalPosition = new Vector3(0, CarHeight * 0.3f, 0),
            LocalScale = new Vector3(ModelScale, ModelScale, ModelScale),
            LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI)
        };

        if (carModel.Meshes.Count > 0)
        {
            body.AddComponent(new MeshComponent { Mesh = carModel.Meshes[0] });
            body.AddComponent(new MaterialComponent { Material = carMaterial });
        }
        AddChild(body);

        var wheelModel = GltfModel.Load(WheelModelPath);

        if (wheelModel.Meshes.Count > 0)
        {
            var wheelMesh = wheelModel.Meshes[0];
            var wheelPositions = new[]
            {
                new Vector3(-TrackWidth / 2f, WheelRadius, WheelBase / 2f),
                new Vector3(TrackWidth / 2f, WheelRadius, WheelBase / 2f),
                new Vector3(-TrackWidth / 2f, WheelRadius, -WheelBase / 2f),
                new Vector3(TrackWidth / 2f, WheelRadius, -WheelBase / 2f)
            };

            for (var i = 0; i < 4; i++)
            {
                var wheel = new GameObject($"VisualWheel_{i}")
                {
                    LocalPosition = wheelPositions[i],
                    LocalScale = new Vector3(ModelScale, ModelScale, ModelScale)
                };

                var isRightWheel = i % 2 == 1;
                if (isRightWheel)
                {
                    wheel.LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
                }

                wheel.AddComponent(new MeshComponent { Mesh = wheelMesh });
                wheel.AddComponent(new MaterialComponent { Material = carMaterial });
                AddChild(wheel);
                _visualWheels[i] = wheel;
            }
        }
    }

    public override void Update()
    {
        HandleInput();
        UpdateVisualWheels();
    }

    private void HandleInput()
    {
        var throttle = 0f;
        var steerInput = 0f;
        var brake = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Up))
        {
            throttle = 1f;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Down))
        {
            throttle = -1f;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Left))
        {
            steerInput = -1f;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.Right))
        {
            steerInput = 1f;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            brake = 1f;
        }

        var targetSteerAngle = steerInput * MaxSteerAngle;
        var steerDiff = targetSteerAngle - _currentSteerAngle;
        var maxChange = SteerSpeed * Time.DeltaTime;
        _currentSteerAngle += MathF.Min(maxChange, MathF.Max(-maxChange, steerDiff));
        _currentSteerAngle = MathF.Min(MaxSteerAngle, MathF.Max(-MaxSteerAngle, _currentSteerAngle));

        for (var i = 0; i < 4; i++)
        {
            var wheel = _wheels[i];

            var isFrontWheel = i < 2;
            var isRearWheel = i >= 2;

            wheel.SteerAngle = isFrontWheel ? _currentSteerAngle : 0f;

            if (brake > 0.1f)
            {
                wheel.MotorTorque = 0f;
                wheel.BrakeTorque = MaxBrakeTorque * brake;
            }
            else
            {
                wheel.BrakeTorque = 0f;

                var shouldDrive = DriveType switch
                {
                    DriveType.FrontWheelDrive => isFrontWheel,
                    DriveType.RearWheelDrive => isRearWheel,
                    DriveType.AllWheelDrive => true,
                    _ => isRearWheel
                };

                wheel.MotorTorque = shouldDrive ? throttle * MaxMotorTorque : 0f;
            }
        }
    }

    private void UpdateVisualWheels()
    {
        var forwardSpeed = GetForwardSpeed();
        _wheelSpinAngle += forwardSpeed * Time.DeltaTime * 2.5f;

        var inverseCarRotation = Quaternion.Inverse(LocalRotation);

        for (var i = 0; i < 4; i++)
        {
            var wheel = _wheels[i];
            var visualWheel = _visualWheels[i];

            var (worldPos, _) = wheel.GetWorldPose();
            var localPos = Vector3.Transform(worldPos - LocalPosition, inverseCarRotation);
            visualWheel.LocalPosition = localPos;

            var isFrontWheel = i < 2;
            var isRightWheel = i % 2 == 1;

            var baseRotation = isRightWheel
                ? Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI)
                : Quaternion.Identity;

            var spinRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, _wheelSpinAngle);

            if (isFrontWheel)
            {
                var steerRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _currentSteerAngle);
                visualWheel.LocalRotation = steerRotation * baseRotation * spinRotation;
            }
            else
            {
                visualWheel.LocalRotation = baseRotation * spinRotation;
            }
        }
    }

    private Vector3 GetVelocity()
    {
        return _rigidbody?.IsRegistered != true ? Vector3.Zero : Physics.GetVelocity(Id);
    }

    private float GetForwardSpeed()
    {
        var velocity = GetVelocity();
        var forward = Vector3.Transform(Vector3.UnitZ, LocalRotation);
        return Vector3.Dot(velocity, forward);
    }

    public void ResetPosition(Vector3 position, Quaternion rotation)
    {
        LocalPosition = position;
        LocalRotation = rotation;
    }
}

public class SyntyCarMaterial : Material
{
    public SyntyCarMaterial(string name, Texture2d texture)
    {
        Name = name;
        Albedo = texture;
        GpuShader = Assets.GetShader("Builtin/Shaders/Default");
    }
}
