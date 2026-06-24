# Collage Generator

A WinUI 3 desktop collage generator written in C#. It supports manual image selection, folder and recursive folder loading, random folder selections, persistent favorite folders, live preview, layout customization, and export to common bitmap formats.

## Build

```powershell
dotnet restore
dotnet build -f net8.0-windows10.0.22621.0
```

This project targets Windows and requires the Windows App SDK workload/runtime.


## Publish a self-contained Windows executable

Use this command from the repository root to produce a self-contained Win x64 publish output:

```powershell
dotnet publish -c Release -f net8.0-windows10.0.22621.0 -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/win-x64-self-contained
```

The executable will be written to `publish/win-x64-self-contained/CollageGenerator.exe`.
