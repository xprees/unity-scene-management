# Unity Scene Management Package

[![NPM Version](https://img.shields.io/npm/v/cz.xprees.scene-management)](https://www.npmjs.com/package/cz.xprees.scene-management)

This package builds on top of the Unity Scene Management system
and [Unity Addressables package](https://docs.unity3d.com/Packages/com.unity.addressables@2.6/manual/index.html) to build a powerful scene management
system for Unity multi-scene projects (single scene is supported, too). Based on my [Unity Events package](https://github.com/xprees/unity-events), to
avoid tight coupling.

## Features

- **Scene Loading** - Async scene (un)loading using the Addressables system and UniTasks.
- **SceneSO** - A ScriptableObject that represents a scene with a metadata and type, which is used to load the scene properly.
- **Addressables Initialization** - Provides scripts to initialize the Addressables system in the Init scene and load into the desktop mode.
    - Extendable to add custom initialization logic. See the [`Initialization`](Runtime/Initialization) folder.

## Installation

Install the package using npm scoped registry in `Project Settings > Package Manager > Scoped Registries`

```json
{
    "name": "NPM - xprees",
    "url": "https://registry.npmjs.org",
    "scopes": [
        "cz.xprees",
        "com.dbrizov.naughtyattributes"
    ]
}

```

Then simply install the package using the Unity Package Manager using the _NPM - xprees_ scope or by the package name `cz.xprees.scene-management`.
