using Xunit.Abstractions;
using MoonWorks.Math.Fixed;

namespace Chunk;

using CharGoosh.Game.World;
using CharGoosh.Game.Block;
using Chunk = CharGoosh.Game.World.Chunk;

public class ChunkTest
{

    ITestOutputHelper _output;


    Block core;
    Block dirt;
    Block stone;
    Block air;
    public ChunkTest(ITestOutputHelper output)
    {
        _output = output;
        core = BlockRegistry.RegisterBlock().Name("bedrock");
        dirt = BlockRegistry.RegisterBlock().Name("dirt");
        stone = BlockRegistry.RegisterBlock().Name("stone");
        air = BlockRegistry.GetBlockFromName("air");
    }

    [Fact]
    public void Test1()
    {

        var chunk = Chunk.DebugChunk(air, dirt, stone, core, Vector2.Zero);

        // make sure all functions work
        var dirt_block = BlockRegistry.GetBlockFromName("dirt");
        var stone_block = BlockRegistry.GetBlockFromName("stone");
        var core_block = BlockRegistry.GetBlockFromName("bedrock");

        var block = chunk.GetBlock(0, 0, 0);
        _output.WriteLine(block.ID.ToString());
        Assert.True(block.ID == core_block.ID);

        block = chunk.GetBlock(0, 1, 0);
        _output.WriteLine(block.ID.ToString());
        Assert.True(block.ID == stone_block.ID);

        block = chunk.GetBlock(0, 63, 0);
        _output.WriteLine(block.ID.ToString());
        Assert.True(block.ID == dirt_block.ID);

        block = chunk.GetBlock(0, 111, 0);
        _output.WriteLine(block.ID.ToString());
        Assert.True(block.ID == air.ID);

        block = chunk.GetBlock(255, 255, 255); // will bee ushort max - 1
        _output.WriteLine(block.ID.ToString());
        Assert.True(block.ID == air.ID);
    }

    [Fact]
    public void Test2()
    {
        World world = World.DebugWorld(air, dirt, stone, core);

        int index = world.CenterChunk + (world.CenterChunk * world.ChunksPerRow) + 13;

        index = world.GetChunkIndexLocal(Vector2.Zero);
        Chunk chunk1 = world.GetChunk(index);

        world.SetCurrentPoition(new Vector3(100, 0, 100));
        int moved = 6; // 100 / 16 = (int)6

        index = world.GetChunkIndexLocal(new Vector2(-moved, -moved));
        Chunk chunk2 = world.GetChunk(index);
        Assert.True(ReferenceEquals(chunk1, chunk2));

        index = world.GetChunkIndexLocal(Vector2.Zero);
        chunk2 = world.GetChunk(index);
        Assert.False(ReferenceEquals(chunk1, chunk2));
    }
}
