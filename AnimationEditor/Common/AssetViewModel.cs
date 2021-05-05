﻿using Common;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace AnimationEditor.Common.ReferenceModel
{
    public class AssetViewModel : NotifyPropertyChangedImpl, IAnimationProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;

        ILogger _logger = Logging.Create<AssetViewModel>();
        PackFileService _pfs;

        ResourceLibary _resourceLibary;
        ISceneNode _parentNode;
        SkeletonNode _skeletonSceneNode;
        List<Rmv2MeshNode> _meshNodes = new List<Rmv2MeshNode>();
        View3D.Animation.AnimationPlayer _player;



        public bool IsActive => true;
        public GameSkeleton Skeleton { get; set; }
        public AnimationClip AnimationClip { get; set; }

        string _meshName;
        public string MeshName { get => _meshName; set => SetAndNotify(ref _meshName, value); }

        string _skeletonName;
        public string SkeletonName { get => _skeletonName; set => SetAndNotify(ref _skeletonName, value); }

        string _animationName;
        public string AnimationName { get => _animationName; set => SetAndNotify(ref _animationName, value); }


        bool _showMesh = true;
        public bool ShowMesh { get => _showMesh; set { SetAndNotify(ref _showMesh, value); _meshNodes.ForEach((x) => x.IsVisible = value); } }

        bool _isSkeletonVisible = true;
        public bool IsSkeletonVisible { get => _isSkeletonVisible; set { SetAndNotify(ref _isSkeletonVisible, value); _skeletonSceneNode.IsVisible = value; } }


        bool _isAnimationActive = true;
        public bool IsAnimationActive { get => _isAnimationActive; set { SetAndNotify(ref _isAnimationActive, value); _player.Play(value); } }

        public AssetViewModel(PackFileService pfs)
        {
            _pfs = pfs;
        }

        internal void Initialize(ResourceLibary resourceLib, View3D.Animation.AnimationPlayer animationPlayer, ISceneNode parentNode, Color skeletonColour)
        {
            _resourceLibary = resourceLib;
            _parentNode = parentNode;
            _player = animationPlayer;
            _skeletonSceneNode = new SkeletonNode(_resourceLibary.Content, this);
            _skeletonSceneNode.NodeColour = skeletonColour;
            parentNode.AddObject(_skeletonSceneNode);
        }

        public void SetMesh(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_pfs.GetFullPath(file)}");

            SceneLoader loader = new SceneLoader(_pfs, _resourceLibary);
            var outSkeletonName = "";
            var result = loader.Load(file, null, _player, ref outSkeletonName);
            if (result == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            result.ForeachNode((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = false;
            });

            for (int i = 0; i < _meshNodes.Count; i++)
                _meshNodes[i].Parent.RemoveObject(_meshNodes[i]);

            _meshNodes.Clear();
            _meshNodes = Rmv2MeshNodeHelper.GetAllVisibleMeshes(result);

            for (int i = 0; i < _meshNodes.Count; i++)
                _parentNode.AddObject(_meshNodes[i]);

            var fullSkeletonName = $"animations\\skeletons\\{outSkeletonName}.anim";
            var skeletonFile = _pfs.FindFile(fullSkeletonName);
            SetSkeleton(skeletonFile as PackFile);
            MeshName = file.Name;
        }

        public void SetSkeleton(PackFile skeletonPackFile)
        {
            if (skeletonPackFile != null)
            {
                var newSkeletonName = _pfs.GetFullPath(skeletonPackFile);
                if (newSkeletonName == SkeletonName)
                    return;

                var skeleton = AnimationFile.Create(skeletonPackFile);
                SetSkeleton(skeleton, newSkeletonName);
            }
            else
            {
                SkeletonName = "";
                Skeleton = null;
                AnimationClip = null;
                _player.SetAnimation(null, Skeleton);
                SkeletonChanged?.Invoke(Skeleton);
            }

        }

        public void SetSkeleton(AnimationFile animFile, string skeletonName)
        {
            SkeletonName = skeletonName;
            Skeleton = new GameSkeleton(animFile, _player);

            AnimationClip = null;
            _player.SetAnimation(null, Skeleton);
            SkeletonChanged?.Invoke(Skeleton);
        }

        internal void ResetAnimation()
        {
            _player.CurrentFrame = 0;
            _player.Play();
        }

        internal void OnlyShowMeshRelatedToBones(List<int> selectedBoneIds)
        {
            var boneArray = selectedBoneIds.ToArray();
            foreach (var meshNode in _meshNodes)
            {
                //meshNode.IsVisible = false;
                var geo = meshNode.Geometry as Rmv2Geometry;
                //if (geo.ContainsAnimationBone(boneArray) == false)
                //    meshNode.IsVisible = false;
                //else
                geo.RemoveAllVertexesNotUsedByBones(boneArray);
            }
        }

        internal void SelectedBoneIndex(int? boneIndex)
        {
            _skeletonSceneNode.SelectedBoneIndex = boneIndex;
        }

        public void SetAnimation(PackFile file)
        {
            if (file != null)
            {
                AnimationName = _pfs.GetFullPath(file);
                var animation = AnimationFile.Create(file);
                SetAnimationClip(new AnimationClip(animation));
            }
            else
            {
                AnimationName = "";
                SetAnimationClip(null);
            }
        }

        public void SetAnimationClip(AnimationClip clip)
        {
            AnimationClip = clip;
            _player.SetAnimation(AnimationClip, Skeleton);
            if (AnimationClip != null)
                _player.Play();
        }
    }





    public class AnimatedPropViewModel : NotifyPropertyChangedImpl, IAnimationProvider
    {
        public event ValueChangedDelegate<GameSkeleton> SkeletonChanged;

        ILogger _logger = Logging.Create<AssetViewModel>();
        PackFileService _pfs;

        ResourceLibary _resourceLibary;
        ISceneNode _parentNode;
        SkeletonNode _skeletonSceneNode;
        List<Rmv2MeshNode> _meshNodes = new List<Rmv2MeshNode>();
        View3D.Animation.AnimationPlayer _player;



        public bool IsActive => true;
        public GameSkeleton Skeleton { get; set; }


        string _meshName;
        public string MeshName { get => _meshName; set => SetAndNotify(ref _meshName, value); }

        string _skeletonName;
        public string SkeletonName { get => _skeletonName; set => SetAndNotify(ref _skeletonName, value); }

        string _animationName;
        public string AnimationName { get => _animationName; set => SetAndNotify(ref _animationName, value); }


        bool _showMesh = true;
        public bool ShowMesh { get => _showMesh; set { SetAndNotify(ref _showMesh, value); _meshNodes.ForEach((x) => x.IsVisible = value); } }

        bool _isSkeletonVisible = true;
        public bool IsSkeletonVisible { get => _isSkeletonVisible; set { SetAndNotify(ref _isSkeletonVisible, value); _skeletonSceneNode.IsVisible = value; } }


        bool _isAnimationActive = true;
        public bool IsAnimationActive { get => _isAnimationActive; set { SetAndNotify(ref _isAnimationActive, value); _player.Play(value); } }

        public AnimatedPropViewModel(PackFileService pfs)
        {
            _pfs = pfs;
        }

        internal void Initialize(ResourceLibary resourceLib, View3D.Animation.AnimationPlayer animationPlayer, ISceneNode parentNode, Color skeletonColour)
        {
            _resourceLibary = resourceLib;
            _parentNode = parentNode;
            _player = animationPlayer;
            _skeletonSceneNode = new SkeletonNode(_resourceLibary.Content, this);
            _skeletonSceneNode.NodeColour = skeletonColour;
            parentNode.AddObject(_skeletonSceneNode);
        }

        public void SetMesh(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_pfs.GetFullPath(file)}");

            SceneLoader loader = new SceneLoader(_pfs, _resourceLibary);
            var outSkeletonName = "";
            var result = loader.Load(file, null, _player, ref outSkeletonName);
            if (result == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            result.ForeachNode((node) =>
            {
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = false;
            });

            for (int i = 0; i < _meshNodes.Count; i++)
                _meshNodes[i].Parent.RemoveObject(_meshNodes[i]);

            _meshNodes.Clear();
            _meshNodes = Rmv2MeshNodeHelper.GetAllVisibleMeshes(result);

            for (int i = 0; i < _meshNodes.Count; i++)
                _parentNode.AddObject(_meshNodes[i]);

            var fullSkeletonName = $"animations\\skeletons\\{outSkeletonName}.anim";
            var skeletonFile = _pfs.FindFile(fullSkeletonName);
            SetSkeleton(skeletonFile as PackFile);
            MeshName = file.Name;
        }

        public void SetSkeleton(PackFile skeletonPackFile)
        {
            if (skeletonPackFile != null)
            {
                var newSkeletonName = _pfs.GetFullPath(skeletonPackFile);
                if (newSkeletonName == SkeletonName)
                    return;

                SkeletonName = newSkeletonName;
                var skeletonAnimationFile = AnimationFile.Create(skeletonPackFile);
                Skeleton = new GameSkeleton(skeletonAnimationFile, _player);
            }
            else
            {
                SkeletonName = "";
                Skeleton = null;
            }
            _player.SetAnimation(null, Skeleton);
            SkeletonChanged?.Invoke(Skeleton);
        }

        internal void OnlyShowMeshRelatedToBones(List<int> selectedBoneIds)
        {
            var boneArray = selectedBoneIds.ToArray();
            foreach (var meshNode in _meshNodes)
            {
                //meshNode.IsVisible = false;
                var geo = meshNode.Geometry as Rmv2Geometry;
                //if (geo.ContainsAnimationBone(boneArray) == false)
                //    meshNode.IsVisible = false;
                //else
                geo.RemoveAllVertexesNotUsedByBones(boneArray);
            }
        }

        internal void SelectedBoneIndex(int? boneIndex)
        {
            _skeletonSceneNode.SelectedBoneIndex = boneIndex;
        }

        public void SetAnimation(PackFile file)
        {
            if (file != null)
            {
                var animFile = AnimationFile.Create(file);
                var animClip = new AnimationClip(animFile);
                AnimationName = _pfs.GetFullPath(file);
                _player.SetAnimation(animClip, Skeleton);
                _player.Play();
            }
            else
            {
                AnimationName = "";
                _player.SetAnimation(null, Skeleton);
            }
        }
    }
}
