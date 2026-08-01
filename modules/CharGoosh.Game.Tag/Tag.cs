// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.


namespace CharGoosh.Game.Tag;

public struct Tag(ulong hash)
{
    public readonly ulong Hash = hash;

    /// Gets hash name. try to call it once and use many times
    public string Name => TagRegistery.GetName(Hash);

}

