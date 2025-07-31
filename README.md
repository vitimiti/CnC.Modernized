# CnC.Modernized

This is a dotnet and modernized implementation of the open-sourced engine from EA Electronics' repository of the Command
and Conquer games.

## Aims

This project aims to create libraries and executables that allow running the original game assets and mods/games based
on the original games/engines or with custom assets with a modernized and cross-platform engine written in dotnet.

This project will create a launcher that would allow to run all these games and mods, considering each game a mod of its
own, including the original games.

This project will **NOT** aim to maintain online play compatibility with the original games. This compatibility will not
actively be broken and will be maintained, as long as it doesn't stop the progress of the modernization and adaptation
of the original code.

## Projects

### CnC.Modernized.GeneralsAndZeroHour.Compression.Eac

An implementation of the original EAC compression algorithms and codecs from the C&C Generals and Zero Hour games. See
its [readme](CnC.Modernized.GeneralsAndZeroHour.Compression.Eac/README.md) for more information.

### CnC.Modernized.Sdl3

A safe and managed library that imports SDL3 for use with the CnC.Modernized projects. See
its [readme](CnC.Modernized.Sdl3/README.md) for more information.