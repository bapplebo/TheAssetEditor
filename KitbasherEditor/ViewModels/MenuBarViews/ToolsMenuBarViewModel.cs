﻿using Common;
using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.ErrorListDialog;
using CommonControls.MathViews;
using CommonControls.Services;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.ViewModels.BmiEditor;
using KitbasherEditor.ViewModels.MeshFitter;
using KitbasherEditor.Views.EditorViews;
using KitbasherEditor.Views.EditorViews.VertexDebugger;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using View3D.Animation;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Services;
using MessageBox = System.Windows.MessageBox;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class ToolsMenuBarViewModel : NotifyPropertyChangedImpl
    {
        IComponentManager _componentManager;
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;
        FaceEditor _faceEditor;
        IEditableMeshResolver _editableMeshResolver;
        ViewOnlySelectedComponent _viewOnlySelectedComp;
        PackFileService _packFileService;
        SkeletonAnimationLookUpHelper _skeletonHelper;
        WindowKeyboard _keyboard;

        public ICommand DivideSubMeshCommand { get; set; }
        public ICommand MergeObjectCommand { get; set; }
        public ICommand DuplicateObjectCommand { get; set; }
        public ICommand DeleteObjectCommand { get; set; }
        public ICommand MergeVertexCommand { get; set; }
        public ICommand CreateLodCommand { get; set; }
        public ICommand ExpandSelectedFacesToObjectCommand { get; set; }
        public ICommand FaceToVertexCommand { get; set; }
        public ICommand GroupCommand { get; set; }
        public ICommand ReduceMeshCommand { get; set; }
        public ICommand ToggleShowSelectionCommand { get; set; }
        public ICommand BmiToolCommand { get; set; }
        public ICommand SkeletonReshaperCommand { get; set; }
        public ICommand CreateStaticMeshesCommand { get; set; }
        public ICommand PinMeshToMeshCommand { get; set; }
        public ICommand ReRiggingToolCommand { get; set; }

        public NotifyAttr<DoubleViewModel> VertexMovementFalloff { get; set; }
        public ICommand ShowVertexDebugInfoCommand { get; set; }

        bool _showObjectTools = true;
        public bool ShowObjectTools { get => _showObjectTools; set => SetAndNotify(ref _showObjectTools, value); }


        bool _showFaceTools = false;
        public bool ShowFaceTools { get => _showFaceTools; set => SetAndNotify(ref _showFaceTools, value); }


        bool _showVertexTools = false;
        public bool ShowVertexTools { get => _showVertexTools; set => SetAndNotify(ref _showVertexTools, value); }


        bool _divideSubMeshEnabled;
        public bool DivideSubMeshEnabled { get => _divideSubMeshEnabled; set => SetAndNotify(ref _divideSubMeshEnabled, value); }

        bool _mergeMeshEnabled;
        public bool MergeMeshEnabled { get => _mergeMeshEnabled; set => SetAndNotify(ref _mergeMeshEnabled, value); }

        bool _duplicateEnabled;
        public bool DuplicateEnabled { get => _duplicateEnabled; set => SetAndNotify(ref _duplicateEnabled, value); }

        bool _deleteEnabled;
        public bool DeleteEnabled { get => _deleteEnabled; set => SetAndNotify(ref _deleteEnabled, value); }

        bool _mergeVertexEnabled;
        public bool MergeVertexEnabled { get => _mergeVertexEnabled; set => SetAndNotify(ref _mergeVertexEnabled, value); }

        bool _expandSelectedFacesToObjectEnabled;
        public bool ExpandSelectedFacesToObjectEnabled { get => _expandSelectedFacesToObjectEnabled; set => SetAndNotify(ref _expandSelectedFacesToObjectEnabled, value); }

        bool _faceToVertexEnabled;
        public bool FaceToVertexEnabled { get => _faceToVertexEnabled; set => SetAndNotify(ref _faceToVertexEnabled, value); }

        bool _groupCommandEnabled;
        public bool GroupCommandEnabled { get => _groupCommandEnabled; set => SetAndNotify(ref _groupCommandEnabled, value); }

        bool _reduceMeshCommandEnabled;
        public bool ReduceMeshCommandEnabled { get => _reduceMeshCommandEnabled; set => SetAndNotify(ref _reduceMeshCommandEnabled, value); }


        bool _toggleShowSelectionEnabled = true;
        public bool ToggleShowSelectionEnabled { get => _toggleShowSelectionEnabled; set => SetAndNotify(ref _toggleShowSelectionEnabled, value); }

        bool _bmiToolCommandEnabled = true;
        public bool BmiToolCommandEnabled { get => _bmiToolCommandEnabled; set => SetAndNotify(ref _bmiToolCommandEnabled, value); }

        bool _skeletonReshaperCommandEnabled = true;
        public bool SkeletonReshaperCommandEnabled { get => _skeletonReshaperCommandEnabled; set => SetAndNotify(ref _skeletonReshaperCommandEnabled, value); }

        bool _createStaticMeshesCommandEnabled = false;
        public bool CreateStaticMeshesCommandEnabled { get => _createStaticMeshesCommandEnabled; set => SetAndNotify(ref _createStaticMeshesCommandEnabled, value); }

        bool _pinMeshToMeshEnabled = false;
        public bool PinMeshToMeshEnabled { get => _pinMeshToMeshEnabled; set => SetAndNotify(ref _pinMeshToMeshEnabled, value); }

        bool _reRiggingToolCommandEnabled = false;
        public bool ReRiggingToolCommandEnabled { get => _reRiggingToolCommandEnabled; set => SetAndNotify(ref _reRiggingToolCommandEnabled, value); }

        bool _showVertexDebugInfoEnabled = false;
        public bool ShowVertexDebugInfoEnabled { get => _showVertexDebugInfoEnabled; set => SetAndNotify(ref _showVertexDebugInfoEnabled, value); }

        public ToolsMenuBarViewModel(IComponentManager componentManager, ToolbarCommandFactory commandFactory, PackFileService packFileService, SkeletonAnimationLookUpHelper skeletonHelper, WindowKeyboard keyboard)
        {
            _packFileService = packFileService;
            _componentManager = componentManager;
            _skeletonHelper = skeletonHelper;
            _keyboard = keyboard;

            DivideSubMeshCommand = new RelayCommand(DivideSubMesh);
            MergeObjectCommand = commandFactory.Register(new RelayCommand(MergeObjects), Key.M, ModifierKeys.Control);
            DuplicateObjectCommand = commandFactory.Register(new RelayCommand(DubplicateObject), Key.D, ModifierKeys.Control);
            DeleteObjectCommand = commandFactory.Register(new RelayCommand(DeleteObject), Key.Delete, ModifierKeys.None);
            MergeVertexCommand = new RelayCommand(MergeVertex);
            CreateLodCommand = new RelayCommand(CreateLods);
            ExpandSelectedFacesToObjectCommand = new RelayCommand(ExpandFaceSelection);
            GroupCommand = commandFactory.Register(new RelayCommand(GroupItems), Key.G, ModifierKeys.Control);
            ToggleShowSelectionCommand = commandFactory.Register(new RelayCommand(ToggleShowSelection), Key.Space, ModifierKeys.None);
            ReduceMeshCommand = new RelayCommand(ReduceMesh);
            FaceToVertexCommand = new RelayCommand(ConvertFacesToVertex);
            BmiToolCommand = new RelayCommand(OpenBmiTool);
            SkeletonReshaperCommand = new RelayCommand(OpenSkeletonReshaperTool);
            CreateStaticMeshesCommand = new RelayCommand(CreateStaticMeshes);
            PinMeshToMeshCommand = new RelayCommand(PinMeshToMesh);
            ReRiggingToolCommand = new RelayCommand(OpenReRiggingTool);
            ShowVertexDebugInfoCommand = new RelayCommand(ShowVertexDebugInfo);

            VertexMovementFalloff = new NotifyAttr<DoubleViewModel>(new DoubleViewModel());
            VertexMovementFalloff.Value.PropertyChanged += VertexMovementFalloffChanged;
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _selectionManager.SelectionChanged += OnSelectionChanged;

            _objectEditor = componentManager.GetComponent<ObjectEditor>();
            _faceEditor = componentManager.GetComponent<FaceEditor>();
            _editableMeshResolver = componentManager.GetComponent<IEditableMeshResolver>();
            _viewOnlySelectedComp = componentManager.GetComponent<ViewOnlySelectedComponent>();

            OnSelectionChanged(_selectionManager.GetState());
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            ShowObjectTools = state is ObjectSelectionState;
            ShowFaceTools = state is FaceSelectionState;
            ShowVertexTools = state is VertexSelectionState;

            DivideSubMeshEnabled = false;
            DuplicateEnabled = false;
            DeleteEnabled = false;
            MergeMeshEnabled = false;
            ExpandSelectedFacesToObjectEnabled = false;
            MergeVertexEnabled = false;
            FaceToVertexEnabled = false;
            GroupCommandEnabled = false;
            ReduceMeshCommandEnabled = false;
            BmiToolCommandEnabled = false;
            SkeletonReshaperCommandEnabled = false;
            CreateStaticMeshesCommandEnabled = false;
            PinMeshToMeshEnabled = false;
            ReRiggingToolCommandEnabled = false;
            ShowVertexDebugInfoEnabled = false;

            if (state is ObjectSelectionState objectSelection)
            {
                DivideSubMeshEnabled = objectSelection.SelectedObjects().Count == 1;
                MergeMeshEnabled = objectSelection.SelectedObjects().Count >= 2;
                DuplicateEnabled = objectSelection.SelectedObjects().Count > 0;
                DeleteEnabled = objectSelection.SelectedObjects().Count > 0;
                GroupCommandEnabled = objectSelection.SelectedObjects().Count > 0;
                ReduceMeshCommandEnabled = objectSelection.SelectedObjects().Count > 0;
                BmiToolCommandEnabled = objectSelection.SelectedObjects().Count == 1;
                SkeletonReshaperCommandEnabled = objectSelection.SelectedObjects().Count > 0;
                CreateStaticMeshesCommandEnabled = objectSelection.SelectedObjects().Count > 0;
                PinMeshToMeshEnabled = objectSelection.SelectedObjects().Count == 2;
                ReRiggingToolCommandEnabled = objectSelection.SelectedObjects().Count != 0;
            }
            else if (state is FaceSelectionState faceSelection && faceSelection.SelectedFaces.Count != 0)
            {
                DeleteEnabled = true;
                ExpandSelectedFacesToObjectEnabled = true;
                FaceToVertexEnabled = true;
                DuplicateEnabled = true;
                DivideSubMeshEnabled = true;
            }
            else if (state is VertexSelectionState vertexState )
            {
                ShowVertexDebugInfoEnabled = vertexState.SelectedVertices.Count != 0;
            }
        }

        void DivideSubMesh()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DivideIntoSubmeshes(objectSelectionState, !_keyboard.IsKeyDown(Key.LeftAlt));
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.DuplicatedSelectedFacesToNewMesh(faceSelectionState, true);
        }

        void MergeObjects()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
            {
                if (objectSelectionState.CurrentSelection().Count >= 2)
                {
                    if (!_objectEditor.CombineMeshes(objectSelectionState, out var errorList))
                        ErrorListWindow.ShowDialog("Combine Errors", errorList, false);
                }
            }
        }

        void DubplicateObject()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DuplicateObject(objectSelectionState);
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.DuplicatedSelectedFacesToNewMesh(faceSelectionState, false);
        }

        void DeleteObject()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DeleteObject(objectSelectionState);
            else if (_selectionManager.GetState() is FaceSelectionState faceSelection)
                _faceEditor.DeleteFaces(faceSelection);
        }

        void MergeVertex()
        {
        }

        void ExpandFaceSelection()
        {
            _faceEditor.GrowSelection(_selectionManager.GetState() as FaceSelectionState, !_keyboard.IsKeyDown(Key.LeftAlt));
        }

        void GroupItems()
        {
            _objectEditor.GroupItems(_selectionManager.GetState() as ObjectSelectionState);
        }

        void ReduceMesh()
        {
            var selectedObjects = _selectionManager.GetState() as ObjectSelectionState;
            if (selectedObjects == null || selectedObjects.SelectionCount() == 0)
                return;

            var meshNodes = selectedObjects.SelectedObjects()
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .ToList();

            _objectEditor.ReduceMesh(meshNodes, 0.9f, true);
        }

        void CreateLods()
        {
            var rootNode = _editableMeshResolver.GeEditableMeshRootNode();
            var lods = rootNode.GetLodNodes();

            var firtLod = lods.First();
            var lodsToGenerate = lods
                .Skip(1)
                .Take(rootNode.Children.Count - 1)
                .ToList();

            // Delete all the lods
            foreach (var lod in lodsToGenerate)
            {
                var itemsToDelete = new List<ISceneNode>();
                foreach (var child in lod.Children)
                    itemsToDelete.Add(child);

                foreach (var child in itemsToDelete)
                    child.Parent.RemoveObject(child);
            }

            var modelGroups = firtLod.GetAllModelsGrouped(false);

            //Generate lod
            for (int lodIndex = 0; lodIndex < lodsToGenerate.Count(); lodIndex++)
            {
                var deductionRatio = MeshSaverService.GetDefaultLodReductionValue(lodsToGenerate.Count(), lodIndex);

                foreach (var modelGroupCollection in modelGroups)
                {
                    ISceneNode parentNode = lodsToGenerate[lodIndex];
                    if (modelGroupCollection.Key is Rmv2LodNode == false && modelGroupCollection.Key is GroupNode groupNode)
                    {
                        parentNode = SceneNodeHelper.CloneNode(groupNode);
                        lodsToGenerate[lodIndex].AddObject(parentNode);
                    }

                    foreach (var mesh in modelGroupCollection.Value)
                    {
                        var clone = SceneNodeHelper.CloneNode(mesh);

                        var reduceValue = deductionRatio;
                        if (clone.ReduceMeshOnLodGeneration == false)
                            reduceValue = 1;
                        _objectEditor.ReduceMesh(clone, reduceValue, false);
                        parentNode.AddObject(clone);
                    }
                }
            }
        }

        void ConvertFacesToVertex()
        {
            _faceEditor.ConvertSelectionToVertex(_selectionManager.GetState() as FaceSelectionState);
        }

        void ToggleShowSelection()
        {
            _viewOnlySelectedComp.Toggle();
        }

        void OpenBmiTool()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            var meshNode = state.GetSingleSelectedObject() as Rmv2MeshNode;

            if (meshNode != null)
            {
                var skeletonName = meshNode.Geometry.ParentSkeletonName;

                var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, skeletonName);
                GameSkeleton skeleton = new GameSkeleton(newSkeletonFile, null);

                var window = new BmiWindow();
                window.DataContext = new BmiViewModel(skeleton, meshNode, _componentManager);
                window.Show();
            }
        }

        void OpenSkeletonReshaperTool()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            MeshFitterViewModel.ShowView(state.CurrentSelection(), _componentManager, _skeletonHelper, _packFileService);
        }

        void CreateStaticMeshes()
        {
            // Get the frame
            var animationPlayers = _componentManager.GetComponent<AnimationsContainerComponent>();
            var mainPlayer = animationPlayers.Get("MainPlayer");

            var frame = mainPlayer.GetCurrentAnimationFrame();
            if (frame == null)
            {
                MessageBox.Show("An animation must be playing for this tool to work");
                return;
            }

            var state = _selectionManager.GetState<ObjectSelectionState>();
            var selectedObjects = state.SelectedObjects();
            List<Rmv2MeshNode> meshes = new List<Rmv2MeshNode>();

            GroupNode groupNodeContainer = new GroupNode("staticMesh");
            var root = _editableMeshResolver.GeEditableMeshRootNode();
            var lod0 = root.GetLodNodes()[0];
            lod0.AddObject(groupNodeContainer);
            foreach (var obj in selectedObjects)
            {
                if (obj is Rmv2MeshNode meshNode)
                {
                    var cpy = SceneNodeHelper.CloneNode(meshNode);
                    groupNodeContainer.AddObject(cpy);
                    meshes.Add(cpy);
                }
            }

            var cmd = new CreateAnimatedMeshPoseCommand(meshes, frame, true);
            var commandExecutor = _componentManager.GetComponent<CommandExecutor>();
            commandExecutor.ExecuteCommand(cmd, false);
        }

        void PinMeshToMesh()
        {
            var state = _selectionManager.GetState<ObjectSelectionState>();
            var selectedObjects = state.SelectedObjects();

            var cmd = new PinMeshToMeshCommand(selectedObjects[0] as Rmv2MeshNode, selectedObjects[1] as Rmv2MeshNode);
            var commandExecutor = _componentManager.GetComponent<CommandExecutor>();
            commandExecutor.ExecuteCommand(cmd);
        }

        private void VertexMovementFalloffChanged(object sender, PropertyChangedEventArgs e)
        {
            _selectionManager.UpdateVertexSelectionFallof((float)VertexMovementFalloff.Value.Value);
        }

        private void OpenReRiggingTool()
        {
            var root = _editableMeshResolver.GeEditableMeshRootNode();
            var skeletonName = root.Skeleton.Name;
            Remap(_selectionManager.GetState<ObjectSelectionState>(), skeletonName);
        }

        void Remap(ObjectSelectionState state, string targetSkeletonName)
        {
            var existingSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, targetSkeletonName);
            if (existingSkeletonFile == null)
                throw new System.Exception("TargetSkeleton not found -" + targetSkeletonName);

            var selectedMeshses = state.SelectedObjects<Rmv2MeshNode>();
            if (selectedMeshses.Count(x => x.Geometry.VertexFormat == VertexFormat.Static) != 0)
            {
                MessageBox.Show($"A static mesh is selected, which can not be remapped");
                return;
            }

            var selectedMeshSkeletons = selectedMeshses
                .Select(x => x.Geometry.ParentSkeletonName)
                .Distinct();

            if (selectedMeshSkeletons.Count() != 1)
            {
                MessageBox.Show($"{selectedMeshSkeletons.Count()} skeleton types selected, the tool only works when a single skeleton types is selected");
                return;
            }

            var selectedMeshSkeleton = selectedMeshSkeletons.First();
            var newSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(_packFileService, selectedMeshSkeleton);

            // Ensure all the bones have valid stuff
            var allUsedBoneIndexes = new List<byte>();
            foreach (var mesh in selectedMeshses)
            {
                var boneIndexes = mesh.Geometry.GetUniqeBlendIndices();
                var activeBonesMin = boneIndexes.Min(x => x);
                var activeBonesMax = boneIndexes.Max(x => x);

                var skeletonBonesMax = newSkeletonFile.Bones.Max(x => x.Id);
                bool hasValidBoneMapping = activeBonesMin >= 0 && skeletonBonesMax >= activeBonesMax;
                if (!hasValidBoneMapping)
                {
                    MessageBox.Show($"Mesh {mesh.Name} has an invalid bones, this might cause issues. Its a result of an invalid re-rigging most of the time");
                    return;
                }
                allUsedBoneIndexes.AddRange(boneIndexes);
            }

            var animatedBoneIndexes = allUsedBoneIndexes
                .Distinct()
                .Select(x => new AnimatedBone(x, newSkeletonFile.Bones[x].Name))
                .OrderBy(x => x.BoneIndex.Value).
                ToList();

            var config = new RemappedAnimatedBoneConfiguration();


            config.MeshSkeletonName = selectedMeshSkeleton;
            config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(newSkeletonFile, animatedBoneIndexes.Select(x => x.BoneIndex.Value).ToList());

            config.ParnetModelSkeletonName = targetSkeletonName;
            config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(existingSkeletonFile);

            if (targetSkeletonName == selectedMeshSkeleton)
                MessageBox.Show("Trying to map to and from the same skeleton. This does not really make any sense if you are trying to make the mesh fit an other skeleton.", "Error", MessageBoxButton.OK);

            var window = new BoneMappingWindow(new BoneMappingViewModel(config), false);
            window.ShowDialog();

            if (window.Result == true)
            {
                var remapping = AnimatedBoneHelper.BuildRemappingList(config.MeshBones.First());
                _componentManager.GetComponent<CommandExecutor>().ExecuteCommand(new RemapBoneIndexesCommand(selectedMeshses, remapping, config.ParnetModelSkeletonName));
            }
        }

        void ShowVertexDebugInfo()
        {
            VertexDebuggerViewModel.Create(_componentManager);
        }
    }
}
