﻿using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.RigidModel;
using CommonControls.SelectionListDialog;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using View3D.Animation;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace AnimationEditor.AnimationTransferTool
{
    public class Editor : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<Editor>();

        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        IComponentManager _componentManager;
        AssetViewModel _copyTo;
        AssetViewModel _copyFrom;
        AnimationPlayerViewModel _player;
        public AssetViewModel Generated { get; set; }
        List<IndexRemapping> _remappingInformaton;
        RemappedAnimatedBoneConfiguration _config;
        BoneMappingWindow _activeBoneMappingWindow;

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public ObservableCollection<SkeletonBoneNode> FlatBoneList { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public AnimationSettings AnimationSettings { get; set; } = new AnimationSettings();

        SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); HightlightSelectedBones(value); }
        }

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel copyToAsset, AssetViewModel copyFromAsset, AssetViewModel generated, IComponentManager componentManager, AnimationPlayerViewModel player)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _componentManager = componentManager;
            _player = player;

            _copyTo = copyToAsset;
            _copyFrom = copyFromAsset;
            Generated = generated;

            _copyFrom.SkeletonChanged += CopyFromSkeletonChanged;
            _copyTo.MeshChanged += CopyToMeshChanged;

            AnimationSettings.DisplayOffset.OnValueChanged += DisplayOffset_OnValueChanged;
            DisplayOffset_OnValueChanged(new CommonControls.MathViews.Vector3ViewModel(0,0,2));

            if (_copyTo.Skeleton != null)
                CopyToMeshChanged(_copyTo);

            if (_copyFrom.Skeleton != null)
                CopyFromSkeletonChanged(_copyFrom.Skeleton);
        }

        private void DisplayOffset_OnValueChanged(CommonControls.MathViews.Vector3ViewModel newValue)
        {
            _copyTo.Offset = Matrix.CreateTranslation(newValue.GetAsVector3() * -1);
            _copyFrom.Offset = Matrix.CreateTranslation(newValue.GetAsVector3());
        }

        void HightlightSelectedBones(SkeletonBoneNode bone)
        {
            if (bone == null)
            {
                Generated.SelectedBoneIndex(-1);
                _copyFrom.SelectedBoneIndex(-1);
            }
            else
            {
                Generated.SelectedBoneIndex(bone.BoneIndex.Value);
                if (_remappingInformaton != null)
                {
                    var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                    if (mapping != null)
                        _copyFrom.SelectedBoneIndex(mapping.NewValue);
                }
            }
        }

        private void CopyToMeshChanged(AssetViewModel newValue)
        {
            Generated.CopyMeshFromOther(newValue);
            CreateBoneOverview(newValue.Skeleton);
            HightlightSelectedBones(null);

            _config = null;
            AnimationSettings.UseScaledSkeletonName.Value = false;
            AnimationSettings.ScaledSkeletonName.Value = "";
        }

        private void CopyFromSkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == _copyFrom.Skeleton)
                return;

            _remappingInformaton = null;
            CreateBoneOverview(_copyTo.Skeleton);
            HightlightSelectedBones(null);

            var standAnim = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(newValue.SkeletonName).FirstOrDefault(x => x.AnimationFile.Contains("stand"));
            if(standAnim != null)
                _copyFrom.SetAnimation(standAnim);

            _config = null;
            AnimationSettings.UseScaledSkeletonName.Value = false;
            AnimationSettings.ScaledSkeletonName.Value = "";
        }

        public void OpenMappingWindow()
        {
            if (_activeBoneMappingWindow != null)
            {
                _activeBoneMappingWindow.Focus();
                return;
            }

            if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Source or target skeleton not selected", "Error");
                return;
            }

            var targetSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyTo.SkeletonName.Value);
            var sourceSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyFrom.SkeletonName.Value);

            if (_config == null)
            {
                _config = new RemappedAnimatedBoneConfiguration();
                _config.MeshSkeletonName = _copyTo.SkeletonName.Value;
                _config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeleton);

                _config.ParnetModelSkeletonName = _copyFrom.SkeletonName.Value;
                _config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(sourceSkeleton);
                _config.SkeletonBoneHighlighter = new SkeletonBoneHighlighter(Generated, _copyFrom);
            }

            _activeBoneMappingWindow = new BoneMappingWindow(new BoneMappingViewModel(_config), false);
            _activeBoneMappingWindow.Show();
            _activeBoneMappingWindow.ApplySettings += BoneMappingWindow_Apply;
            _activeBoneMappingWindow.Closed += BoneMappingWindow_Closed;
        }

        private void BoneMappingWindow_Apply(object sender, EventArgs e)
        {
            _remappingInformaton = AnimatedBoneHelper.BuildRemappingList(_config.MeshBones.First());
            UpdateAnimation();
            UpdateBonesAfterMapping(Bones);
        }

        private void BoneMappingWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                if (_activeBoneMappingWindow.Result == true)
                {
                    _remappingInformaton = AnimatedBoneHelper.BuildRemappingList(_config.MeshBones.First());
                    UpdateAnimation();
                    UpdateBonesAfterMapping(Bones);
                }
            }
            finally
            {
                _activeBoneMappingWindow.Closed -= BoneMappingWindow_Closed;
                _activeBoneMappingWindow.ApplySettings -= BoneMappingWindow_Apply;
                _activeBoneMappingWindow = null;
            }
        }

        void UpdateBonesAfterMapping(IEnumerable<SkeletonBoneNode> bones)
        {
            foreach (var bone in bones)
            {
                var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                bone.HasMapping.Value = mapping != null;
                UpdateBonesAfterMapping(bone.Children);
            }
        }

        public void ClearRelativeSelectedBoneAction()
        {
            if (SelectedBone != null)
                SelectedBone.SelectedRelativeBone.Value = null;
        }

        public void UpdateAnimation()
        {
            if (CanUpdateAnimation(true))
            {
                var newAnimationClip = UpdateAnimation(_copyFrom.AnimationClip, _copyTo.AnimationClip);
                Generated.SetAnimationClip(newAnimationClip, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));

                _player.SelectedMainAnimation = _player.PlayerItems.First(x => x.Asset == Generated);
            }
        }

        AnimationClip UpdateAnimation(AnimationClip animationToCopy, AnimationClip originalAnimation)
        {
            var service = new AnimationRemapperService(AnimationSettings, _remappingInformaton, Bones);
            var newClip = service.ReMapAnimation(_copyFrom.Skeleton, _copyTo.Skeleton, animationToCopy);
            return newClip;
        }

        bool CanUpdateAnimation(bool requireAnimation)
        {
            if (_remappingInformaton == null)
            {
                MessageBox.Show("No mapping created?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Missing a skeleton?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_copyFrom.AnimationClip == null && requireAnimation)
            {
                MessageBox.Show("No animation to copy selected", "Error", MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        public void OpenBatchProcessDialog()
        {
            if (!CanUpdateAnimation(false))
                return;

            // Find all animations for skeleton
            var copyFromAnims = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(_copyFrom.Skeleton.SkeletonName);

            var items = copyFromAnims.Select(x => new SelectionListViewModel<SkeletonAnimationLookUpHelper.AnimationReference>.Item()
            {
                IsChecked = new NotifyAttr<bool>(! (x.AnimationFile.Contains("tech", StringComparison.InvariantCultureIgnoreCase) || x.AnimationFile.Contains("skeletons", StringComparison.InvariantCultureIgnoreCase))),
                DisplayName = x.AnimationFile,
                ItemValue = x
            }).ToList();

            var window = SelectionListWindow.ShowDialog("Select animations:", items);
            if (window.Result)
            {
                using (var waitCursor = new WaitCursor())
                {
                    var index = 1;
                    var numItemsToProcess = items.Count(x => x.IsChecked.Value);
                    foreach (var item in items)
                    {
                        if (item.IsChecked.Value)
                        {
                            var file = _pfs.FindFile(item.ItemValue.AnimationFile, item.ItemValue.Container);
                            var animFile = AnimationFile.Create(file.DataSource.ReadDataAsChunk());
                            var clip = new AnimationClip(animFile, _copyFrom.Skeleton);

                            _logger.Here().Information($"Processing animation {index} / {numItemsToProcess} - {item.DisplayName}");

                            var updatedClip = UpdateAnimation(clip, null);
                            SaveAnimation(updatedClip, item.ItemValue.AnimationFile, false);
                            index++;
                        }

                    }
                }
            }
        }

        public void SaveAnimationAction()
        {
            if (Generated.AnimationClip == null || Generated.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Can not save, as no animation has been generated. Press the Apply button first", "Error", MessageBoxButton.OK);
                return;
            }

            SaveAnimation(Generated.AnimationClip, _copyFrom.AnimationName.Value.AnimationFile);
        }

        void SaveAnimation(AnimationClip clip, string animationName, bool prompOnOverride = true)
        {
            var animFile = clip.ConvertToFileFormat(_copyTo.Skeleton);
            if (AnimationSettings.UseScaledSkeletonName.Value)
                animFile.Header.SkeletonName = AnimationSettings.ScaledSkeletonName.Value;

            if (AnimationSettings.AnimationOutputFormat.Value != 7)
                animFile.ConvertToVersion(AnimationSettings.AnimationOutputFormat.Value, _skeletonAnimationLookUpHelper, _pfs);

            if (AnimationSettings.UseScaledSkeletonName.Value)
                animFile.Header.SkeletonName = AnimationSettings.ScaledSkeletonName.Value;

            var orgSkeleton = _copyFrom.Skeleton.SkeletonName;
            var newSkeleton = _copyTo.Skeleton.SkeletonName;
            var newPath = animationName.Replace(orgSkeleton, newSkeleton);
            var currentFileName = Path.GetFileName(newPath);
            newPath = newPath.Replace(currentFileName, AnimationSettings.SavePrefix.Value + currentFileName);
            newPath = SaveHelper.EnsureEnding(newPath, ".anim");

            SaveHelper.Save(_pfs, newPath, null, AnimationFile.ConvertToBytes(animFile), prompOnOverride);
        }

        public void ClearAllSettings()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                CreateBoneOverview(_copyTo.Skeleton);
        }

        public void UseTargetAsSource()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                AnimationSettings.UseScaledSkeletonName.Value = false;
                AnimationSettings.ScaledSkeletonName.Value = "";
                _copyFrom.CopyMeshFromOther(_copyTo);
            }
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
            FlatBoneList.Clear();
            FlatBoneList.Add(null);

            if (skeleton == null)
                return;
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                SkeletonBoneNode newBone = null;
                var parentBoneId = skeleton.GetParentBoneIndex(i);
                if (parentBoneId == -1)
                {
                    newBone = new SkeletonBoneNode(skeleton.BoneNames[i], i, -1);
                    Bones.Add(newBone);
                }
                else
                {
                    var treeParent = BoneHelper.GetBoneFromId(Bones, parentBoneId);
                    if (treeParent != null)
                    {
                        newBone = new SkeletonBoneNode(skeleton.BoneNames[i], i, parentBoneId);
                        treeParent.Children.Add(newBone);
                    }
                }

                FlatBoneList.Add(newBone);
            }
        }

        //public void ExportMappedSkeleton()
        //{
        //    var skeletonFile = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyFrom.Skeleton.SkeletonName);
        //    var clip = new AnimationClip(skeletonFile);
        //
        //    var mappedSkeleton = UpdateAnimation(clip);
        //    var mappedSkeletonFile = mappedSkeleton.ConvertToFileFormat(Generated.Skeleton);
        //
        //    var newSkeletonName = Generated.Skeleton.SkeletonName + "_generated";
        //    AnimationSettings.UseScaledSkeletonName.Value = true;
        //    AnimationSettings.ScaledSkeletonName.Value = newSkeletonName + ".anim";
        //
        //
        //    var skeletonBytes = AnimationFile.ConvertToBytes(mappedSkeletonFile);
        //    SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".anim", null, skeletonBytes);
        //
        //
        //    //Save inv matrix file
        //    var newSkeleton = new GameSkeleton(mappedSkeletonFile, null);
        //    var invMatrixFile = newSkeleton.CreateInvMatrixFile();
        //    var invMatrixBytes = invMatrixFile.GetBytes();
        //    SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".bone_inv_trans_mats", null, invMatrixBytes);
        //
        //    SaveMeshWithNewSkeleton(mappedSkeleton, "changed");
        //}

        public void ExportScaledMesh()
        {
            var commandExecutor = _componentManager.GetComponent<CommandExecutor>();

            var modelNodes = SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(Generated.MainNode, (x) => x.IsVisible)
                .Where(x => x.IsVisible)
                .ToList();

            if (modelNodes.Count == 0)
            {
                MessageBox.Show("Can not save, as there is no mesh", "Error", MessageBoxButton.OK);
                return;
            }

            AnimationSettings.UseScaledSkeletonName.Value = true;
            var scaleStr = "s" + AnimationSettings.Scale.Value.ToString().Replace(".", "").Replace(",", "");
            var newSkeletonName = Generated.Skeleton.SkeletonName + "_" + scaleStr;
            var originalSkeletonName = modelNodes.First().Model.Header.SkeletonName;
            AnimationSettings.ScaledSkeletonName.Value = newSkeletonName;

            // Create scaled animation
            var scaleAnimClip = new AnimationClip();
            scaleAnimClip.DynamicFrames.Add(new AnimationClip.KeyFrame());
            scaleAnimClip.DynamicFrames.Add(new AnimationClip.KeyFrame());
            scaleAnimClip.PlayTimeInSec = 2.0f / 20.0f;
            for (int i = 0; i < Generated.Skeleton.BoneCount; i++)
            {
                scaleAnimClip.DynamicFrames[0].Position.Add(Generated.Skeleton.Translation[i]);
                scaleAnimClip.DynamicFrames[0].Rotation.Add(Generated.Skeleton.Rotation[i]);
                scaleAnimClip.DynamicFrames[0].Scale.Add(Vector3.One);

                scaleAnimClip.DynamicFrames[1].Position.Add(Generated.Skeleton.Translation[i]);
                scaleAnimClip.DynamicFrames[1].Rotation.Add(Generated.Skeleton.Rotation[i]);
                scaleAnimClip.DynamicFrames[1].Scale.Add(Vector3.One);
            }

            scaleAnimClip.DynamicFrames[0].Scale[0] = new Vector3((float)AnimationSettings.Scale.Value);
            scaleAnimClip.DynamicFrames[1].Scale[0] = new Vector3((float)AnimationSettings.Scale.Value);

            // Create a skeleton from the scaled animation

            SaveMeshWithNewSkeleton(scaleAnimClip, scaleStr);
        }

        void SaveMeshWithNewSkeleton(AnimationClip newSkeletonClip, string savePostFix)
        {
            var commandExecutor = _componentManager.GetComponent<CommandExecutor>();

            var modelNodes = SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(Generated.MainNode, (x) => x.IsVisible)
                .Where(x => x.IsVisible)
                .ToList();

            if (modelNodes.Count == 0)
            {
                MessageBox.Show("Can not save, as there is no mesh", "Error", MessageBoxButton.OK);
                return;
            }

            // Create a skeleton from the scaled animation
            var newSkeletonName = Generated.Skeleton.SkeletonName + "_" + savePostFix;
            var skeletonAnimFile = newSkeletonClip.ConvertToFileFormat(Generated.Skeleton);
            skeletonAnimFile.Header.SkeletonName = newSkeletonName;

            AnimationSettings.UseScaledSkeletonName.Value = true;
            AnimationSettings.ScaledSkeletonName.Value = newSkeletonName;

            var skeletonBytes = AnimationFile.ConvertToBytes(skeletonAnimFile);
            SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".anim", null, skeletonBytes);

            //Save inv matrix file
            var newSkeleton = new GameSkeleton(skeletonAnimFile, null);
            var invMatrixFile = newSkeleton.CreateInvMatrixFile();
            var invMatrixBytes = invMatrixFile.GetBytes();
            SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".bone_inv_trans_mats", null, invMatrixBytes);

            var animationFrame = AnimationSampler.Sample(0, 0, Generated.Skeleton, newSkeletonClip);

            int numCommandsToUndo = 0;
            var originalSkeletonName = modelNodes.First().Model.Header.SkeletonName;
            foreach (var model in modelNodes)
            {
                var header = model.Model.Header;
                header.SkeletonName = newSkeletonName;
                model.Model.Header = header;

                var meshList = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(model);
                var cmd = new CreateAnimatedMeshPoseCommand(meshList, animationFrame, false);
                commandExecutor.ExecuteCommand(cmd, true);

                numCommandsToUndo++;
            }

            var meshName = Path.GetFileNameWithoutExtension(_copyTo.MeshName.Value);
            var newMeshName = meshName + "_" + savePostFix + ".rigid_model_v2";
            var bytes = SceneSaverService.Save(true, modelNodes, newSkeleton, RmvVersionEnum.RMV2_V7);

            SaveHelper.Save(_pfs, newMeshName, null, bytes);

            // Undo the mesh transform
            for (int i = 0; i < numCommandsToUndo; i++)
                commandExecutor.Undo();

            // Reset the skeleton
            foreach (var model in modelNodes)
            {
                var header = model.Model.Header;
                header.SkeletonName = originalSkeletonName;
                model.Model.Header = header;
            }
        }
    }
}

