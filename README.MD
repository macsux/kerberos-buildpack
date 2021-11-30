This project offers simplified way of creating buildpacks for Cloud Foundry in .NET

Buildpacks fall into two categories: supply and final. Supply buildpacks can be chained to supply dependencies to the app or modify its configuration. They essentially act as middleware. Final buildpack is the one that is launched in the end of the buildpack chain and is responsible for telling Cloud Foundry the startup command for the application.

## Quick Start

This is a template project that can be used by DotNet CLI to boostrap a new buildpack. Install it into CLI via

```shell
dotnet new -i CloudFoundry.Buildpack.V2
```

The embedded build scripts rely on Git to do versioning through the use of [GitVersion](https://gitversion.readthedocs.io/en/latest/). This means that you need to initialize a git repo and create at least one commit

```shell
git init
git add .
git commit -m Initial
```

#### How to implement

Start with `MyBuilpack` class. Depending on the type of buildpack you're creating, inherit either from `SupplyBuildpack` or `FinalBuildpack` and implement the `Detect`, `Apply`, and in the case of final buildpack the `GetStartupCommand`.

## How to package

Included build packaging is based on Nuke.Build project. Either use the included build shell scripts, or install IDE plugins to run included targets. Get full list of useful targets and parameters with `--help` argument for the build script. 

You can build buildpack that are compatible with both Windows and Linux stacks, however when targeting Windows stack it can only be compiled on a Windows machine.

The provided build script accepts one argument to specify the stack you're targeting. Windows stack will leverage .NET Framework (which is already included in the stemcell), resulting in smaller buildpack. When targeting Linux, the buildpack will be assembled with .NET core as self-contained (resulting in ~22mb package).

### Compiling on Windows

```powershell
.\build.ps1 --stack windows
```

or

```powershell
.\build.ps1 --stack linux
```

### Compiling on Linux or Mac

```bash
./build.sh --stack linux
```

Final output will be placed in `/artifacts` folder

## How to Release

Use the embedded `Release` target to create and publish buildpack as asset to GitHub releases. This requires you have remote repo set and have specified API Key as parameter (or set via environmental variable).

## How to use

* Option 1: Upload to Cloud Foundry via `cf create-buildpack` option and reference in manifest by name
* Option 2: Upload to a public host (like GitHub releases page) and reference in manifest via URL

#### Sample manifest


```yaml
applications:
- name: simpleapp
  stack: windows2016
  buildpacks: 
    - https://github.com/macsux/web-config-transform-buildpack/releases/download/1.0/web-config-transform-buildpack.zip
    - hwc_buildpack
```

