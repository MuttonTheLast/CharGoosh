// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

using System;
using MoonWorks;
using MoonWorks.Graphics;

namespace CharGoosh;

class Program
{
    static void Main(string[] args)
    {

        var appInfo = new AppInfo("Fig", "CharGoosh");
        var wcInfo = new WindowCreateInfo("CharGoosh", 800, 600, ScreenMode.Windowed, true,
                false, false);
        var framePacingSettings = FramePacingSettings.CreateUncapped(60, 60);
        var shaderFormats = ShaderFormat.SPIRV /*| ShaderFormat.DXBC | ShaderFormat.DXIL*/;
        var debug = false;
#if DEBUG 
        debug = true;
#endif
        var game = new CGGame(appInfo, wcInfo, framePacingSettings, shaderFormats, debug);
        game.Run();
    }
}
