// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

#include "common.hlsl"

PositionColorTexOutput VS_Main(PositionColorTexInput input) {
    PositionColorTexOutput output;
    output.Position = float4(input.Position.xyz, 1.0f);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}
