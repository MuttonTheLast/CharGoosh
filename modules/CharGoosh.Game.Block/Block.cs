// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

using CharGoosh.Game.Tag;

namespace CharGoosh.Game.Block;

public struct Block(int id)
{
    public readonly int ID = id;
    public Tags Tags => BlockRegistry.GetTags(this);

    public uint TextureID()
    {
        return BlockRegistry.GetTextureID(this);
    }
    public Block TextureID(uint textureID)
    {
        return BlockRegistry.SetTextureID(this, textureID);
    }

    public uint MeshID()
    {
        return BlockRegistry.GetMeshID(this);
    }
    public Block MeshID(uint meshID)
    {
        return BlockRegistry.SetMeshID(this, meshID);
    }

    public ushort DestroyTime()
    {
        return BlockRegistry.GetDestroyTime(this);
    }
    public Block DestroyTime(ushort destroyTime)
    {
        return BlockRegistry.SetDestroyTime(this, destroyTime);
    }

    public Block Name(string name)
    {
        return BlockRegistry.SetBlockName(this, name);
    }

    // returns a block based on its name. if not found returns block 0
    public static Block FromName(string name)
    {
        return BlockRegistry.GetBlockFromName(name);
    }

}
