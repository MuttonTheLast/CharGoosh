// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

using System;
using System.Numerics;
using CharGoosh.Math;

namespace CharGoosh.Game;

public class Camera
{
    public static Camera? Main { get; private set; } = null;

    public Transform Transform = new();

    private float _fov = 45;
    public float FOV
    {
        get { return _fov; }
        set
        {
            _fov = value < 45 ? 45 :
                (value > 120 ? 120 : value);
        }
    }


    public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAtLeftHanded(Transform.Position,
            Transform.Position + Transform.Forward, Transform.Up);

    public Matrix4x4 Projection => Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            FOV * MathF.PI / 180f,
            Transform.Scale.X / Transform.Scale.Y, 0.1f,
            Transform.Scale.Z > 0.2f ? Transform.Scale.Z : 0.2f);

    public Camera() { }
    public Camera(Transform transform, float fov)
    {
        Transform = transform;
        FOV = fov;
    }

    ~Camera()
    {
        if (Main == this)
        {
            Main = null;
        }
    }

    public void SetMain()
    {
        Main = this;
    }

}
