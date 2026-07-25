// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

#define RESULT_SUCCESS 0       // success
#define RESULT_FAILED 1        // failed
#define RESULT_CANT_FIT 2      // Not fit in current texture atlas
#define RESULT_CANT_FIT_ANY 4  // cant fit anything in current atlas

struct AddResult {
    uint2 texturePos;  // just return position maybe needed someday
    uint result;
};

#define TextureSpriteData_Spride 24
struct TextureSpriteData {
    uint tid;
    uint atlas;
    uint2 position;
    uint2 size;
};

cbuffer TextureAtlasUniform : register(b0, space2) {
    uint tid : packoffset(c0.x);             // texture id
    uint atlasIndex : packoffset(c0.y);      // current atlas index
    uint minTextureSize : packoffset(c0.z);  // min texture size (quad) that fits
};

// INFO: Texture with its sampler basically we don't want sampler but SDL forces
// us to have sampler with texture for my usecase
Texture2D<float4> Texture : register(t0, space0);
SamplerState Sampler : register(s0, space0);

// INFO: We cant have RWTexture2DArray in compute shader because of SDL
RWTexture2D<float4> Atlas : register(u0, space1);  // Current texture atlas

// INFO: Ok SDL forces us to use RWByteAddressBuffer and not RWStructoredBuffer
// for better cross platform compatability (problem is with metal and android i
// guess)
RWByteAddressBuffer SpriteDataBuffer : register(u1, space1);  // textures data
RWByteAddressBuffer ResultData : register(u2, space1);        // result

// checks if texture fits or no
bool TextureFits(in uint2 foundedPos, in uint2 textureSize, in uint2 atlasSize) {
    for (uint height = 0; height < textureSize.y; height += minTextureSize) {
        if (foundedPos.y + height >= atlasSize.y) {
            return false;
        }

        for (uint width = 0; width < textureSize.x; width += minTextureSize) {
            uint2 realPos = uint2(foundedPos.x + width, foundedPos.y + height);
            if (realPos.x >= atlasSize.x) {
                return false;
            }

            float4 Color = Atlas[realPos];
            if (Color.r != 0.0f || Color.g != 0.0f || Color.b != 0.0f || Color.a != 0.0f) {
                return false;
            }
        }
    }
    return true;
}

// finds empty slot then checks if texture can fit
bool FindEmptySlot(out uint2 foundedPos, in uint2 textureSize, in uint2 atlasSize,
                   out bool canFitAnything) {
    foundedPos = uint2(0, 0);
    canFitAnything = false;
    for (foundedPos.y = 0; foundedPos.y < atlasSize.y; foundedPos.y += minTextureSize) {
        for (foundedPos.x = 0; foundedPos.x < atlasSize.x; foundedPos.x += minTextureSize) {
            float4 color = Atlas[foundedPos];
            if (color.r == 0.0f && color.g == 0.0f && color.b == 0.0f && color.a == 0.0f) {
                canFitAnything = true;
                if (TextureFits(foundedPos, textureSize, atlasSize)) {
                    return true;
                }
            }
        }
    }
    return false;
}

// put texture at wanted position
void ReplaceTexture(in uint2 foundedPos, in uint2 textureSize) {
    // each thread draws to its own texture region
    for (uint height = 0; height < textureSize.y; height++) {
        for (uint width = 0; width < textureSize.x; width++) {
            uint2 AtlasPos = uint2(foundedPos.x + width, foundedPos.y + height);
            float2 uv = float2(width, height) / float2(textureSize.x, textureSize.y);

            float4 Color = Texture.SampleLevel(Sampler, uv, 0);
            // Atlas[AtlasPos] = float4(1, 1, 1, 1);
            if (Color.a < 0.01f) {
                Atlas[AtlasPos] = float4(0, 0, 0, 0.001);  // Mark as empty
            } else {
                Atlas[AtlasPos] = Color;
            }
        }
    }
}

void StoreTextureData(in uint2 foundedPos, in uint2 textureSize) {
    uint base = tid * TextureSpriteData_Spride;

    SpriteDataBuffer.Store2(base, uint2(tid, atlasIndex));
    SpriteDataBuffer.Store4(base + 8, uint4(foundedPos, textureSize));
}

void StoreResult(uint2 foundedPos, uint result) {
    ResultData.Store3(0, uint3(foundedPos, result));
}

[numthreads(1, 1, 1)]
void CS_Main(uint3 GlobalInvocationID: SV_DispatchThreadID) {
    // default result is fail result
    uint result = RESULT_FAILED;

    uint2 textureSize;
    uint2 atlasSize;
    Texture.GetDimensions(textureSize.x, textureSize.y);
    Atlas.GetDimensions(atlasSize.x, atlasSize.y);

    uint2 foundedPos;
    bool canFitAnything = false;
    if (FindEmptySlot(foundedPos, textureSize, atlasSize, canFitAnything)) {
        ReplaceTexture(foundedPos, textureSize);
        StoreTextureData(foundedPos, textureSize);

        result = RESULT_SUCCESS;
    } else {
        result = result | RESULT_CANT_FIT;

        if (!canFitAnything) {
            result = result | RESULT_CANT_FIT_ANY;
        }
    }
    StoreResult(foundedPos, result);
}

