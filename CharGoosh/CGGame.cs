using System;
using System.Numerics;
using System.Runtime.InteropServices;
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

    public bool Debug { get; private set; }

    public CGGame(AppInfo appInfo, WindowCreateInfo windowCreateInfo,
            FramePacingSettings framePacingSettings, ShaderFormat availableShaderFormats,
            bool debugMode = false) :
        base(appInfo, windowCreateInfo, framePacingSettings, availableShaderFormats, debugMode)
    {
        Debug = debugMode;


        Shader vertex_shader = ShaderCross.Create(GraphicsDevice, RootTitleStorage,
                SHADERS_PATH + "PositionColorTex.vert.hlsl", "VS_Main",
                ShaderCross.ShaderFormat.HLSL, ShaderStage.Vertex, Debug, null, SHADERS_PATH);

        Shader pixel_shader = ShaderCross.Create(GraphicsDevice, RootTitleStorage,
                SHADERS_PATH + "PositionColorTex.pixel.hlsl", "PS_Main",
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
            VertexInputState = VertexInputState.CreateSingleBinding<PositionColorTexVertex>(),
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

        ReadOnlySpan<PositionColorTexVertex> vertexData = [
            new PositionColorTexVertex(new Vector3(-0.5f,  0.5f, 0),Color.White, new Vector2(0, 0)),
            new PositionColorTexVertex(new Vector3( 0.5f,  0.5f, 0),Color.White, new Vector2(2, 0)),
            new PositionColorTexVertex(new Vector3( 0.5f, -0.5f, 0),Color.White, new Vector2(2, 2)),
            new PositionColorTexVertex(new Vector3(-0.5f, -0.5f, 0), Color.White, new Vector2(0, 2)),
        ];

        ReadOnlySpan<ushort> indexData = [
            0, 1, 2,
            0, 2, 3,
        ];

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

            renderPass.BindGraphicsPipeline(DrawPipeline);
            renderPass.BindVertexBuffers(vertexBuffer);
            renderPass.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
            renderPass.BindFragmentSamplers(new TextureSamplerBinding(textures[currentTextureIndex], samplers[currentSamplerIndex]));
            renderPass.DrawIndexedPrimitives(6, 1, 0, 0, 0);
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
struct PositionColorTexVertex(Vector3 position, Color color, Vector2 uv) : IVertexType
{
    [FieldOffset(0)]
    public Vector3 Position = position;

    [FieldOffset(12)]
    public Color Color = color;

    [FieldOffset(28)]
    public Vector2 UV = uv;

    public static VertexElementFormat[] Formats { get; } = [
        VertexElementFormat.Float3,
        VertexElementFormat.Ubyte4Norm,
        VertexElementFormat.Float2,
    ];

    public static uint[] Offsets { get; } = [
        0, 12, 28
    ];

    public override readonly string ToString()
    {
        return $"Positon: {Position}, Color: {Color}, UV: {UV}";
    }
}
