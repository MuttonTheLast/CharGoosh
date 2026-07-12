using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CharGoosh.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace CharGoosh;

using Buffer = MoonWorks.Graphics.Buffer;
public class CGGame : Game
{


    readonly static string SHADERS_PATH = "assets/shaders/";


    readonly static string[] TEXTURE_PATHS = [
        "assets/textures/invalid.png",
        "assets/textures/green.png",
        "assets/textures/gray.png",
        "assets/textures/brown.png",
        "assets/textures/blue.png",
        "assets/textures/red.png",
        "assets/textures/purple.png",

    ];
    readonly Texture[] textures = new Texture[TEXTURE_PATHS.Length];

    readonly Sampler[] samplers = new Sampler[6];


    readonly Buffer vertexBuffer;
    readonly Buffer indexBuffer;

    readonly GraphicsPipeline DrawPipeline;

    int currentTextureIndex = 0;
    int currentSamplerIndex = 0;

    readonly TextureAtlasManager textureAtlasManager;
    public bool Debug { get; private set; }

    public CGGame(AppInfo appInfo, WindowCreateInfo windowCreateInfo,
            FramePacingSettings framePacingSettings, ShaderFormat availableShaderFormats,
            bool debugMode = false) :
        base(appInfo, windowCreateInfo, framePacingSettings, availableShaderFormats, debugMode)
    {
        Debug = debugMode;


        Shader vertex_shader = ShaderCross.Create(GraphicsDevice, RootTitleStorage,
                SHADERS_PATH + "PositionColorAtlas.vert.hlsl", "VS_Main",
                ShaderCross.ShaderFormat.HLSL, ShaderStage.Vertex, Debug, null, SHADERS_PATH);

        Shader pixel_shader = ShaderCross.Create(GraphicsDevice, RootTitleStorage,
                SHADERS_PATH + "PositionColorAtlas.pixel.hlsl", "PS_Main",
                ShaderCross.ShaderFormat.HLSL, ShaderStage.Fragment, Debug, null, SHADERS_PATH);

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
                ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionColorAtlas>(),
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

        ReadOnlySpan<PositionColorAtlas> vertexData = [
            new PositionColorAtlas(new Vector3(-0.5f,  0.5f, 0), Color.White, 0, 1),
            new PositionColorAtlas(new Vector3( 0.5f,  0.5f, 0), Color.White, 1, 1),
            new PositionColorAtlas(new Vector3( 0.5f, -0.5f, 0), Color.White, 3, 1),
            new PositionColorAtlas(new Vector3(-0.5f, -0.5f, 0), Color.Black, 2, 1),
            new PositionColorAtlas(new Vector3(0.5f, 1.0f, 0), Color.White, 0, 2),
            new PositionColorAtlas(new Vector3(1.0f,  1.0f, 0), Color.White, 1, 2),
            new PositionColorAtlas(new Vector3(1.0f, 0.5f, 0), Color.White, 3, 2),
            new PositionColorAtlas(new Vector3(0.5f, 0.5f, 0), Color.Black, 2, 2),

        ];

        ReadOnlySpan<ushort> indexData = [
            0, 1, 2,
            0, 2, 3,

            4, 5, 6,
            4, 6, 7,
        ];

        textureAtlasManager = new TextureAtlasManager(GraphicsDevice, RootTitleStorage,
                16, 256, ushort.MaxValue, Debug);
        textureAtlasManager.RequestAddTexture("assets/textures/invalid.png");
        textureAtlasManager.RequestAddTexture("assets/textures/red.png");
        var resourceUploader = new ResourceUploader(GraphicsDevice);


        vertexBuffer = resourceUploader.CreateBuffer(vertexData, BufferUsageFlags.Vertex);
        indexBuffer = resourceUploader.CreateBuffer(indexData, BufferUsageFlags.Index);

        for (int i = 0; i < TEXTURE_PATHS.Length; i++)
        {
            string path = TEXTURE_PATHS[i];
            if (!RootTitleStorage.Exists(path))
            {
                textures[i] = Texture.Create2D(GraphicsDevice, 16, 16, TextureFormat.R8G8B8A8Unorm,
                        TextureUsageFlags.Sampler);
                continue;
            }
            RootTitleStorage.GetFileSize(path, out ulong size);
            byte[] data = new byte[size];
            Span<byte> data_span = data.AsSpan();
            RootTitleStorage.ReadFile(path, data_span);

            textures[i] = resourceUploader.CreateTexture2DFromCompressed(data_span,
                    TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);
        }
        resourceUploader.Upload();
        resourceUploader.Dispose();


    }


    protected override void Step()
    {
    }

    protected override void Update(TimeSpan delta)
    {

        if (Inputs.Keyboard.IsPressed(KeyCode.A))
        {
            currentTextureIndex = (currentTextureIndex - 1 + textures.Length) % textures.Length;
        }
        if (Inputs.Keyboard.IsPressed(KeyCode.D))
        {
            currentTextureIndex = (currentTextureIndex + 1) % textures.Length;
        }
        if (Inputs.Keyboard.IsPressed(KeyCode.W))
        {
            currentSamplerIndex = (currentSamplerIndex + 1) % samplers.Length;
        }
        if (Inputs.Keyboard.IsPressed(KeyCode.S))
        {
            currentSamplerIndex = (currentSamplerIndex - 1 + samplers.Length) % samplers.Length;
        }
        textureAtlasManager.Update();
    }

    protected override void Draw(double alpha)
    {
        var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
        var swapchain = cmdbuf.AcquireSwapchainTexture(MainWindow);
        if (swapchain != null)
        {
            var renderPass = cmdbuf.BeginRenderPass(
                    new ColorTargetInfo(swapchain, Color.Cyan, true)
                    );

            cmdbuf.PushVertexUniformData(textureAtlasManager.TextureAtlasSize, 0);

            renderPass.BindGraphicsPipeline(DrawPipeline);
            renderPass.BindVertexBuffers(vertexBuffer);
            renderPass.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
            renderPass.BindFragmentSamplers(
                    new TextureSamplerBinding(textureAtlasManager.AtlasArray,
                        samplers[currentSamplerIndex]));
            renderPass.BindVertexStorageBuffers(textureAtlasManager.TextureDataBuffer);
            renderPass.DrawIndexedPrimitives(12, 1, 0, 0, 0);
            cmdbuf.EndRenderPass(renderPass);
        }
        GraphicsDevice.Submit(cmdbuf);

    }

    protected override void Destroy()
    {
        Console.WriteLine("Destroy Called :) im happy");
    }
}

[StructLayout(LayoutKind.Explicit, Size = 36)]
struct PositionColorAtlas(Vector3 position, Color color, uint coordPos, uint tid) : IVertexType
{
    [FieldOffset(0)]
    public Vector3 Position = position;
    [FieldOffset(12)]
    public Color Color = color;
    [FieldOffset(28)]
    public uint CoordPos = coordPos;
    [FieldOffset(32)]
    public uint TID = tid;


    public static VertexElementFormat[] Formats { get; } = [
        VertexElementFormat.Float3,
        VertexElementFormat.Ubyte4Norm,
        VertexElementFormat.Uint,
        VertexElementFormat.Uint,
    ];

    public static uint[] Offsets { get; } = [
        0, 12, 28,32
    ];

    public override readonly string ToString()
    {
        return $"Positon: {Position}, Color: {Color}, CoordPos: {CoordPos}, TID: {TID}";
    }
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
