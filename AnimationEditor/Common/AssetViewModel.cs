﻿using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Collections.Generic;
using View3D.Animation;
using View3D.Animation.MetaData;
using View3D.Components;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{
    public class AssetViewModel : BaseComponent, ISkeletonProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;
        public event ValueChangedDelegate<AnimationClip> AnimationChanged;
        public event ValueChangedDelegate<AssetViewModel> MeshChanged;
        public event ValueChangedDelegate<AssetViewModel> MetaDataChanged;


        ILogger _logger = Logging.Create<AssetViewModel>();
        PackFileService _pfs;
        ResourceLibary _resourceLibary;
        SceneNode _parentNode;
        Color _skeletonColor;
        SkeletonNode _skeletonSceneNode;
        ISceneNode _modelNode;
        IComponentManager _componentManager;
        ApplicationSettingsService _applicationSettingsService;

        bool _isSelectable = false;
        public bool IsSelectable { get => _isSelectable; set { _isSelectable = value; SetSelectableState(); } } 

        public View3D.Animation.AnimationPlayer Player;
        public List<IMetaDataInstance> MetaDataItems { get; set; } = new List<IMetaDataInstance>();

        public SceneNode MainNode { get => _parentNode; }

        public string Description { get; set; }

        public bool IsActive => true;
        public GameSkeleton Skeleton { get; private set; }
        public AnimationClip AnimationClip { get; private set; }
        public PackFile MetaData { get; private set; }
        public PackFile PersistMetaData { get; private set; }
        public Matrix Offset { get; set; } = Matrix.Identity;


        // --- UI elements
        public NotifyAttr<string> MeshName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<AnimationReference> AnimationName { get; set; } = new NotifyAttr<AnimationReference>(null);

        public NotifyAttr<bool> ShowMesh { get; set; }
        public NotifyAttr<bool> ShowSkeleton { get; set; }


        public AssetViewModel(PackFileService pfs, string description, Color skeletonColour, IComponentManager componentManager, ApplicationSettingsService applicationSettingsService)  : base(componentManager)
        {
            Description = description;
            _pfs = pfs;
            _skeletonColor = skeletonColour;
            _componentManager = componentManager;
            _applicationSettingsService = applicationSettingsService;

            ShowMesh = new NotifyAttr<bool>(true, (x) => SetMeshVisability(x));
            ShowSkeleton = new NotifyAttr<bool>(true, (x) => _skeletonSceneNode.IsVisible = ShowSkeleton.Value);
        }

        public override void Initialize()
        {
            var rootNode = _componentManager.GetComponent<SceneManager>().RootNode;
            _resourceLibary = _componentManager.GetComponent<ResourceLibary>();
            var animComp = _componentManager.GetComponent<AnimationsContainerComponent>();

            _parentNode = rootNode.AddObject(new GroupNode(Description)) as GroupNode;
            Player = animComp.RegisterAnimationPlayer(new View3D.Animation.AnimationPlayer(), Description);

            // Create skeleton
            _skeletonSceneNode = new SkeletonNode(_componentManager, this);
            _skeletonSceneNode.NodeColour = _skeletonColor;
            _parentNode.AddObject(_skeletonSceneNode);

            base.Initialize();
        }

        void SetMeshVisability(bool value)
        {
            if (_modelNode == null)
                return;
            _modelNode.IsVisible = value;
        }

        public void SetMesh(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_pfs.GetFullPath(file)}");

            var graphics = _componentManager.GetComponent<DeviceResolverComponent>();
            SceneLoader loader = new SceneLoader(_resourceLibary, _pfs, GeometryGraphicsContextFactory.CreateInstance(graphics.Device), _componentManager, _applicationSettingsService);
            var loadedNode = loader.Load(file, null, Player);
            if (loadedNode == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            if (_modelNode != null)
                _parentNode.RemoveObject(_modelNode);
            _modelNode = loadedNode;
            _parentNode.AddObject(loadedNode);

            var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
            var fullSkeletonName = $"animations\\skeletons\\{skeletonName}.anim";
            var skeletonFile = _pfs.FindFile(fullSkeletonName);
            SetSkeleton(skeletonFile);
            MeshName.Value = file.Name;
            ShowMesh.Value = ShowMesh.Value;
            ShowSkeleton.Value = ShowSkeleton.Value;

            loadedNode.ForeachNodeRecursive((node) =>
            {
                if (node is Rmv2MeshNode mesh && string.IsNullOrWhiteSpace(mesh.AttachmentPointName) == false)
                {
                    if (Skeleton != null)
                    {
                        int boneIndex = Skeleton.GetBoneIndexByName(mesh.AttachmentPointName);
                        mesh.AttachmentBoneResolver = new SkeletonBoneAnimationResolver(this, boneIndex);
                    }
                }
            });

            MeshChanged?.Invoke(this);
        }

        void SetSelectableState()
        {
            if (_modelNode == null)
                return;
            _modelNode.ForeachNodeRecursive((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = IsSelectable;
            });
        }

        public void CopyMeshFromOther(AssetViewModel other)
        {
            if (_modelNode != null)
                _parentNode.RemoveObject(_modelNode);

            if (other._modelNode == null)
                return;

            _modelNode = SceneNodeHelper.DeepCopy(other._modelNode);

            var cloneMeshes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(_modelNode);
            foreach (var mesh in cloneMeshes)
                mesh.AnimationPlayer = Player;

            _parentNode.AddObject(_modelNode);
            var skeletonFile = _pfs.FindFile(other.SkeletonName.Value);
            SetSkeleton(skeletonFile);
            
            ShowMesh.Value = ShowMesh.Value;
            ShowSkeleton.Value = ShowSkeleton.Value;
            SetMeshVisability(ShowMesh.Value);

            MeshChanged?.Invoke(this);
        }

        public void SetSkeleton(PackFile skeletonPackFile)
        {
            if (skeletonPackFile != null)
            {
                var newSkeletonName = _pfs.GetFullPath(skeletonPackFile);
                if (newSkeletonName == SkeletonName.Value)
                    return;

                var skeleton = AnimationFile.Create(skeletonPackFile);
                SetSkeleton(skeleton, newSkeletonName);
            }
            else
            {
                if (Skeleton == null)
                    return;
                SkeletonName.Value = "";
                Skeleton = null;
                AnimationClip = null;
                Player.SetAnimation(null, Skeleton);
                SkeletonChanged?.Invoke(Skeleton);
            }
        }

        public void SetTransform(Matrix matrix)
        {
            if(_modelNode != null)
                _modelNode.ModelMatrix = matrix;
        }

        public void SetSkeleton(AnimationFile animFile, string skeletonName)
        {
            SkeletonName.Value = skeletonName;
            Skeleton = new GameSkeleton(animFile, Player);

            AnimationClip = null;
            Player.SetAnimation(null, Skeleton);
            SkeletonChanged?.Invoke(Skeleton);
        }


        internal void SelectedBoneIndex(int? boneIndex)
        {
            _skeletonSceneNode.SelectedBoneIndex = boneIndex;
        }

        internal void SelectedBoneScale(float scaleMult)
        {
            _skeletonSceneNode.SelectedBoneScaleMult = scaleMult;
        }


        public void SetAnimation(AnimationReference animationReference)
        {
            if (animationReference != null)
            {
                var file = _pfs.FindFile(animationReference.AnimationFile, animationReference.Container) ;
                AnimationName.Value = animationReference;
                var animation = AnimationFile.Create(file);
                SetAnimationClip(new AnimationClip(animation, Skeleton), animationReference);
            }
            else
            {
                SetAnimationClip(null, null);
            }
        }

        public void SetAnimationClip(AnimationClip clip, AnimationReference animationReference)
        {
            if (AnimationClip == null && clip == null && animationReference == null)
                return;
            var frame = Player.CurrentFrame;
            AnimationClip = clip;
            AnimationName.Value = animationReference;
            Player.SetAnimation(AnimationClip, Skeleton);
            AnimationChanged?.Invoke(clip);
            Player.CurrentFrame = frame;
        }

        public void SetMetaFile(PackFile metaFile, PackFile persistantFile)
        {
            MetaData = metaFile;
            PersistMetaData = persistantFile;
            MetaDataChanged?.Invoke(this);
        }

        public void ReApplyMeta()
        {
            MetaDataChanged?.Invoke(this);
        }

        public override void Update(GameTime gameTime)
        {
            _parentNode.ModelMatrix = Matrix.Multiply(Offset,Matrix.Identity);

            var p = Player.CurrentFrame;
            foreach (var item in MetaDataItems)
                item.Update(p);
        }

        
    }
}
