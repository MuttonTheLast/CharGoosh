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
#define MeshIndexData_Spride 32
struct MeshIndexData {
    float3 pos;
    float3 normal;
    float2 uv;
};

cbuffer TextureAtlasUniform : register(b0, space1) {
    float4x4 mat : packoffset(c0);
    uint atlasSize : packoffset(c4.x);  // current atlas size
};

ByteAddressBuffer SpriteData : register(t0, space0);
ByteAddressBuffer VertexData : register(t1, space0);

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

MeshIndexData GetVertexData(uint offset, uint mvid) {
    MeshIndexData data;
    uint base = offset + MeshIndexData_Spride * mvid;
    uint3 pos = VertexData.Load3(base);
    data.pos = float3(asfloat(pos.x), asfloat(pos.y), asfloat(pos.z));
    uint3 normal = VertexData.Load3(base + 12);
    data.normal = float3(asfloat(normal.x), asfloat(normal.y), asfloat(normal.z));
    uint2 uv = VertexData.Load2(base + 24);
    data.uv = float2(asfloat(uv.x), asfloat(uv.y));
    return data;
}

PositionMeshIndexOutput VS_Main(PositionMeshIndexInput input) {
    PositionMeshIndexOutput output;
    MeshIndexData vertexData = GetVertexData(input.Mesh, input.VertexIndex);

    float4x4 rotMat = GetRotationMatrix(float3(0, 0, 0));
    float3 Anchor = float3(0, 0, 0);
    float3 localPos = vertexData.pos;
    float3 rotatedPos = mul(rotMat, float4(localPos, 1.0)).xyz;
    float3 worldPos = rotatedPos + Anchor + input.Position;
    output.Position = mul(float4(worldPos, 1.0), mat);

    // output.Position = mul(float4(vertexData.pos, 1.0), mat);

    TextureSpriteData data = GetTextureData(input.TID);
    float2 pos = float2(data.position.x, data.position.y);
    float2 endPos = pos + float2(data.size.x, data.size.y);
    pos = pos / atlasSize;
    endPos = endPos / atlasSize;

    output.TexCoord.xy = lerp(pos, endPos, vertexData.uv);
    output.TexCoord.z = data.atlas;

    return output;
}
