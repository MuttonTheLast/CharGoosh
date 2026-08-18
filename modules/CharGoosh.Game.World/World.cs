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


public class World
{
    public readonly byte ViewDistance;
    public readonly byte SimulateDistance;
    public readonly int CenterDistance;
    public readonly int CenterChunk;
    public readonly int ChunksPerRow;

    public Vector2 CenterChunkV2 => new Vector2(CenterChunk, CenterChunk);

    Chunk[] _chunks;
    public int ChunksCount => _chunks.Length;
    // we wont reload from disk and use loaded one
    // trying to cache near chunks and when they got really far then unload
    Chunk[] _chunkCache;
    public int CacheSize => _chunkCache.Length;

    public Vector2 CurrentChunkPos { get; private set; }

    public World(byte viewDistance = 10, byte simulateDistance = 10, byte cacheSize = 64)
    {
        ViewDistance = viewDistance;
        SimulateDistance = simulateDistance;
        // we need 1 more passive chunk each way because maybe we want to draw
        // structor or tree that needs more chunk and yeah 4 is enough 
        // for bigger structors we will think something
        // always + 1 because i want be center
        CenterDistance = Math.Max(viewDistance, simulateDistance) + 2; // distance from center
        CenterChunk = CenterDistance;
        ChunksPerRow = CenterDistance * 2 + 1;
        var ChunkCount = ChunksPerRow * ChunksPerRow;

        _chunks = new Chunk[ChunkCount];
        _chunkCache = new Chunk[cacheSize];
        CurrentChunkPos = Vector2.Zero;
        ClearCache();
    }

    public static World DebugWorld(Block air, Block dirt, Block stone, Block bedrock)
    {
        World world = new World();
        world.ReloadAllChunks();
        return world;
    }

    public void ClearCache()
    {
        // its not possible we can access Fix64 max 
        // because we dont have block position that points to that chunk
        Fix64 max = Fix64.MaxValue;
        for (int i = 0; i < _chunkCache.Length; i++)
        {
            _chunkCache[i] = new Chunk(new Vector2(max, max));
        }
    }

    public int GetChunkIndex(in Vector2 chunkPos)
    {
        if (!IsValidChunk(chunkPos))
        {
            return -1;
        }
        var offset = chunkPos - CurrentChunkPos;
        int centerIndex = CenterChunk + CenterChunk * ChunksPerRow;
        return (int)(centerIndex + offset.Y * ChunksPerRow + offset.X);
    }

    public int GetChunkIndexLocal(in Vector2 localChunkPos)
    {
        return GetChunkIndex(localChunkPos + CurrentChunkPos);
    }

    public int MakeSafeChunkIndex(int index)
    {
        return index < _chunks.Length && index > -1 ?
            index :
            CenterChunk + CenterChunk * ChunksPerRow;
    }

    public Vector2 CalculateLocalPosition(int index)
    {
        index = MakeSafeChunkIndex(index);
        Vector2 pos = Vector2.Zero;

        pos.Y = new Fix64(index / ChunksPerRow);
        pos.X = new Fix64(index % ChunksPerRow);

        pos = pos - new Vector2(CenterChunk, CenterChunk);


        return pos;
    }

    public Vector2 GetChunkPosition(int index)
    {
        index = MakeSafeChunkIndex(index);
        return _chunks[index].Position;
    }

    public static Vector2 GetChunkPosition(in Vector3 blockPos)
    {
        // in negative we should -1 but positive its ok
        Vector2 pos = new Vector2(0, 0);
        if (blockPos.X < 0)
        {
            pos.X = -Fix64.One;
        }
        if (blockPos.Z < 0)
        {
            pos.Y = -Fix64.One;
        }

        pos.X += Fix64.Floor(blockPos.X / Chunk.CHUNK_SIZE);
        pos.Y += Fix64.Floor(blockPos.Z / Chunk.CHUNK_SIZE);
        return pos;
    }

    // checks if chunk is in some distance.
    // if not give distance will use normal size of chunk
    public bool IsValidChunk(in Vector2 chunkPos, int distance = 0)
    {
        distance = distance == 0 ? CenterChunk : distance;
        if (chunkPos.X > CurrentChunkPos.X + distance ||
            chunkPos.X < CurrentChunkPos.X - distance ||
            chunkPos.Y > CurrentChunkPos.Y + distance ||
            chunkPos.Y < CurrentChunkPos.Y - distance)
        {
            return false;
        }
        return true;
    }

    // Gets chunk at position if loaded
    // and if not will get current chunk
    public Chunk GetChunk(Vector2 chunkPos)
    {
        if (!IsValidChunk(chunkPos))
        {
            chunkPos = CurrentChunkPos;
        }
        int index = GetChunkIndex(chunkPos);

        return GetChunk(index);
    }

    public Chunk GetChunk(int index)
    {
        index = MakeSafeChunkIndex(index);
        return _chunks[index];
    }

    public void SetCurrentPoition(Vector3 blockPos)
    {
        var chunkPos = GetChunkPosition(blockPos);
        if (chunkPos == CurrentChunkPos)
        {
            return;
        }

        Vector2 diff = chunkPos - CurrentChunkPos;
        CurrentChunkPos = chunkPos;
        if (!IsValidChunk(chunkPos, CenterChunk))
        {
            ReloadAllChunks();
        }
        else
        {
            ReuseChunks(diff);
        }
    }

    private void ReuseChunks(Vector2 diff)
    {
        int dX = (int)diff.X;
        int dY = (int)diff.Y;

        // INFO: AI FIXED:
        int minX = Math.Max(-CenterDistance, -CenterDistance - dX);
        int maxX = Math.Min(CenterDistance, CenterDistance - dX);
        int minY = Math.Max(-CenterDistance, -CenterDistance - dY);
        int maxY = Math.Min(CenterDistance, CenterDistance - dY);

        int stepX = dX >= 0 ? 1 : -1;
        int stepY = dY >= 0 ? 1 : -1;
        // INFO: End AI FIX


        for (int y = stepY > 0 ? minY : maxY; stepY > 0 ? y <= maxY : y >= minY; y += stepY)
        {
            for (int x = stepX > 0 ? minX : maxX; stepX > 0 ? x <= maxX : x >= minX; x += stepX)
            {
                var dest = new Vector2(x, y);
                var src = new Vector2(x + dX, y + dY);
                _chunks[GetChunkIndexLocal(dest)] = _chunks[GetChunkIndexLocal(src)];
            }
        }

        // INFO: AI Recreated my logic 
        // i love that code so i wont change it
        CreateChunks(new Vector2(-CenterDistance, -CenterDistance),
                new Vector2(minX - 1, CenterDistance));

        CreateChunks(new Vector2(maxX + 1, -CenterDistance),
                new Vector2(CenterDistance, CenterDistance));

        CreateChunks(new Vector2(minX, -CenterDistance),
                new Vector2(maxX, minY - 1));

        CreateChunks(new Vector2(minX, maxY + 1), new Vector2(maxX, CenterDistance));
        // INFO: End AI Generate

    }

    private void ReloadAllChunks()
    {
        CreateChunks(new Vector2(-CenterDistance, -CenterDistance),
                new Vector2(CenterDistance, CenterDistance));
    }

    // creates chunks in [from, to] inclusive (local coords)
    private void CreateChunks(Vector2 from, Vector2 to)
    {
        int startX = (int)from.X;
        int endX = (int)to.X;
        int startY = (int)from.Y;
        int endY = (int)to.Y;
        if (startX > endX || startY > endY)
        {
            return;
        }
        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                _chunks[GetChunkIndexLocal(new Vector2(x, y))] = CreateDebugChunk(new Vector2(x, y));
            }
        }
    }

    private Chunk CreateDebugChunk(Vector2 localPos)
    {
        Block air = BlockRegistry.GetBlockFromName("air");
        Block dirt = BlockRegistry.GetBlockFromName("dirt");
        Block stone = BlockRegistry.GetBlockFromName("stone");
        Block bedrock = BlockRegistry.GetBlockFromName("bedrock");
        return Chunk.DebugChunk(air, dirt, stone, bedrock, CurrentChunkPos + localPos);
    }

    private void SetChunk(int replaceIndex, Chunk chunk)
    {
        int furthest = GetFurthestChunkIndex();
        if (furthest != 0)
        {
            _chunkCache[furthest] = _chunks[replaceIndex];
        }
        _chunks[replaceIndex] = chunk;
    }

    private int GetFurthestChunkIndex()
    {
        Chunk cached;
        Fix64 distance;
        Fix64 furthest = Fix64.Zero;
        int index = -1;
        for (int i = 0; i < _chunkCache.Length; i++)
        {
            cached = _chunkCache[i];
            distance = Vector2.Distance(CurrentChunkPos, cached.Position);
            if (distance > furthest)
            {
                furthest = distance;
                index = i;
            }
        }
        return index;
    }

    // public Block GetBlock(long x, long y, long z)
    // {
    //     Vector2 cp = GetChunckPos(x, z);
    // }
    public override string ToString()
    {
        string result = "";
        Chunk chunk;
        for (int i = 0; i < _chunks.Length; i++)
        {
            if (i % ChunksPerRow == 0)
            {
                result += "\n";
            }
            chunk = _chunks[i];
            result += $"[ {(chunk.Position.X):D2}, {(chunk.Position.Y):D2}] ";
        }
        return result;
    }
}
