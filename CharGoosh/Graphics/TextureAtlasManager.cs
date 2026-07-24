// i wont add remove texture because maybe someone needs it (and im lazy)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Storage;
using Serilog;

using Buffer = MoonWorks.Graphics.Buffer;

namespace CharGoosh.Graphics;
// item, result, reason
using AddCallback = Action<AddTextureData, uint, string>;
public class AddTextureData(uint tid, uint atlas, byte[] data, string path = "", AddCallback? cb = null,
        bool loading = false, bool downloading = false)
{
    public readonly uint TID = tid;
    public uint Atlas = atlas;
    public byte[] Data = data;
    public readonly string Path = path;
    public readonly AddCallback? Callback = cb;
    public bool Loading = loading;
    public bool Downloading = downloading;

    public override string ToString()
    {
        return $"TID: {TID}, Atlas: {Atlas}, Path: '{Path}', Size: {Data.Length}b";
    }
}

public class TextureAtlasManager : IDisposable
{

    // Lists
    readonly Queue<AddTextureData> _textureQeueu = [];

    // public private setters
    public Texture AtlasArray { get; private set; }
    public Sampler DefaultSampler { get; private set; }
    public Buffer TextureDataBuffer { get; private set; }


    // private
    Texture _oldTextureAtlas = null!;
    Texture? _textureToAdd = null;
    Fence? _fence = null;
    uint _startingAtlas = 0;
    uint _idCounter = 1;
    bool _extending = false;
    bool _containsData = false;

    // readonly data
    readonly GraphicsDevice _gd;
    readonly TitleStorage _titleStorage;
    readonly Buffer _resultDataBuffer;
    readonly TransferBuffer _resultTB;
    readonly ComputePipeline _addPipeline;
    readonly uint _maxTextures;



    // Guarded variables
    private uint _smallTextureSize;
    public uint SmallTextureSize
    {
        get => _smallTextureSize;
        set => _smallTextureSize = (value < 8) ? 16 : value;
    }

    uint _textureAtlasSize;
    public uint TextureAtlasSize
    {
        get => _textureAtlasSize;
        set
        {
            uint v = value >= 128 ? value : 512;
            _textureAtlasSize = !_containsData || v >= _textureAtlasSize ? v : _textureAtlasSize;
        }
    }


    bool disposed = false;

    public TextureAtlasManager(GraphicsDevice gd, TitleStorage storage,
            uint minTextureSize = 16, uint atlasSize = 2048,
            uint maxTextures = ushort.MaxValue, bool debug = false)
    {
        _titleStorage = storage;
        TextureAtlasSize = atlasSize;
        SmallTextureSize = minTextureSize;
        _maxTextures = maxTextures;
        _gd = gd;

        DefaultSampler = Sampler.Create(_gd, SamplerCreateInfo.PointClamp);

        _addPipeline = ShaderCross.Create(_gd, _titleStorage, "assets/shaders/TextureAtlas.comp.hlsl",
                "CS_Main", ShaderCross.ShaderFormat.HLSL, debug, null, "assets/shaders/");


        AtlasArray = Texture.Create2DArray(_gd, atlasSize, atlasSize, 1,
                TextureFormat.R8G8B8A8Unorm,
                TextureUsageFlags.Sampler | TextureUsageFlags.ComputeStorageRead |
                TextureUsageFlags.ComputeStorageWrite);

        TextureDataBuffer = Buffer.Create<TextureData>(_gd,
               BufferUsageFlags.ComputeStorageWrite | BufferUsageFlags.GraphicsStorageRead,
               maxTextures);
        _resultDataBuffer = Buffer.Create<ResultData>(_gd,
                BufferUsageFlags.ComputeStorageWrite, 1);


        _resultTB = TransferBuffer.Create<ResultData>(_gd,
                TransferBufferUsage.Download, 1);

    }

    public void Update()
    {
        HandleFence();
        HandleTextures();
    }

    internal uint RequestAddTexture(string path, AddCallback? callback = null)
    {
        if (_idCounter >= _maxTextures)
        {
            Log.Warning("Cannot add more textures to texture atlases. " +
            "Some textures will use fallback/default atlas. " +
            "This may cause visual artifacts.");
            return 1;
        }
        _textureQeueu.Enqueue(new AddTextureData(_idCounter, _startingAtlas, [], path, callback));
        return _idCounter++;
    }

    internal uint RequestAddTexture(byte[] data, AddCallback? callback = null)
    {
        if (_idCounter >= _maxTextures)
        {
            Log.Warning("Cannot add more textures to texture atlases. " +
            "Some textures will use fallback/default atlas. " +
            "This may cause visual artifacts.");
            return 1;
        }
        _textureQeueu.Enqueue(new AddTextureData(_idCounter, _startingAtlas, data, "", callback));
        return _idCounter++;
    }

    void HandleFence()
    {
        if (_fence == null)
        {
            return;
        }

        if (!_gd.QueryFence(_fence))
        {
            return;
        }
        _gd.WaitForFence(_fence); // fixes some bugs...
        _gd.ReleaseFence(_fence);
        _fence = null;

        if (_extending)
        {
            Log.Information("TextureAtlas extended.");
            _extending = false;
            return;
        }
        if (_textureQeueu.Count == 0)
        {
            Log.Fatal("texture queue is empty but we have a _fence");
            throw new UnreachableException("texture queue is empty but we have a _fence");
        }

        HandleTextureAddResult();
    }


    void HandleTextures()
    {
        if (_fence != null || _textureQeueu.Count == 0)
        {
            return;
        }

        var item = _textureQeueu.Peek();

        if (!IsTextureDataAvailable(ref item))
        {
            return;
        }
        UploadTextureToAdd(ref item);

        // bounds check
        if (_textureToAdd?.Width >= _textureAtlasSize ||
                _textureToAdd?.Height >= TextureAtlasSize)
        {
            item.Callback?.Invoke(item, 2, "Texture bounds bigger than atlas bounds");
            _textureQeueu.Dequeue();
            HandleTextures();
            return;
        }

        ExecuteComputeData(ref item);
    }

    /// returns true if started downloading and false if downloaded latest
    bool DownloadTextureAddResult(ref AddTextureData item)
    {
        if (!item.Downloading)
        {
            var cmdBuf = _gd.AcquireCommandBuffer();
            var copyPass = cmdBuf.BeginCopyPass();
            var region = new BufferRegion(_resultDataBuffer, 0);

            copyPass.DownloadFromBuffer(region, new TransferBufferLocation(_resultTB, 0));
            cmdBuf.EndCopyPass(copyPass);
            _fence = _gd.SubmitAndAcquireFence(cmdBuf);
            item.Downloading = true;
            Log.Information("Downloading texture atlas result.");
            return true;
        }
        Log.Information("Texture atlas result downloaded.");
        return false;
    }

    /// if data isnt available it will download and wait on an update
    /// if data is available then decides how to optimize things
    void HandleTextureAddResult()
    {
        var item = _textureQeueu.Peek();
        if (DownloadTextureAddResult(ref item))
        {
            return;
        }

        item.Downloading = false;

        var data = _resultTB.Map<ResultData>(false, 0);
        ResultData rd = data[0];
        _resultTB.Unmap();

        if (rd.Result.HasFlag(TextureResult.Failed))
        {
            if (rd.Result.HasFlag(TextureResult.CantFit))
            {
                if (rd.Result.HasFlag(TextureResult.CantFitAnything) &&
                        item.Atlas == _startingAtlas)
                {
                    Log.Information("A texture atlass is filled up. ignoring for next additions.");
                    _startingAtlas++;
                }
                if (item.Atlas == AtlasArray.LayerCountOrDepth - 1)
                {
                    ExtendAtlasArray();
                }
                item.Atlas++;
            }
            return;
        }
        Log.Information("Added texture: {$Data}", item);
        item.Callback?.Invoke(item, 0, "Success");
        _textureToAdd?.Dispose();
        _textureToAdd = null;
        _textureQeueu.Dequeue();
        _containsData = true;
    }


    bool IsTextureDataAvailable(ref AddTextureData item)
    {
        if (item.Loading)
        {
            return false;
        }

        if (item.Path.Length > 0 && item.Data.Length == 0)
        {
            if (!_titleStorage.Exists(item.Path))
            {
                item.Callback?.Invoke(item, 1, "File path does not exists!!");
                _textureQeueu.Dequeue();
                HandleTextures();
                return false;
            }
            item.Loading = true;
            ReadTextureData(item);
            return false;
        }
        return true;
    }

    void UploadTextureToAdd(ref AddTextureData item)
    {
        if (_textureToAdd == null)
        {
            var resourceUploader = new ResourceUploader(_gd);
            _textureToAdd = resourceUploader.CreateTexture2DFromCompressed(item.Data.AsSpan(),
                    TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.ComputeStorageRead);
            Log.Information("Uploading texture to gpu so we can add it to atlas.");
            resourceUploader.UploadAndWait();
            resourceUploader.Dispose();
        }
    }

    void ExecuteComputeData(ref AddTextureData item)
    {

        item.Atlas = item.Atlas > _startingAtlas ? item.Atlas : _startingAtlas;
        var cmdBuf = _gd.AcquireCommandBuffer();
        cmdBuf.PushComputeUniformData(new UniformData
        {
            TID = item.TID,
            AtlasIndex = item.Atlas,
            MinTextureSize = _smallTextureSize,
        }, 0);

        var compPass = cmdBuf.BeginComputePass(
                [new StorageTextureReadWriteBinding(AtlasArray, 0, item.Atlas, false)],
                [new StorageBufferReadWriteBinding( TextureDataBuffer,false),
                 new StorageBufferReadWriteBinding(_resultDataBuffer, false)]
                );

        compPass.BindComputePipeline(_addPipeline);
        compPass.BindSamplers(new TextureSamplerBinding(_textureToAdd, DefaultSampler));
        compPass.Dispatch(1, 1, 1);

        cmdBuf.EndComputePass(compPass);
        _fence = _gd.SubmitAndAcquireFence(cmdBuf);
    }

    async void ReadTextureData(AddTextureData item)
    {
        Log.Information("Reading texture data from disk.");
        if (!_titleStorage.GetFileSize(item.Path, out ulong size))
        {
            Log.Fatal("Tried to load a texture file that does not exists.");
            throw new Exception("Tried to load a file that does not exists!!!");
        }
        item.Data = new byte[size];
        _titleStorage.ReadFile(item.Path, item.Data.AsSpan());
        item.Loading = false;
    }

    void ExtendAtlasArray()
    {
        if (_fence != null)
        {
            return;
        }
        _oldTextureAtlas = AtlasArray;
        var layers = _oldTextureAtlas.LayerCountOrDepth + 1;

        AtlasArray = Texture.Create2DArray(
                _gd, _textureAtlasSize, _textureAtlasSize, layers,
                TextureFormat.R8G8B8A8Unorm,
                TextureUsageFlags.Sampler | TextureUsageFlags.ComputeStorageRead |
                TextureUsageFlags.ComputeStorageWrite, 1);

        var cmdBuf = _gd.AcquireCommandBuffer();
        var copyPass = cmdBuf.BeginCopyPass();

        for (uint i = 0; i < layers - 1; i++)
        {
            var source = new TextureLocation(_oldTextureAtlas) with
            {
                Layer = i,
            };
            var dest = new TextureLocation(AtlasArray) with
            {
                Layer = i,
            };
            copyPass.CopyTextureToTexture(source, dest,
                    _oldTextureAtlas.Width, _oldTextureAtlas.Height, 1, false);
        }

        cmdBuf.EndCopyPass(copyPass);
        _fence = _gd.SubmitAndAcquireFence(cmdBuf);
        _extending = true;
        Log.Information("Extending texture atlas.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }
        if (_fence != null)
        {
            _gd.WaitForFence(_fence);
        }
        if (disposing)
        {
            AtlasArray.Dispose();
            DefaultSampler.Dispose();
            TextureDataBuffer.Dispose();

            _textureQeueu.Clear();
            _oldTextureAtlas.Dispose();
            _textureToAdd?.Dispose();

            _resultDataBuffer.Dispose();
            _resultTB?.Dispose();
            _addPipeline.Dispose();

            _smallTextureSize = 0;
            _textureAtlasSize = 0;
            _fence = null;
        }

        disposed = false;
    }
}

// texture id data
[StructLayout(LayoutKind.Explicit, Size = 24)]
struct TextureData
{

    [FieldOffset(0)]
    public uint TID;
    [FieldOffset(4)]
    public uint Atlas;
    [FieldOffset(8)]
    public uint Position_X;
    [FieldOffset(12)]
    public uint Position_Y;
    [FieldOffset(16)]
    public uint Size_X;
    [FieldOffset(20)]
    public uint Size_Y;
}


[StructLayout(LayoutKind.Explicit, Size = 12)]
struct ResultData
{
    [FieldOffset(0)]
    public uint Position_X;
    [FieldOffset(4)]
    public uint Position_Y;
    [FieldOffset(8)]
    public TextureResult Result;
}

[StructLayout(LayoutKind.Explicit, Size = 12)]
struct UniformData
{
    [FieldOffset(0)]
    public uint TID;
    [FieldOffset(4)]
    public uint AtlasIndex;
    [FieldOffset(8)]
    public uint MinTextureSize;
}

[Flags]
public enum TextureResult : uint
{
    Success = 0,
    Failed = 1 << 0,
    CantFit = 1 << 1,
    CantFitAnything = 1 << 2,
}


