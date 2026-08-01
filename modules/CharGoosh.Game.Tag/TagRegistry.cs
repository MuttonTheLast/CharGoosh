
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace CharGoosh.Game.Tag;


// just a name registery for tags
// GetHash
public static class TagRegistery
{
    // hash to name
    private readonly static Dictionary<ulong, string> _names = new();

    // this method gives tag hash a name
    // usefull when debugging and want to print all tags of an object
    public static bool Register(string name)
    {
        var hash = GetHash(name);
        _names[hash] = name;
        return true;
    }

    /// returns the hash uint64 of a string
    /// better to call it once and reuse it
    public static ulong GetHash(string name)
    {
        name = name.ToLower();
        ReadOnlySpan<byte> data = MemoryMarshal.AsBytes(name.AsSpan());
        return XxHash64.HashToUInt64(data);
    }

    /// returns the the Tag struct of a tag
    /// better to call it once and reuse it
    public static Tag GetTag(string name)
    {
        return new Tag(GetHash(name));
    }

    public static string GetName(ulong tagHash)
    {
        if (_names.TryGetValue(tagHash, out string? name))
        {
            return name;
        }
        return tagHash.ToString();
    }
}

