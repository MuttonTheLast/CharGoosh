using CharGoosh.Game.Block;
using CharGoosh.Game.Tag;
using Xunit.Abstractions;

namespace Block;

using Block = CharGoosh.Game.Block.Block;
public class BlockTest
{
    ITestOutputHelper _output;

    public BlockTest(ITestOutputHelper output)
    {
        _output = output;
    }


    [Fact]
    public void BlockID()
    {
        Block block = BlockRegistry.RegisterBlock();
    }

    [Fact]
    public void TagTest()
    {
        Block block = BlockRegistry.RegisterBlock();
        Block block2 = BlockRegistry.RegisterBlock();
        Tags tags = block.Tags;
        Tags tags2 = block2.Tags;

        const int testTagCount = 3;

        for (int i = 0; i < testTagCount; i++)
        {
            tags.Add("Test" + i);
            tags2.Add("Test2" + i);
        }

        for (int i = 0; i < testTagCount; i++)
        {
            Assert.True(block.Tags.Contains("Test" + i),
                    $"Block should contain tag Test{i}");
            Assert.True(block2.Tags.Contains("Test2" + i),
                    $"Block2 should contain tag Test2{i}");


            Assert.False(block.Tags.Contains("Test2" + i),
                    $"Block should not contain tag Test2{i}");
            Assert.False(block2.Tags.Contains("Test" + i),
                    $"Block2 should not contain tag Test{i}");
        }

        tags.Remove("Test2");

        Assert.False(block.Tags.Contains("Test2"), "Block should not contain tag Test2");
    }

    [Fact]
    public void AttributeTest()
    {
        Block block = BlockRegistry.RegisterBlock();

        block.MeshID(1).TextureID(2).DestroyTime(20);

        Assert.Equal(1u, block.MeshID());
        Assert.Equal(2u, block.TextureID());
        Assert.Equal(20, block.DestroyTime());
    }

    [Fact]
    public void NameTest()
    {

        Block block = BlockRegistry.RegisterBlock();
        Block block2 = BlockRegistry.RegisterBlock();
        block.Name("TestBlock");
        block2.Name("TestBlock2");
        Block block3 = Block.FromName("TestBlock2");

        Assert.Equal(block2.ID, block3.ID);
        Assert.NotEqual(block.ID, block3.ID);

        Block airBlock = Block.FromName("air");
        Assert.Equal(0, airBlock.ID);

    }
}
