# DragonBones C# Runtime
[中文 README](./README-zh_CN.md)
## [DragonBones common library](./DragonBones/)
## Highly suggest use [DragonBones Pro](http://www.dragonbones.com/) to create aniamtion.

## To learn more about
* [DragonBones offical website](http://www.dragonbones.com/)
* [Online demos](http://www.dragonbones.com/demo/index.html)

## Godot [WIP]
```c#
// for example:
Node2D anchorNode = new Node2D();
anchorNode.Position = new Vector2(16, 16);
GetTree().Root.GetChild(0).AddChild(anchorNode);

var factory = new GodotDragonBonesFactory(GetTree());
        
DragonBonesData ske = factory.LoadDragonBonesData("res://assets/dragonbones/monster_ske.json", "");
TextureAtlasData tex = factory.LoadTextureAtlasData("res://assets/dragonbones/monster_tex.json", "");
GodotAramture armatureNode =
    factory.BuildArmatureNode("Armature", "monster", node: anchorNode, textureAtlasName: "monster");
        
armatureNode.animation.Play("idle");
```

## Online demos
[![PerformanceTest](https://dragonbones.github.io/demo/demos.jpg)](https://github.com/DragonBones/Demos)

Copyright (c) 2012-2018 The DragonBones team and other contributors.
