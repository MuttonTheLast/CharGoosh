// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.


#include "common.hlsl"

#define TextureSpriteData_Spride 24
    struct TextureSpriteData {
    uint tid;
    uint atlas;
    uint2 position;
    uint2 size;
};

cbuffer TextureAtlasUniform : register(b0, space1) {
    uint atlasSize : packoffset(c0.x);  // current atlas index
};

ByteAddressBuffer SpriteData : register(t0, space0);

TextureSpriteData GetTextureData(uint tid) {
    TextureSpriteData data;
    uint base = tid * TextureSpriteData_Spride;
    data.tid = tid;
    data.atlas = SpriteData.Load(base + 4);  // first 4 is tid
    uint4 pos_size = SpriteData.Load4(base + 8);
    data.position = pos_size.xy;
    data.size = pos_size.zw;
    return data;
}

PositionColorAtlasOutput VS_Main(PositionColorAtlasInput input) {
    PositionColorAtlasOutput output;
    output.Position = float4(input.Position.xyz, 1.0f);
    output.Color = input.Color;
    TextureSpriteData data = GetTextureData(input.TID);

    float2 pos = float2(data.position.x, data.position.y);
    float2 endPos = pos + float2(data.size.x, data.size.y);
    pos = pos / atlasSize;
    endPos = endPos / atlasSize;
    // if (input.CoordPos == 0) {
    // output.TexCoord.xy = pos;
    //} else if (input.CoordPos == 1) {
    // output.TexCoord.xy = float2(endPos.x, pos.y);
    //} else if (input.CoordPos == 2) {
    // output.TexCoord.xy = float2(pos.x, endPos.y);
    //} else {  // 3 or somethign bad
    // output.TexCoord.xy = endPos;
    //}
    // float2 uv = float2(input.CoordPos & 1, 1.0 - ((input.CoordPos >> 1) & 1));
    float2 uv = float2(input.CoordPos & 1, (input.CoordPos >> 1) & 1);
    output.TexCoord.xy = lerp(pos, endPos, uv);

    return output;
}
