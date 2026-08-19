// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.


using CharGoosh.Game.Block;
using MoonWorks.Math.Fixed;
using CharGoosh.Graphics.Resource;
namespace CharGoosh.Game.World;

using Block = Block.Block;
public class Chunk
{

    // after calculating different sizes i understood that 16x16 is best maybe
    // change height later but 16x16 is good but a little slower than 32 at 
    // some points
    public const byte CHUNK_SIZE = 16;
    public const short CHUNK_RECT = CHUNK_SIZE * CHUNK_SIZE;
    public const short CHUNK_HEIGHT = 256;
    public const int CHUNK_CUBE = CHUNK_RECT * CHUNK_HEIGHT;

    public Vector2 Position { get; private set; }
    public World? World { get; private set; }
    Block[] blocks = new Block[CHUNK_CUBE];
    MeshCullingFace[] cull = new MeshCullingFace[CHUNK_CUBE];
    public ReadOnlySpan<MeshCullingFace> Culls => cull.AsSpan();
    bool changed = false; // if true rebuild vertexData

    public Chunk(Vector2 position, World? world = null)
    {
        Position = position;
    }

    public static Chunk DebugChunk(Block air, Block dirt,
            Block stone, Block core, Vector2 position, World? world = null)
    {
        Chunk chunk = new Chunk(position);
        for (int i = 0; i < chunk.blocks.Length; i++)
        {
            var height = i / (CHUNK_RECT);
            Block block;
            if (height == 0)
            {
                block = core;
            }
            else if (height < 60)
            {
                block = stone;
            }
            else if (height < 64)
            {
                block = dirt;
            }
            else
            {
                block = air;
            }
            chunk.SetBlock((ushort)i, block);
        }
        chunk.changed = true;
        return chunk;
    }


    public ushort GetBlockIndex(byte x, byte y, byte z)
    {
        int index = y * CHUNK_RECT + z * CHUNK_SIZE + x;
        if (index >= CHUNK_CUBE)
            index = CHUNK_CUBE - 1;

        return (ushort)index;
    }

    public void GetBlockPos(int index, out byte x, out byte y, out byte z)
    {
        y = (byte)(index / CHUNK_RECT);
        int remainder = index % CHUNK_RECT;
        z = (byte)(remainder / CHUNK_SIZE);
        x = (byte)(remainder % CHUNK_SIZE);
    }

    public Block GetBlock(byte x, byte y, byte z)
    {
        int index = GetBlockIndex(x, y, z);
        return blocks[index];
    }

    public Block SetBlock(byte x, byte y, byte z, Block block)
    {
        int index = GetBlockIndex(x, y, z);
        blocks[index] = block;
        changed = true;
        return block;
    }

    public Block GetBlock(ushort index)
    {
        return blocks[index];
    }

    public Block SetBlock(ushort index, Block block)
    {
        blocks[index] = block;
        changed = true;
        CalculateBlockCullings(index);
        return block;
    }

    internal void SetChunkPosition(Vector2 pos)
    {
        Position = pos;
    }

    internal void CalculateBlockCullings(ushort index)
    {
        GetBlockPos(index, out byte x, out byte y, out byte z);

        // TODO: use transparent tag later

        Block currentBlock = blocks[index];
        if (currentBlock.Tags.Contains("no-cull"))
        {
            cull[index] = MeshCullingFace.None;
            NotifyNeighborsOfChange(x, y, z, currentBlock);
            return;
        }

        MeshCullingFace culling = MeshCullingFace.None;

        Block block;


        // Left (x - 1)
        if (x > 0)
        {
            block = GetBlock((byte)(x - 1), y, z);
            if (!block.Tags.Contains("no-cull"))
            {
                culling |= MeshCullingFace.Left;
            }
        }

        // Right (x + 1)
        if (x + 1 < CHUNK_SIZE)
        {
            block = GetBlock((byte)(x + 1), y, z);
            if (!block.Tags.Contains("no-cull"))
            {
                culling |= MeshCullingFace.Right;
            }
        }

        // Down (y - 1)
        if (y > 0)
        {
            block = GetBlock(x, (byte)(y - 1), z);
            if (!block.Tags.Contains("no-cull"))
            {
                culling |= MeshCullingFace.Down;
            }
        }

        // Top (y + 1)
        if (y + 1 < CHUNK_HEIGHT)
        {
            block = GetBlock(x, (byte)(y + 1), z);
            if (!block.Tags.Contains("no-cull"))
            {
                culling |= MeshCullingFace.Top;
            }
        }

        // Back (z - 1)
        if (z > 0)
        {
            block = GetBlock(x, y, (byte)(z - 1));
            if (!block.Tags.Contains("no-cull"))
            {
                culling |= MeshCullingFace.Back;
            }
        }

        // Front (z + 1)
        if (z + 1 < CHUNK_SIZE)
        {
            block = GetBlock(x, y, (byte)(z + 1));
            if (!block.Tags.Contains("no-cull"))
            {
                culling |= MeshCullingFace.Front;
            }
        }
        NotifyNeighborsOfChange(x, y, z, currentBlock);
        cull[index] = culling;
    }

    private void NotifyNeighborsOfChange(byte x, byte y, byte z, Block currentBlock)
    {
        if (x > 0)
            RecalculateNeighborCulling((byte)(x - 1), y, z, MeshCullingFace.Right, currentBlock);

        if (x + 1 < CHUNK_SIZE)
            RecalculateNeighborCulling((byte)(x + 1), y, z, MeshCullingFace.Left, currentBlock);

        if (y > 0)
            RecalculateNeighborCulling(x, (byte)(y - 1), z, MeshCullingFace.Top, currentBlock);

        if (y + 1 < CHUNK_HEIGHT)
            RecalculateNeighborCulling(x, (byte)(y + 1), z, MeshCullingFace.Down, currentBlock);

        if (z > 0)
            RecalculateNeighborCulling(x, y, (byte)(z - 1), MeshCullingFace.Front, currentBlock);

        if (z + 1 < CHUNK_SIZE)
            RecalculateNeighborCulling(x, y, (byte)(z + 1), MeshCullingFace.Back, currentBlock);

    }

    private void RecalculateNeighborCulling(byte x, byte y, byte z,
            MeshCullingFace faceToRecalculate, Block cullingBlock)
    {
        int blockIndex = GetBlockIndex(x, y, z);
        Block currentBlock = GetBlock(x, y, z);
        // Skip if air
        if (currentBlock.Tags.Contains("no-cull")) return;

        MeshCullingFace currentCull = cull[blockIndex];

        // true if should cull
        bool cullingAllowed = !cullingBlock.Tags.Contains("no-cull");

        switch (faceToRecalculate)
        {
            case MeshCullingFace.Top:
                if (cullingAllowed)
                    currentCull |= MeshCullingFace.Top;
                else
                    currentCull &= ~MeshCullingFace.Top;
                break;
            case MeshCullingFace.Down:
                if (cullingAllowed)
                    currentCull |= MeshCullingFace.Down;
                else
                    currentCull &= ~MeshCullingFace.Down;
                break;
            case MeshCullingFace.Left:
                if (cullingAllowed)
                    currentCull |= MeshCullingFace.Left;
                else
                    currentCull &= ~MeshCullingFace.Left;
                break;
            case MeshCullingFace.Right:
                if (cullingAllowed)
                    currentCull |= MeshCullingFace.Right;
                else
                    currentCull &= ~MeshCullingFace.Right;
                break;
            case MeshCullingFace.Front:
                if (cullingAllowed)
                    currentCull |= MeshCullingFace.Front;
                else
                    currentCull &= ~MeshCullingFace.Front;
                break;
            case MeshCullingFace.Back:
                if (cullingAllowed)
                    currentCull |= MeshCullingFace.Back;
                else
                    currentCull &= ~MeshCullingFace.Back;
                break;
        }

        cull[blockIndex] = currentCull;
    }

}
