
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


