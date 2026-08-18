// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

namespace CharGoosh.Game.Tag;

public struct Tags()
{
    private HashSet<ulong> _tags = new();

    public void Add(ulong hash)
    {
        _tags.Add(hash);
    }

    public void Add(Tag tag)
    {
        Add(tag.Hash);
    }

    public void Add(string tag)
    {
        Add(TagRegistery.GetHash(tag));
    }

    public bool Contains(ulong hash)
    {
        return _tags.Contains(hash);
    }

    public bool Contains(Tag tag)
    {
        return Contains(tag.Hash);
    }
    public bool Contains(string tag)
    {
        return Contains(TagRegistery.GetHash(tag));
    }

    public void Remove(ulong hash)
    {
        _tags.Remove(hash);
    }

    public void Remove(Tag tag)
    {
        Remove(tag.Hash);
    }

    public void Remove(string tag)
    {
        Remove(TagRegistery.GetHash(tag));
    }
}


