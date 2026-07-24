

using System;
using System.Collections.Generic;
using MoonWorks.Graphics;
using MoonWorks.Storage;
using Serilog;

namespace CharGoosh.Graphics;

public class ResourceManager : IDisposable
{

    readonly Dictionary<string, uint> _nameToTextureID = [];
    readonly Dictionary<string, uint> _nameToMeshID = [];

    public readonly TextureAtlasManager TextureAtlasManager;
    public readonly MeshManager MeshManager;

    bool disposed = false;
    public ResourceManager(GraphicsDevice gd, TitleStorage titleStorage, bool debug = false)
    {
        TextureAtlasManager = new TextureAtlasManager(gd, titleStorage, 16, 2048, 65535, debug);
        MeshManager = new MeshManager(gd, titleStorage);
    }

    /// returns resource id of specific type.
    /// types are identified by 'type:resourceName'.
    /// if there is more than two : then will return 0.
    public uint GetResourceID(string resourceName)
    {
        uint result = 0;
        if (TryParseName(resourceName, out string[] data))
        {
            return result;
        }
        string type = data[0];
        string name = data[1];

        switch (type)
        {
            case "texture":
                _nameToTextureID.TryGetValue(name, out result);
                break;
            case "mesh":
                _nameToMeshID.TryGetValue(name, out result);
                break;
            default:
                Log.Warning("Tried to get resource type that does not exists: {0}",
                        type);
                return 0;
        }
        return result;
    }

    public bool AddResource(string resourceName, uint id)
    {

        if (TryParseName(resourceName, out string[] data))
        {
            return false;
        }
        string type = data[0];
        string name = data[1];

        switch (type)
        {
            case "texture":
                AddTexture(name, id);
                break;
            case "mesh":
                AddMesh(name, id);
                break;
            default:
                Log.Warning("Treied to Add resource type that does not exists: {0}", type);
                return false;
        }
        return true;
    }

    public void AddTexture(string name, uint id)
    {
        _nameToTextureID[name] = id;
    }

    public void AddMesh(string name, uint id)
    {
        _nameToMeshID[name] = id;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static string[] ParseName(string name)
    {
        string[] formattedName = name.Split(':');
        if (formattedName.Length != 2)
        {
            return [];
        }
        return formattedName;
    }

    public static bool TryParseName(string name, out string[] result)
    {
        result = [];
        string[] formattedName = name.Split(':');
        if (formattedName.Length == 2)
        {
            result = formattedName;
            return true;
        }
        return false;
    }

    void Dispose(bool disposing)
    {

        if (disposed)
        {
            return;
        }
        if (disposing)
        {
            MeshManager.Dispose();
            TextureAtlasManager.Dispose();
        }

        disposed = false;
    }

}
