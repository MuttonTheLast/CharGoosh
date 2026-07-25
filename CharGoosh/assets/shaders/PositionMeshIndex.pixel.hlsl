// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

#include "common.hlsl"

Texture2DArray<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

float4 PS_Main(PositionMeshIndexOutput input) : SV_Target {
    // Sample the texture using the provided texture coordinates
    uint3 dim;
    Texture.GetDimensions(dim.x, dim.y, dim.z);
    float4 texColor = Texture.Sample(Sampler, input.TexCoord);

    // Multiply the sampled texture color with the vertex color
    float4 finalColor = texColor * float4(1, 1, 1, 1);

    return finalColor;
}

