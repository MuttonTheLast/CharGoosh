using System.Numerics;

namespace CharGoosh.Math;

public struct Transform
{
    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    public readonly Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);
    public readonly Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);
    public readonly Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);

    public Vector3 EulerAngle
    {
        readonly get
        {

            var angle = Vector3.Zero;
            angle.Y = MathF.Atan2(Forward.X, Forward.Z);
            angle.X = MathF.Asin(-Forward.Y);
            angle.Z = MathF.Atan2(Right.Y, Up.Y);
            return angle * (180f / MathF.PI); // return degrees
        }
        set
        {
            value = value * MathF.PI / 180f;

            Console.WriteLine(value);
            Rotation = Quaternion.CreateFromYawPitchRoll(value.Y, value.X, value.Z);
        }
    }

    public Transform() { }
    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public void LookAt(Vector3 target)
    {
        Vector3 forward = Vector3.Normalize(target - Position);

        var dot = Vector3.Dot(forward, Vector3.UnitY);
        var up = System.Math.Abs(dot) > 0.9999f ? Vector3.UnitZ : Vector3.UnitY;

        var right = Vector3.Normalize(Vector3.Cross(forward, up));
        var newUp = Vector3.Cross(right, forward);

        var rotationMatrix = new Matrix4x4(
            right.X, newUp.X, -forward.X, 0,
            right.Y, newUp.Y, -forward.Y, 0,
            right.Z, newUp.Z, -forward.Z, 0,
            0, 0, 0, 1
        );

        Rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);
    }


}

