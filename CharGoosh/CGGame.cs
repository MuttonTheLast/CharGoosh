using System;
using MoonWorks;
using MoonWorks.Graphics;

namespace CharGoosh;


public class CGGame : Game
{
    public bool Debug { get; private set; }

    public CGGame(AppInfo appInfo, WindowCreateInfo windowCreateInfo,
            FramePacingSettings framePacingSettings, ShaderFormat availableShaderFormats,
            bool debugMode = false) :
        base(appInfo, windowCreateInfo, framePacingSettings, availableShaderFormats, debugMode)
    {
        Debug = debugMode;
    }


    protected override void Step()
    {
    }

    protected override void Update(TimeSpan delta)
    {

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

            cmdbuf.EndRenderPass(renderPass);
        }
        GraphicsDevice.Submit(cmdbuf);
    }

    protected override void Destroy()
    {
        Console.WriteLine("Destroy Called :) im happy");
    }
}
