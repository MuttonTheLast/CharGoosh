// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CharGoosh.Graphics.Vertex;
using CharGoosh.Graphics.Resource;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using Serilog;

namespace CharGoosh;

using Buffer = MoonWorks.Graphics.Buffer;
using Camera = Game.Camera;
using PrimitiveType = MoonWorks.Graphics.PrimitiveType;
public class CGGame : MoonWorks.Game
{

    readonly static string BASE_PATH;
    readonly static string SHADERS_PATH;
    readonly static string SHADERS_FULL_PATH;



    readonly Sampler[] samplers = new Sampler[6];


    readonly Buffer vertexBuffer;
    readonly Buffer indexBuffer;

    readonly GraphicsPipeline DrawPipeline;

    Texture depthTexture;

    int currentSamplerIndex = 0;

    readonly Camera _cam = new();

    public readonly ResourceManager ResourceManager;
    public bool Debug { get; private set; }

    public Logger.Logger Logger;

    bool depthOnly = false;

    // debug things
    readonly uint woodimg = 0;

    static CGGame()
    {
        BASE_PATH = SDL3.SDL.SDL_GetBasePath();
        SHADERS_PATH = "assets/shaders/";
        SHADERS_FULL_PATH = BASE_PATH + SHADERS_PATH;
    }

    public CGGame(AppInfo appInfo, WindowCreateInfo windowCreateInfo,
            FramePacingSettings framePacingSettings, ShaderFormat availableShaderFormats,
            bool debugMode = false) :
        base(appInfo, windowCreateInfo, framePacingSettings, availableShaderFormats, debugMode)
    {
        Debug = debugMode;
        this.Logger = new Logger.Logger();
        this.Logger.SetGlobal();
        // ResourceManager.TextureAtlasManager = new TextureAtlasManager(GraphicsDevice, RootTitleStorage,
        //         16, 256, ushort.MaxValue, Debug);
        //_meshManager = new MeshManager(GraphicsDevice, RootTitleStorage);

        this.ResourceManager = new ResourceManager(GraphicsDevice, RootTitleStorage, Debug);
        ResourceManager.TextureAtlasManager.RequestAddTexture("assets/textures/invalid.png");
        ResourceManager.TextureAtlasManager.RequestAddTexture("assets/textures/red.png");
        ResourceManager.TextureAtlasManager.RequestAddTexture("assets/textures/white.png");
        woodimg = ResourceManager.TextureAtlasManager.RequestAddTexture("assets/textures/wood.png");


        // NOTE: AI Generated Data
        ReadOnlySpan<MeshDataGPU> cubeData = [
            // ---------- FRONT (Z = -1) ----------
            new MeshDataGPU(new Vector3(-1,-1,-1), new Vector3(0,0,-1), new Vector2(0,0)),
            new MeshDataGPU(new Vector3( 1,-1,-1), new Vector3(0,0,-1), new Vector2(1,0)),
            new MeshDataGPU(new Vector3( 1, 1,-1), new Vector3(0,0,-1), new Vector2(1,1)),
            new MeshDataGPU(new Vector3(-1, 1,-1), new Vector3(0,0,-1), new Vector2(0,1)),

            // ---------- BACK (Z = +1) ----------
            new MeshDataGPU(new Vector3( 1,-1, 1), new Vector3(0,0,1), new Vector2(0,0)),
            new MeshDataGPU(new Vector3(-1,-1, 1), new Vector3(0,0,1), new Vector2(1,0)),
            new MeshDataGPU(new Vector3(-1, 1, 1), new Vector3(0,0,1), new Vector2(1,1)),
            new MeshDataGPU(new Vector3( 1, 1, 1), new Vector3(0,0,1), new Vector2(0,1)),

            // ---------- LEFT (X = -1) ----------
            new MeshDataGPU(new Vector3(-0.5f,-1, 1), new Vector3(-1,0,0), new Vector2(0,0)),
            new MeshDataGPU(new Vector3(-0.5f,-1,-1), new Vector3(-1,0,0), new Vector2(1,0)),
            new MeshDataGPU(new Vector3(-0.5f, 1,-1), new Vector3(-1,0,0), new Vector2(1,1)),
            new MeshDataGPU(new Vector3(-0.5f, 1, 1), new Vector3(-1,0,0), new Vector2(0,1)),

            // ---------- RIGHT (X = +1) ----------
            new MeshDataGPU(new Vector3( 1,-1,-1), new Vector3(1,0,0), new Vector2(0,0)),
            new MeshDataGPU(new Vector3( 1,-1, 1), new Vector3(1,0,0), new Vector2(1,0)),
            new MeshDataGPU(new Vector3( 1, 1, 1), new Vector3(1,0,0), new Vector2(1,1)),
            new MeshDataGPU(new Vector3( 1, 1,-1), new Vector3(1,0,0), new Vector2(0,1)),

            // ---------- BOTTOM (Y = -1) ----------
            new MeshDataGPU(new Vector3(-1,-1, 1), new Vector3(0,-1,0), new Vector2(0,0)),
            new MeshDataGPU(new Vector3( 1,-1, 1), new Vector3(0,-1,0), new Vector2(1,0)),
            new MeshDataGPU(new Vector3( 1,-1,-1), new Vector3(0,-1,0), new Vector2(1,1)),
            new MeshDataGPU(new Vector3(-1,-1,-1), new Vector3(0,-1,0), new Vector2(0,1)),

            // ---------- TOP (Y = +1) ----------
            new MeshDataGPU(new Vector3(-1, 1,-1), new Vector3(0,1,0), new Vector2(0,0)),
            new MeshDataGPU(new Vector3( 1, 1,-1), new Vector3(0,1,0), new Vector2(1,0)),
            new MeshDataGPU(new Vector3( 1, 1, 1), new Vector3(0,1,0), new Vector2(1,1)),
            new MeshDataGPU(new Vector3(-1, 1, 1), new Vector3(0,1,0), new Vector2(0,1))
        ];

        // NOTE: AI Generated
        ReadOnlySpan<uint> indexData = [
            // Front
            0, 2, 1,  0, 3, 2,

            // Back
            4, 6, 5,  4, 7, 6,

            // Left
            8, 10, 9,  8, 11, 10,

            // Right
            12, 14, 13,  12, 15, 14,

            // Bottom
            16, 18, 17,  16, 19, 18,

            // Top
            20, 22, 21,  20, 23, 22
        ];
        var cubeMeshId = ResourceManager.MeshManager.AddMesh(cubeData, indexData);


        Shader vertex_shader = ShaderCross.Create(GraphicsDevice, RootTitleStorage,
                SHADERS_PATH + PositionMeshIndexAtlas.VertexShaderName, "VS_Main",
                ShaderCross.ShaderFormat.HLSL, ShaderStage.Vertex,
                Debug, null, SHADERS_FULL_PATH);

        Shader pixel_shader = ShaderCross.Create(GraphicsDevice, RootTitleStorage,
                SHADERS_PATH + PositionMeshIndexAtlas.PixelShaderName, "PS_Main",
                ShaderCross.ShaderFormat.HLSL, ShaderStage.Fragment,
                Debug, null, SHADERS_FULL_PATH);

        var pci = new GraphicsPipelineCreateInfo
        {

            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = MainWindow.SwapchainFormat,
                        BlendState = ColorTargetBlendState.Opaque
                    }
                ],
                HasDepthStencilTarget = true,
                DepthStencilFormat = TextureFormat.D16Unorm,
            },
            DepthStencilState = new DepthStencilState
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = CompareOp.LessOrEqual,
            },
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CW_CullBack,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionMeshIndexAtlas>(),
            VertexShader = vertex_shader,
            FragmentShader = pixel_shader,
        };
        DrawPipeline = GraphicsPipeline.Create(GraphicsDevice, pci);

        samplers[0] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);
        samplers[1] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointWrap);
        samplers[2] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);
        samplers[3] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearWrap);
        samplers[4] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.AnisotropicClamp);
        samplers[5] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.AnisotropicWrap);

        depthTexture = Texture.Create2D(GraphicsDevice, MainWindow.Width, MainWindow.Height,
                TextureFormat.D16Unorm,
                TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler);

        ReadOnlySpan<PositionMeshIndexAtlas> vertexData = [
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 0,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 1,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 2,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 3,woodimg),

            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 4,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 5,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 6,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 7,woodimg),

            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 8,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 9,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 10,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 11,woodimg),

            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 12,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 13,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 14,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 15,woodimg),

            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 16,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 17,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 18,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 19,woodimg),

            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 20,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 21,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 22,woodimg),
            new PositionMeshIndexAtlas(new Vector3(0,0,0), 0, 23,woodimg),

        ];




        var resourceUploader = new ResourceUploader(GraphicsDevice);


        vertexBuffer = resourceUploader.CreateBuffer(vertexData, BufferUsageFlags.Vertex);
        indexBuffer = resourceUploader.CreateBuffer(
                ResourceManager.MeshManager.GetMeshVisibleIndices(cubeMeshId, MeshCullingFace.Back | MeshCullingFace.Top | MeshCullingFace.Front),
                BufferUsageFlags.Index);

        resourceUploader.Upload();
        resourceUploader.Dispose();

        MainWindow.SetRelativeMouseMode(true);


        MainWindow.RegisterSizeChangeCallback(WindowSizeChanged);
        DoTest();

        pixel_shader.Dispose();
        vertex_shader.Dispose();
    }

    private void WindowSizeChanged(uint width, uint height)
    {
        Log.Information("Window size changed. width: {width}, height: {height}.", width, height);
        depthTexture.Dispose();
        depthTexture = Texture.Create2D(GraphicsDevice, width, height, TextureFormat.D16Unorm,
                TextureUsageFlags.Sampler | TextureUsageFlags.DepthStencilTarget);
    }

    protected override void Step()
    {
    }

    protected override void Update(TimeSpan delta)
    {
        _cam.Transform.Scale.X = MainWindow.Width;
        _cam.Transform.Scale.Y = MainWindow.Height;
        _cam.Transform.Scale.Z = 100.0f;

        if (Inputs.Keyboard.IsDown(KeyCode.A))
        {
            _cam.Transform.Position -= _cam.Transform.Right * 20 * delta.Milliseconds / 1000.0f;
        }
        if (Inputs.Keyboard.IsDown(KeyCode.D))
        {
            _cam.Transform.Position += _cam.Transform.Right * 20 * delta.Milliseconds / 1000.0f;

        }
        if (Inputs.Keyboard.IsDown(KeyCode.W))
        {
            _cam.Transform.Position += _cam.Transform.Forward * 20 * delta.Milliseconds / 1000.0f;

            //currentSamplerIndex = (currentSamplerIndex + 1) % samplers.Length;
        }
        if (Inputs.Keyboard.IsDown(KeyCode.S))
        {
            _cam.Transform.Position -= _cam.Transform.Forward * 20 * delta.Milliseconds / 1000.0f;

            //currentSamplerIndex = (currentSamplerIndex - 1 + samplers.Length) % samplers.Length;
        }

        float sensitivity = 0.5f;

        float dx = Inputs.Mouse.DeltaX;
        float dy = Inputs.Mouse.DeltaY;

        float yaw = dx * sensitivity * delta.Milliseconds / 1000.0f; // yaw
        float pitch = dy * sensitivity * delta.Milliseconds / 1000.0f; // yaw

        Quaternion yawQ = Quaternion.CreateFromAxisAngle(_cam.Transform.Up, yaw);
        Quaternion pitchQ = Quaternion.CreateFromAxisAngle(_cam.Transform.Right, pitch);

        _cam.Transform.Rotation = pitchQ * yawQ * _cam.Transform.Rotation;


        //e.Y += Inputs.Mouse.DeltaY * sensitivity * delta.Milliseconds / 1000.0f; // pitch
        ResourceManager.TextureAtlasManager.Update();
    }

    protected override void Draw(double alpha)
    {
        var proj = _cam.Projection;
        var view = _cam.ViewMatrix;

        var viewProj = Matrix4x4.Transpose(view * proj);
        var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
        var swapchain = cmdbuf.AcquireSwapchainTexture(MainWindow);
        if (swapchain != null)
        {
            var depthTarget = new DepthStencilTargetInfo(depthTexture, 1.0f, 0, true);
            var renderPass = cmdbuf.BeginRenderPass(
                    in depthTarget,
                    new ColorTargetInfo(swapchain, Color.LightGray, true)
                    );

            cmdbuf.PushVertexUniformData(
                    new VertexUniform(viewProj,
                        ResourceManager.TextureAtlasManager.TextureAtlasSize), 0);

            renderPass.BindGraphicsPipeline(DrawPipeline);
            renderPass.BindVertexBuffers(vertexBuffer);
            renderPass.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
            renderPass.BindFragmentSamplers(
                    new TextureSamplerBinding(ResourceManager.TextureAtlasManager.AtlasArray,
                        samplers[currentSamplerIndex]));
            renderPass.BindVertexStorageBuffers([
                    ResourceManager.TextureAtlasManager.TextureDataBuffer,
                    ResourceManager.MeshManager.Meshes
            ]);
            renderPass.DrawIndexedPrimitives(indexBuffer.Size / sizeof(ushort), 1, 0, 0, 0);
            cmdbuf.EndRenderPass(renderPass);
        }
        GraphicsDevice.Submit(cmdbuf);

    }

    protected override void Destroy()
    {
    }

    // just a method that im testing what i want to do
    void DoTest()
    {
    }
}

// [StructLayout(LayoutKind.Explicit, Size = 36)]
// struct PositionColorAtlas(Vector3 position, Color color, uint coordPos, uint tid) : IVertexType
// {
//     [FieldOffset(0)]
//     public Vector3 Position = position;
//     [FieldOffset(12)]
//     public Color Color = color;
//     [FieldOffset(28)]
//     public uint CoordPos = coordPos;
//     [FieldOffset(32)]
//     public uint TID = tid;
//
//
//     public static VertexElementFormat[] Formats { get; } = [
//         VertexElementFormat.Float3,
//         VertexElementFormat.Ubyte4Norm,
//         VertexElementFormat.Uint,
//         VertexElementFormat.Uint,
//     ];
//
//     public static uint[] Offsets { get; } = [
//         0, 12, 28,32
//     ];
//
//     public override readonly string ToString()
//     {
//         return $"Positon: {Position}, Color: {Color}, CoordPos: {CoordPos}, TID: {TID}";
//     }
// }


[StructLayout(LayoutKind.Explicit, Size = 68)]
struct VertexUniform(Matrix4x4 mat, uint atlasSize)
{
    [FieldOffset(0)]
    public Matrix4x4 Mat = mat; // 64 - c0
    [FieldOffset(64)]
    public uint AtlasSize = atlasSize; // 64-> 4 c1.x
}

// [StructLayout(LayoutKind.Explicit, Size = 36)]
// struct PositionColorTexVertex(Vector3 position, Color color, Vector2 uv) : IVertexType
// {
//     [FieldOffset(0)]
//     public Vector3 Position = position;
//
//     [FieldOffset(12)]
//     public Color Color = color;
//
//     [FieldOffset(28)]
//     public Vector2 UV = uv;
//
//     public static VertexElementFormat[] Formats { get; } = [
//         VertexElementFormat.Float3,
//         VertexElementFormat.Ubyte4Norm,
//         VertexElementFormat.Float2,
//     ];
//
//     public static uint[] Offsets { get; } = [
//         0, 12, 28
//     ];
//
//     public override readonly string ToString()
//     {
//         return $"Positon: {Position}, Color: {Color}, UV: {UV}";
//     }
// }
