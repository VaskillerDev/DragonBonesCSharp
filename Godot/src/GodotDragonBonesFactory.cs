using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DragonBones;
using Godot;
using MiniJSON;
using DragonBonesInstance = DragonBones.DragonBones;

namespace Test.code.dragonbones
{
    internal class ClockHandler : Node
    {
        public override void _Process(float delta)
        {
            GodotDragonBonesFactory.GetFactory()._dragonBones.AdvanceTime(delta);
        }
    }

    public class GodotDragonBonesFactory : BaseFactory
    {
        // use for Node naming, like GameObject.AddComponent<T>() in Unity
        private const string FACTORY_NODE_NAME = nameof(GodotDragonBonesFactory);
        private const string CLOCK_HANDLER_NODE_NAME = nameof(ClockHandler);
        private const string GODOT_EVENT_DISPATCHER_NODE_NAME = nameof(GodotEventDispatcher);
        private static DragonBonesInstance _dragonBonesInstance;
        private static GodotDragonBonesFactory _factory;
        private static Node _factoryNode;
        private Node2D _displayNode;

        public GodotDragonBonesFactory(SceneTree sceneTree, DataParser dataParser = null) :
            base(dataParser) // initialization
        {
            if (_factory != null) return;
            _factory = this;

            if (sceneTree == null) return;
            if (sceneTree.Paused) InitDragonBonesInstance(null);

            // factory node
            if (_factoryNode == null
            ) // creating hidden static GodotDragonBonesFactory reference as in-game node, and attach it in root
            {
                _factoryNode = new Node {Name = FACTORY_NODE_NAME};

                var root = sceneTree.Root.GetChild(0); // todo: hardcode. temporary solution
                root.AddChild(_factoryNode);
            }

            // clock handler
            var clockHandler = _factoryNode.FindNode(CLOCK_HANDLER_NODE_NAME) as ClockHandler;
            if (clockHandler == null)
            {
                clockHandler = new ClockHandler {Name = CLOCK_HANDLER_NODE_NAME};
                _factoryNode.AddChild(clockHandler);
            }

            // event dispatcher
            var godotEventDispatcher = _factoryNode.FindNode(GODOT_EVENT_DISPATCHER_NODE_NAME) as GodotEventDispatcher;
            if (godotEventDispatcher == null)
            {
                godotEventDispatcher = new GodotEventDispatcher {Name = GODOT_EVENT_DISPATCHER_NODE_NAME};
                _factoryNode.AddChild(godotEventDispatcher);
            }

            InitDragonBonesInstance(godotEventDispatcher);
        }

        private void InitDragonBonesInstance(IEventDispatcher<EventObject> eventManager)
        {
            if (_dragonBonesInstance != null) return;
            _dragonBonesInstance = new DragonBonesInstance(eventManager);
            DragonBonesInstance.yDown = false; // ???
            _dragonBones = _dragonBonesInstance;
        }

        protected override TextureAtlasData _BuildTextureAtlasData(TextureAtlasData textureAtlasData,
            object textureAtlas)
        {
            if (textureAtlasData == null) return BaseObject.BorrowObject<GodotTextureAtlasData>();
            return null;
        }

        protected override Armature _BuildArmature(BuildArmaturePackage dataPackage)
        {
            var armature = BaseObject.BorrowObject<Armature>();
            var armatureDisplay = _displayNode == null ? Helper.CreateNodeWithName(dataPackage.dataName) : _displayNode;
            var armatureName = dataPackage.armature.name;

            GodotArmature armatureNode = null;
            if (armatureDisplay.HasNode(armatureName))
                armatureNode = armatureDisplay.GetNode<GodotArmature>(armatureName);

            if (armatureNode == null) // create node
            {
                armatureNode = new GodotArmature();
                armatureNode.Name = dataPackage.armature.name;
                armatureDisplay.AddChild(armatureNode);
            }

            armatureNode.armature = armature;
            armature.Init(dataPackage.armature, armatureNode, armatureDisplay, _dragonBonesInstance);
            _displayNode = null;

            return armature;
        }

        protected override Slot _BuildSlot(BuildArmaturePackage dataPackage, SlotData slotData, Armature armature)
        {
            var slot = BaseObject.BorrowObject<GodotSlot>();
            var armatureDisplay = armature.display as Node2D;
            if (armatureDisplay == null) return null;
            
            var boneNode = CreateBoneNode(slotData);
            armatureDisplay.AddChild(boneNode);

            var slotNode = armatureDisplay.FindNode(slotData.name) as Sprite;
            if (slotNode == null) slotNode = Helper.CreateSlotNodeWithName(slotData.name);
            armatureDisplay.AddChild(slotNode);

            var atlasName = dataPackage.textureAtlasName ?? dataPackage.dataName;
            var atlasData = GetTextureAtlasData(atlasName).First() as GodotTextureAtlasData; // get first elem
            slot.SetCurrentTextureAtlasData(ref atlasData);
            slot.SetBoneNode(boneNode);
            slot.Init(slotData, armature, slotNode, slotNode);

            return slot;
        }

        private Node2D CreateBoneNode(SlotData slotData)
        {
            // bone adding
            var bone = slotData.parent;
            var boneNodeName = bone.name;
            var boneNode = Helper.CreateNodeWithName(boneNodeName);
            boneNode.Position = new Vector2(bone.transform.x, bone.transform.y);
            return boneNode;
        }

        protected override void _FoldBones(BuildArmaturePackage dataPackage, Armature armature)
        {
            var armatureDisplay = armature.display as Node2D;
            if (armatureDisplay == null) return;
            List<Node> childrenArmatureDisplay = armatureDisplay.GetChildren().OfType<Node>().ToList();
            
            foreach (var slot in armature.GetSlots()) // slots
            {
                var boneParentName = slot.parent?.name;
                if (boneParentName == null) continue;
                
                var parentNode = childrenArmatureDisplay.Find(node => node.Name == boneParentName );
                var godotSlot = slot as GodotSlot;
                var slotNode = godotSlot?.GetSlotNode();
                armatureDisplay.RemoveChild(slotNode);
                parentNode.AddChild(slotNode);
            }
            
            foreach (var bone in dataPackage.armature.bones) // bones
            {
                var boneData = bone.Value;
                var (boneName, boneParentName) = (bone.Key, boneData.parent?.name);
                var isRoot = boneParentName == null;
                
                if (isRoot) continue;
                var parentNode = childrenArmatureDisplay.Find(node => node.Name == boneParentName );
                var boneNode = childrenArmatureDisplay.Find(node => node.Name == boneName );
                armatureDisplay.RemoveChild(boneNode);
                parentNode.AddChild(boneNode);
            }
        }

        public GodotArmature BuildArmatureNode(string armatureName, string dragonBonesName = "", string skinName = "",
            string textureAtlasName = "", Node2D node = null)
        {
            _displayNode = node;
            var armature =
                BuildArmature(armatureName, dragonBonesName, skinName,
                    textureAtlasName); // its call _BuildArmature => _BuildBones => _BuildSlots
            if (armature == null) return null;

            _dragonBones.clock.Add(armature);
            var armatureDisplay = armature.display as Node2D;
            var armatureNode = armatureDisplay?.GetNode<GodotArmature>(armatureName);

            return armatureNode;
        }

        public DragonBonesData LoadDragonBonesData(string pathToJsonData, string name, float scale = 0.01f)
        {
            if (pathToJsonData == null) return null;
            if (name == null) return null;

            {
                // try get cache storage
                var existedData = GetDragonBonesData(name);
                if (existedData != null) return existedData;
            }

            var content = Helper.GetTextContentByPath(pathToJsonData); // json as string
            if (content == null) return null;

            DragonBonesData data;
            if (content == "DBDT") // binary parsing
            {
                BinaryDataParser.jsonParseDelegate = Json.Deserialize;
                var bytes = Encoding.UTF8.GetBytes(content);
                data = ParseDragonBonesData(bytes, name, scale);
            }
            else
            {
                // normal parsing via key-value
                var jsonData = Json.Deserialize(content) as Dictionary<string, object>;
                data = ParseDragonBonesData(jsonData, name, scale);
            }

            if (string.IsNullOrEmpty(name)) name = data.name;
            AddDragonBonesData(data, name); // save value in DragonBones cache storage: _dragonBonesDataMap
            return data;
        }

        public TextureAtlasData LoadTextureAtlasData(string pathToJsonAtlas, string name, float scale = 1.0f)
        {
            if (pathToJsonAtlas == null) return null;
            if (name == null) return null;

            var content = Helper.GetTextContentByPath(pathToJsonAtlas);
            if (content == null) return null;

            var jsonData = Json.Deserialize(content) as Dictionary<string, object>;
            var data = ParseTextureAtlasData(jsonData, null, name, scale);
            if (data == null) return null;
            data.imagePath =
                Helper.GetPathToImageTexture(pathToJsonAtlas,
                    data.imagePath); // "my_texture.png" => "path/to/jsonAtlas/my_texture.png" 

            var atlasTexture = new AtlasTexture();
            var textureFromPath = Helper.LoadTexture(data.imagePath);

            foreach (var texture in data.textures.ToList()) // set texture foreach textureData in TextureAtlasData
            {
                var key = texture.Key;
                var textureData = texture.Value as GodotTextureData;
                if (textureData == null) continue;
                textureData.Texture = textureFromPath;

                data.textures[key] = textureData;
            }

            atlasTexture.Atlas = textureFromPath;

            return data;
        }

        public static GodotDragonBonesFactory GetFactory()
        {
            if (_factory == null)
                GD.PushWarning(
                    "GodotDragonBonesFactory call GetFactory when factory == null. First one, initialize GodotDragonBonesFactory via constructor.");
            return _factory;
        }

        // ---------------------------------------------
        private static class Helper
        {
            public static string GetPathToImageTexture(string pathToJsonAtlas, string imageName)
            {
                // "res://myfolder/file.json" => "res://myfolder"
                var indexDirectoryFile = pathToJsonAtlas.LastIndexOf("/", StringComparison.Ordinal);
                var directoryFile = pathToJsonAtlas.Substring(0, indexDirectoryFile);

                return directoryFile + "/" + imageName;
            }

            public static string GetTextContentByPath(string pathToFile)
            {
                var file = new File();
                file.Open(pathToFile, File.ModeFlags.Read);
                return file.GetAsText();
            }

            public static Node2D CreateNodeWithName(string name)
            {
                var node = new Node2D {Name = name};
                return node;
            }

            public static Sprite CreateSlotNodeWithName(string name)
            {
                var sprite = new Sprite {Name = name};
                return sprite;
            }

            public static Texture LoadTexture(string path)
            {
                var image = new Image();
                image.Load(path);
                var imageTexture = new ImageTexture();
                imageTexture.CreateFromImage(image);
                return imageTexture;
            }
        }
    }
}