
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;
namespace CharGoosh.Graphics.Vertex;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct PositionMeshIndexAtlas(Vector3 pos, uint meshOffset, uint vertexIndex, uint tid) : IVertexType
{
    [FieldOffset(0)]
    public Vector3 Position = pos;
    [FieldOffset(12)]
    public uint MeshOffset = meshOffset;
    [FieldOffset(16)]
    public uint VertexIndex = vertexIndex;
    [FieldOffset(20)]
    public uint TID = tid;


    public static VertexElementFormat[] Formats { get; } = [
        VertexElementFormat.Float3,
        VertexElementFormat.Uint,
        VertexElementFormat.Uint,
        VertexElementFormat.Uint,
    ];

    public static uint[] Offsets { get; } = [
        0, 12, 16, 20
    ];

    public static string VertexShaderName = "PositionMeshIndex.vert.hlsl";
    public static string PixelShaderName = "PositionMeshIndex.pixel.hlsl";

}

