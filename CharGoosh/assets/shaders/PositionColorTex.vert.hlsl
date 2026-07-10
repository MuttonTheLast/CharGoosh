#include "common.hlsl"

PositionColorTexOutput VS_Main(PositionColorTexInput input) {
  PositionColorTexOutput output;
  output.Position = float4(input.Position.xyz, 1.0f);
  output.Color = input.Color;
  output.TexCoord = input.TexCoord;
  return output;
}
