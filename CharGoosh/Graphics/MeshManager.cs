// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Storage;
using Serilog;

using Buffer = MoonWorks.Graphics.Buffer;
namespace CharGoosh.Graphics;

[Flags]
public enum MeshCullingFace : byte
{
    None = 0,
    Top = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Front = 1 << 4,
    Back = 1 << 5,

    All = Top | Down | Left | Right | Front | Back,
}

struct MeshData(MeshIndex[] indices, uint offet, ushort vertexCount)
{
    public MeshIndex[] Indices = indices;
    public uint Offset = offet;
    public ushort VertexCount = vertexCount;
}
public struct MeshIndex(ushort index, MeshCullingFace cullingFace)
{
    public readonly ushort Index = index;
    public MeshCullingFace CullingFace = cullingFace;
}


public class MeshManager : IDisposable
{

    readonly GraphicsDevice _gd;
    readonly TitleStorage _titleStorage;

    // meshid -> MeshOffset
    readonly Dictionary<uint, MeshData> _meshDatas = [];


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
        if (!_meshDatas.TryGetValue(meshId, out MeshData md))
        {
            return 0;
        }
        return md.Offset;
    }
    public ReadOnlySpan<MeshIndex> GetMeshIndices(uint meshId)
    {
        if (!_meshDatas.TryGetValue(meshId, out MeshData md))
        {
            return [];
        }
        return md.Indices.AsSpan();
    }

    public ushort[] GetMeshVisibleIndices(uint meshId, MeshCullingFace visibleFaces)
    {
        var indices = GetMeshIndices(meshId);
        ushort[] allIndices = new ushort[indices.Length];
        int facesCount = 0;
        for (int i = 0; i < indices.Length; i++)
        {
            var currentIndex = indices[i];
            // Check if the face should be rendered (not culled)
            // if cull is off for that face or currently visible face then draw it
            if (currentIndex.CullingFace == 0 ||
                    (visibleFaces & currentIndex.CullingFace) != 0)
            {
                allIndices[facesCount] = currentIndex.Index;
                facesCount++;
            }
        }
        ushort[] visibleIndices = new ushort[facesCount];
        Array.Copy(allIndices, 0, visibleIndices, 0, facesCount);
        return visibleIndices;
    }


    public uint AddMesh(ReadOnlySpan<MeshDataGPU> meshData, ReadOnlySpan<uint> indices,
            float faceCullingDistance = 1.0f)
    {
        var indicesLength = indices.Length;
        if (!ValidateMeshData(indicesLength, out string error))
        {
            Log.Error(error);
            return 0;
        }
        uint meshDataLenght = (uint)meshData.Length;

        var oldBuffer = Meshes;
        Meshes = Buffer.Create<MeshDataGPU>(_gd, BufferUsageFlags.GraphicsStorageRead,
                _bufferElementCount + meshDataLenght);

        var cmdBuf = _gd.AcquireCommandBuffer();
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
        int indexCounter = 0;
        MeshCullingFace cullingFace = MeshCullingFace.All; // all faces cull by default
        var indicesData = new MeshIndex[indicesLength];
        for (int i = 0; i < indicesLength; i++, indexCounter++)
        {
            var index = indices[i];
            var vertex = meshData[(int)index];

            CalculateCullingFace(in vertex.Pos, ref cullingFace, faceCullingDistance);

            MeshIndex meshIndex = new MeshIndex((ushort)index, cullingFace);
            indicesData[i] = meshIndex;

            if (indexCounter == 2)
            {
                SetIndexCullingFace(i - 2, cullingFace, ref indicesData);
                indexCounter = -1;
                cullingFace = MeshCullingFace.All;
            }
        }

        _gd.WaitForFence(fence);
        transferBuffer.Dispose();
        oldBuffer.Dispose();

        _meshDatas.Add(_counter, new MeshData(indicesData, _bufferElementCount,
                    (ushort)meshDataLenght));

        _bufferElementCount += meshDataLenght;
        return _counter++;
    }

    // Validation method
    private bool ValidateMeshData(int indicesLength, out string errorMessage)
    {
        errorMessage = "";
        if (indicesLength == 0)
        {
            errorMessage = "Empty index buffer provided. Mesh must have at least one triangle.";
            return false;
        }

        if (indicesLength % 3 != 0)
        {
            errorMessage = $"Invalid index count {indicesLength}: must be multiple of 3 for triangles.";
            return false;
        }

        if (indicesLength > ushort.MaxValue)
        {
            errorMessage = $"Index count {indicesLength} exceeds 16-bit limit ({ushort.MaxValue}). " +
                           "Use 32-bit indices or split mesh into sub-meshes.";
            return false;
        }

        return true;
    }

    private void CalculateCullingFace(in Vector3 pos, ref MeshCullingFace cullingFace,
            float faceCullingDistance)
    {

        if (pos.X < faceCullingDistance)
            cullingFace &= ~MeshCullingFace.Right;  // Remove Right from culling

        if (pos.X > -faceCullingDistance)
            cullingFace &= ~MeshCullingFace.Left;   // Remove Left from culling

        if (pos.Y < faceCullingDistance)
            cullingFace &= ~MeshCullingFace.Top;    // Remove Top from culling

        if (pos.Y > -faceCullingDistance)
            cullingFace &= ~MeshCullingFace.Down;   // Remove Down from culling

        if (pos.Z < faceCullingDistance)
            cullingFace &= ~MeshCullingFace.Back;  // Remove Front from culling

        if (pos.Z > -faceCullingDistance)
            cullingFace &= ~MeshCullingFace.Front;   // Remove Back from culling
    }

    private void SetIndexCullingFace(int startingIndex, MeshCullingFace cullingFace,
            ref MeshIndex[] indices)
    {
        for (int i = 0; i < 3; i++)
        {
            indices[startingIndex + i].CullingFace = cullingFace;
        }
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
            _meshDatas.Clear();
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

