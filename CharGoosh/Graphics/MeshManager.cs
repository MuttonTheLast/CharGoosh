
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Storage;

using Buffer = MoonWorks.Graphics.Buffer;
namespace CharGoosh.Graphics;

struct MeshData(ushort[] indices, uint offet, ushort vertexCount)
{
    public ushort[] Indices = indices;
    public uint Offset = offet;
    public ushort VertexCount = vertexCount;
}

public class MeshManager : IDisposable
{

    readonly GraphicsDevice _gd;
    readonly TitleStorage _titleStorage;

    // meshid -> MeshOffset
    readonly Dictionary<uint, MeshData> _meshOffsets = [];


    public Buffer Meshes { get; private set; }
    uint _bufferElementCount = 0;
    uint _counter = 1;

    private bool disposed = false;

    public MeshManager(GraphicsDevice gd, TitleStorage titleStorage)
    {
        _gd = gd;

        _titleStorage = titleStorage;
        Meshes = Buffer.Create<MeshDataGPU>(_gd, BufferUsageFlags.GraphicsStorageRead, 0);
    }

    public uint GetMeshOffset(uint meshId)
    {
        if (!_meshOffsets.TryGetValue(meshId, out MeshData md))
        {
            return 0;
        }
        return md.Offset;
    }
    public ReadOnlySpan<ushort> GetMeshIndices(uint meshId)
    {
        if (!_meshOffsets.TryGetValue(meshId, out MeshData md))
        {
            return [];
        }
        return md.Indices.AsSpan();
    }

    public uint AddMesh(ReadOnlySpan<MeshDataGPU> meshData, ReadOnlySpan<uint> indices)
    {
        var indicesLength = indices.Length;
        if (indicesLength > ushort.MaxValue)
        {
            Console.WriteLine($"Voxel engine does not support indices more than {ushort.MaxValue}");
            return 0;
        }

        if (indicesLength == 0)
        {
            Console.WriteLine("MeshData should be passed with valid indices");
            return 0;
        }

        uint meshDataLenght = (uint)meshData.Length;

        var oldBuffer = Meshes;
        Meshes = Buffer.Create<MeshDataGPU>(_gd, BufferUsageFlags.GraphicsStorageRead,
                _bufferElementCount + meshDataLenght);

        var cmdBuf = _gd.AcquireCommandBuffer();
        // copy data
        var copyPass = cmdBuf.BeginCopyPass();
        // copy old data
        var source = new BufferLocation { Buffer = oldBuffer, Offset = 0 };
        var dest = new BufferLocation { Buffer = Meshes, Offset = 0 };
        copyPass.CopyBufferToBuffer(source, dest, oldBuffer.Size, false);

        // Upload to new space
        var transferBuffer = TransferBuffer.Create<MeshDataGPU>(_gd,
                TransferBufferUsage.Upload, meshDataLenght);

        Span<MeshDataGPU> uploadBuffer = transferBuffer.Map<MeshDataGPU>(false, 0);
        meshData.CopyTo(uploadBuffer);
        transferBuffer.Unmap();
        copyPass.UploadToBuffer<MeshDataGPU>(transferBuffer, Meshes, 0, oldBuffer.Size, meshDataLenght, false);

        cmdBuf.EndCopyPass(copyPass);

        var fence = _gd.SubmitAndAcquireFence(cmdBuf);

        // copy indices to an array
        var indicesData = new ushort[indicesLength];
        for (int i = 0; i < indicesLength; i++)
        {
            indicesData[i] = (ushort)indices[i];
        }

        // var uploader = new ResourceUploader(_gd, meshDataLenght);
        // uploader.SetBufferData(Meshes, _bufferElementCount, meshData, false);
        //
        // uploader.UploadAndWait();
        // uploader.Dispose();
        //
        _gd.WaitForFence(fence);
        transferBuffer.Dispose();
        oldBuffer.Dispose();

        _meshOffsets.Add(_counter, new MeshData(indicesData, _bufferElementCount,
                    (ushort)meshDataLenght));

        _bufferElementCount += meshDataLenght;
        return _counter++;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }
        if (disposing)
        {
            Meshes.Dispose();
            _meshOffsets.Clear();
            _counter = 0;
            _bufferElementCount = 0;
        }

        disposed = true;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct MeshDataGPU(Vector3 pos, Vector3 normal, Vector2 uv)
{
    [FieldOffset(0)]
    public Vector3 Pos = pos;
    [FieldOffset(12)]
    public Vector3 Normal = normal;
    [FieldOffset(24)]
    public Vector2 UV = uv;
}

