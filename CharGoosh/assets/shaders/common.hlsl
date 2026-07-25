// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

struct PositionColorInput {
    float3 Position : TEXCOORD0;
    float4 Color : COLOR0;
};

struct PositionColorOutput {
    float4 Position : SV_Position;
    float4 Color : COLOR0;
};

struct PositionColorTexInput {
    float3 Position : TEXCOORD0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD1;
};

struct PositionColorTexOutput {
    float4 Position : SV_Position;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct PositionColorAtlasInput {
    float3 Position : TEXCOORD0;
    float4 Color : COLOR0;
    uint CoordPos : TEXCOORD1;
    uint TID : TEXCOORD2;
};

struct PositionColorAtlasOutput {
    float4 Position : SV_Position;
    float4 Color : COLOR0;
    float3 TexCoord : TEXCOORD0;
};

struct PositionMeshIndexInput {
    float3 Position : TEXCOORD0;
    uint Mesh : COLOR0;
    uint VertexIndex : TEXCOORD1;
    uint TID : TEXCOORD2;
};

struct PositionMeshIndexOutput {
    float4 Position : SV_Position;
    float3 TexCoord : TEXCOORD0;
};

// rotation.x = pitch, rotation.y = yaw, rotation.z = roll
float4x4 GetRotationMatrix(float3 rotation) {
    float yaw = rotation.y;
    float pitch = rotation.x;
    float roll = rotation.z;

    float cy = cos(yaw);
    float sy = sin(yaw);
    float cp = cos(pitch);
    float sp = sin(pitch);
    float cr = cos(roll);
    float sr = sin(roll);

    // reuse things to reduce multiplies
    float sp_sr = sp * sr;
    float sp_sy = sp * sy;
    float sp_cy = sp * cy;
    float cp_sy = cp * sy;
    float cp_cy = cp * cy;
    float sr_cp = sr * cp;
    float cr_cp = cr * cp;

    return float4x4(cr * cy + sp_sr * sy,   // m00
                    -sr * cy + cr * sp_sy,  // m10
                    cp_sy,                  // m20
                    0.0,                    // m30

                    sr_cp,  // m01
                    cr_cp,  // m11
                    -sp,    // m21
                    0.0,    // m31

                    -cr * sy + sp_sr * cy,  // m02
                    sr * sy + cr * sp_cy,   // m12
                    cp_cy,                  // m22
                    0.0,                    // m32

                    0.0, 0.0, 0.0, 1.0);
}

