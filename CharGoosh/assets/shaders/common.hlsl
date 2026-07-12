
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
