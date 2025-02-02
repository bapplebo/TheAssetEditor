﻿using CommonControls.Common;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class MeshEditorViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        public MeshViewModel Mesh { get; set; }
        public AnimationViewModel Animation { get; set; }
        public MaterialGeneralViewModel MaterialGeneral { get; set; }
        public WeightedMaterialViewModel Material { get; set; }

        public MeshEditorViewModel(Rmv2MeshNode node, PackFileService pfs, SkeletonAnimationLookUpHelper animLookUp, IComponentManager componentManager)
        {
            Mesh = new MeshViewModel(node, componentManager);
            Animation = new AnimationViewModel(node, pfs, animLookUp);
            MaterialGeneral = new MaterialGeneralViewModel(node, pfs, componentManager);

            if (node.Material is WeightedMaterial)
                Material = new WeightedMaterialViewModel(node);
        }

        public void Dispose()
        {

        }
    }
}
