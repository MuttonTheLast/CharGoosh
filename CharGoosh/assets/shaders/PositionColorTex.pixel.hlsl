#include "common.hlsl"

Texture2DArray<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

float4 PS_Main(PositionColorTexOutput input) : SV_Target {
  // Sample the texture using the provided texture coordinates
  float4 texColor = Texture.Sample(Sampler, float3(input.TexCoord, 0));

  // Multiply the sampled texture color with the vertex color
  float4 finalColor = texColor * input.Color;

  return finalColor;
}
