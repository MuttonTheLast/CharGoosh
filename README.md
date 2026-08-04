# CharGoosh
Its a simple voxel engine. we will see what will happen.

## What is it?
This is a **learning exercise** where I **intend** to design with **modding** in mind.\
The backend is [**MoonWorks**](https://github.com/MoonsideGames/MoonWorks)
which is basically lower level version of\
[**FNA-XNA**](https://fna-xna.github.io/).
Because **MoonWorks** is made with [SDL](https://github.com/libsdl-org/SDL),
it should be cross-platform.

However, primary testing and support will focus on **_Linux and Windows._**

## What is CharGoosh
> [!NOTE]
> **CharGoosh** is a persian word that means: **Rectangle**

## Dependencies

### MoonWorks
this project uses [**MoonWorks**](https://github.com/MoonsideGames/MoonWorks) as main library.
and it has dependencies. MoonWorks dotnet .dll is in project prebuilt in lib/dotnet/ folder.
you can build and replace it.
> these are MoonWorks dependencies.
* [SDL3](https://github.com/flibitijibibo/SDL3-CS) - Window management, Input, Graphics
* [IRO](https://github.com/MoonsideGames/IRO) - Image Loading
* [FAudio](https://github.com/FNA-XNA/FAudio) - Audio
* [Wellspring](https://github.com/MoonsideGames/Wellspring) - Font Rendering
* [dav1dfile](https://github.com/MoonsideGames/dav1dfile) - Compressed Video

Prebuilt native dependencies can be obtained here: https://moonside.games/files/moonlibs.tar.gz

## License
The license is GPLv3 that you can see in [LICENSE](./LICENSE).

That means it should be open source if you publish it on internet.

You can create modules and modules be closed source but any change to my code 
should be open source.

You can publish it and take money in any market but it could be open source 
and not use the names that are in trademark sections of [LICENSE](./LICENSE)

### Third Party
This project uses third party tools and licenses are different so be careful 
if you want to change core libs.

> Full information on [LICENSE](./LICENSE) and [GPLv3](./GPL3-LICENSE).
