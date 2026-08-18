// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.


// Its BlockRegistry. blocks are created and reused here.
// We can easily create new block or edit existing blocks.
// Tried to make it data oriented for fast iteration and ECS like so we can
// have better moddint campatability.
// the values that should exists in all blocks are SoA by design
// i could have create a big 65k array for each but i think lists are better 
// for modding campatability even if we will have slower loading times.


namespace CharGoosh.Game.Block;

using CharGoosh.Game.Tag;

public static class BlockRegistry
{
    const int INIT_LEN = 10;

    // SoA block properties
    private static readonly List<Tags> _tags = new(INIT_LEN);
    private static readonly List<uint> _meshIds = new(INIT_LEN);
    private static readonly List<uint> _textureIds = new(INIT_LEN);
    private static readonly List<ushort> _healths = new(INIT_LEN); // 0 means infinite
    private static readonly List<ushort> _destroyTickTimes = new(INIT_LEN); // 0 means infinite
    private static readonly List<byte> _hardness = new(INIT_LEN); // 0 mean brakable with anything

    // give blocks a name so we dont loose them later
    private static readonly Dictionary<string, int> _nameToId = new(INIT_LEN);
    private static int _counter = 0;

    static BlockRegistry()
    {
        var block = AddBlock();
        block.Name("air");
    }

    public static Block RegisterBlock()
    {
        return AddBlock();
    }

    private static Block AddBlock()
    {

        _tags.Add(new Tags());
        _meshIds.Add(0);
        _textureIds.Add(0);
        _healths.Add(0);
        _destroyTickTimes.Add(0);
        _hardness.Add(0);

        return new Block(_counter++);
    }

    public static Block SetTextureID(Block block, uint textureID)
    {
        if (_counter > block.ID)
        {
            _textureIds[block.ID] = textureID;
        }
        return block;
    }
    public static uint GetTextureID(Block block)
    {
        if (_counter > block.ID)
        {
            return _textureIds[block.ID];
        }
        return 0;
    }

    public static Block SetMeshID(Block block, uint meshID)
    {

        if (_counter > block.ID)
        {
            _meshIds[block.ID] = meshID;
        }
        return block;
    }
    public static uint GetMeshID(Block block)
    {
        if (_counter > block.ID)
        {
            return _meshIds[block.ID];
        }
        return 0;
    }

    public static Block SetDestroyTime(Block block, ushort destroyTickTime)
    {

        if (_counter > block.ID)
        {
            _destroyTickTimes[block.ID] = destroyTickTime;
        }
        return block;
    }
    public static ushort GetDestroyTime(Block block)
    {
        if (_counter > block.ID)
        {
            return _destroyTickTimes[block.ID];
        }
        return 0;
    }

    public static Block SetBlockName(Block block, string name)
    {
        _nameToId[name] = block.ID;
        return block;
    }

    public static Block GetBlockFromName(string name)
    {
        if (_nameToId.TryGetValue(name, out int val))
        {
            return new Block(val);
        }
        return new Block(0);
    }

    // simple method to set a tag to a block.
    public static Block SetTag(Block block, string tag)
    {
        _tags[block.ID].Add(tag);
        return block;
    }

    // returns the tags of a block for better functionality.
    public static Tags GetTags(Block block)
    {
        return _tags[block.ID];
    }
}

