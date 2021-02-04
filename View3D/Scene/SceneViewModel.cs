﻿using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Commands;
using View3D.Components;
using View3D.Components.Component;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Scene
{
    public class SceneViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public SceneContainer Scene { get; set; } 

        string _displayName = "3d viewer";
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }


        IPackFile _packFile;
        public IPackFile MainFile
        {
            get => _packFile;
            set
            {
                _packFile = value;
                SetCurrentPackFile(_packFile);
            }
        }

        public SceneViewModel()
        {
            Scene = new SceneContainer();

            Scene.Components.Add(new FpsComponent(Scene));
            Scene.Components.Add(new KeyboardComponent(Scene));
            Scene.Components.Add(new MouseComponent(Scene));
            Scene.Components.Add(new ResourceLibary(Scene));
            Scene.Components.Add(new ArcBallCamera(Scene, new Vector3(0), 10));
            Scene.Components.Add(new PickingComponent(Scene));
            Scene.Components.Add(new SceneManager(Scene));
            Scene.Components.Add(new SelectionManager(Scene));
            Scene.Components.Add(new CommandManager(Scene));
            Scene.Components.Add(new GizmoComponent(Scene));

            Scene.SceneInitialized += OnSceneInitialized;
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            return;
            var sceneManager = scene.GetComponent<SceneManager>();

            var planeMesh = new PlaneMesh(Scene.GraphicsDevice);
            //sceneManager.RenderItems.Add(RenderItemHelper.CreateRenderItem(planeMesh, new Vector3(2, 0, 0), new Vector3(0.5f), "Item0", scene.GraphicsDevice));
            var cubeMesh = new CubeMesh(Scene.GraphicsDevice);
            sceneManager.RenderItems.Add(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(2, 0, 0),  new Vector3(0.5f),"Item0", scene.GraphicsDevice));
            sceneManager.RenderItems.Add(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(0, 0, 0),  new Vector3(0.5f),"Item1", scene.GraphicsDevice));
            sceneManager.RenderItems.Add(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(-2, 0, 0), new Vector3(0.5f),"Item2", scene.GraphicsDevice));
            //
            if (MainFile != null)
            {
                var file = MainFile as PackFile;
                var m = new RmvRigidModel(file.DataSource.ReadData(), file.FullPath);
                var meshesLod0 = m.MeshList[0];
                foreach (var mesh in meshesLod0)
                {
                    var meshInstance = new Rmv2Geometry(mesh, Scene.GraphicsDevice);
                    var newItem = RenderItemHelper.CreateRenderItem(meshInstance, new Vector3(0, 0, 0), new Vector3(1.0f), "model0", Scene.GraphicsDevice);
                    sceneManager.RenderItems.Add(newItem);
                }
            }
        }

        public string Text { get; set; }

        public bool Save()
        {
            throw new NotImplementedException();
        }
        void SetCurrentPackFile(IPackFile packedFile)
        {
        
           // Rmv2Geometry
           //

        }
    }
}
