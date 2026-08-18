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
    Block[] blocks = new Block[CHUNK_CUBE];
    bool changed = false; // if true rebuild vertexData

    public Chunk(Vector2 position)
    {
        Position = position;
    }

    public static Chunk DebugChunk(Block air, Block dirt,
            Block stone, Block core, Vector2 position)
    {
        Chunk chunk = new Chunk(position);
        for (int i = 0; i < chunk.blocks.Length; i++)
        {
            var height = i / (CHUNK_RECT);
            ref Block block = ref chunk.blocks[i];
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
        }
        return chunk;
    }


    public ushort GetBlockIndex(byte x, byte y, byte z)
    {
        int index = y * CHUNK_RECT + z * CHUNK_SIZE + x;
        if (index >= CHUNK_CUBE)
            index = CHUNK_CUBE - 1;

        return (ushort)index;
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
        return block;
    }

    public Block GetBlock(ushort index)
    {
        return blocks[index];
    }

    public Block SetBlock(ushort index, Block block)
    {
        blocks[index] = block;
        return block;
    }

    internal void SetChunkPosition(Vector2 pos)
    {
        Position = pos;
    }
}
