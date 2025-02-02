﻿using CommonControls.Common;
using CommonControls.Editors.VariantMeshDefinition;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using View3D.Animation;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FileTypes.RigidModel.VariantMeshDefinition;

namespace View3D.Services
{
    public class SceneLoader
    {
        ILogger _logger = Logging.Create<SceneLoader>();
        PackFileService _packFileService;
        ResourceLibary _resourceLibary;
        IGeometryGraphicsContextFactory _geometryContextFactory;
        IComponentManager _componentManager;
        ApplicationSettingsService _applicationSettingsService;

        public SceneLoader(ResourceLibary resourceLibary, PackFileService pfs, IGeometryGraphicsContextFactory geometryContextFactory, IComponentManager componentManager, ApplicationSettingsService applicationSettingsService)
        {
            _componentManager = componentManager;
            _packFileService = pfs;
            _resourceLibary = resourceLibary;
            _geometryContextFactory = geometryContextFactory;
            _applicationSettingsService = applicationSettingsService;
        }

        public SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player)
        {
            return Load(file, parent, player, null);
        }

        SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            if (file == null)
                throw new Exception("File is null in SceneLoader::Load");

            _logger.Here().Information($"Attempting to load file {file.Name}");

            switch (file.Extention)
            {
                case ".variantmeshdefinition":
                    LoadVariantMesh(file, ref parent, player, attachmentPointName);
                    break;

                case ".rigid_model_v2":
                    LoadRigidMesh(file, ref parent, player, attachmentPointName, false);
                    break;

                case ".wsmodel":
                    LoadWsModel(file, ref parent, player, attachmentPointName);
                    break;
                default:
                    throw new Exception("Unknown mesh extention");
            }

            return parent;
        }

        void Load(string path, SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            var file = _packFileService.FindFile(path);
            if (file == null)
            {
                _logger.Here().Error($"File {path} not found");
                return;
            }

            Load(file, parent, player , attachmentPointName);
        }


        void LoadVariantMesh(PackFile file, ref SceneNode parent, AnimationPlayer player , string attachmentPointName)
        {
            var variantMeshElement = new VariantMeshNode(file.Name);
            if (parent == null)
                parent = variantMeshElement;
            else
                parent.AddObject(variantMeshElement);

            try
            {
                var meshFile = VariantMeshToXmlConverter.Load(file);
                LoadVariantMesh(meshFile, variantMeshElement, player, attachmentPointName);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Failed to load file : " + file.Name);
                _logger.Here().Error("Error : " + e.ToString());
                throw e;
            }
        }

        void LoadVariantMesh(VariantMesh mesh, SceneNode root, AnimationPlayer player, string attachmentPointName)
        {
            if (mesh.ChildSlots.Count != 0)
                root = root.AddObject(new SlotsNode("Slots"));

            // Load model
            if (string.IsNullOrWhiteSpace(mesh.ModelReference) != true)
                 Load(mesh.ModelReference.ToLower(), root, player, attachmentPointName); 

            foreach (var slot in mesh.ChildSlots)
            {
                var slotNode = root.AddObject(new SlotNode(slot.Name + " " + slot.AttachmentPoint, slot.AttachmentPoint));

                foreach (var childMesh in slot.ChildMeshes)
                    LoadVariantMesh(childMesh, slotNode, player, attachmentPointName);

                foreach (var meshReference in slot.ChildReferences)
                    Load(meshReference.Reference.ToLower(), slotNode, player, slot.AttachmentPoint);

                for (int i = 0; i < slotNode.Children.Count(); i++)
                {
                    slotNode.Children[i].IsVisible = i == 0;
                    slotNode.Children[i].IsExpanded = false;
            
                    if (slotNode.Name.Contains("stump_"))
                    {
                        slotNode.IsVisible = false;
                        slotNode.IsExpanded = false;
                    }
                }
            }
        }

        Rmv2ModelNode LoadRigidMesh(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName, bool isParentWsModel)
        {
            var rmvModel = ModelFactory.Create().Load(file.DataSource.ReadData());

            var modelFullPath = _packFileService.GetFullPath(file);
            var modelNode = new Rmv2ModelNode(Path.GetFileName(file.Name));
            var autoResolveTexture = isParentWsModel == false && _applicationSettingsService.CurrentSettings.AutoResolveMissingTextures;
            modelNode.CreateModelNodesFromFile(rmvModel, _resourceLibary, player, _geometryContextFactory, modelFullPath, _componentManager, _packFileService, autoResolveTexture);

            foreach (var mesh in modelNode.GetMeshNodes(0))
                mesh.AttachmentPointName = attachmentPointName;

            if (parent == null)
                parent = modelNode;
            else
                parent.AddObject(modelNode);


            return modelNode;
        }

        void LoadWsModel(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            var wsModelNode = new WsModelGroup("WsModel - " + file.Name);
            if (parent == null)
                parent = wsModelNode;
            else
                parent.AddObject(wsModelNode);

            var wsMaterial = new WsMaterial(file);
            if (string.IsNullOrWhiteSpace(wsMaterial.GeometryPath) == false)
            { 
                var modelFile = _packFileService.FindFile(wsMaterial.GeometryPath);
                var modelAsBase = wsModelNode as SceneNode;
                var loadedModelNode = LoadRigidMesh(modelFile, ref modelAsBase, player , attachmentPointName, true);
               
                foreach (var materialNode in wsMaterial.MaterialList)
                {
                    var materialFile = _packFileService.FindFile(materialNode.Material);
                    var materialConfig = new WsModelMaterialFile(materialFile, "");

                    var mesh = loadedModelNode.GetMeshNode(materialNode.LodIndex, materialNode.PartIndex);
                    if (mesh == null)
                    {
                        _logger.Here().Error($"Trying to access material at index {materialNode.PartIndex} at lod {materialNode.LodIndex}, which is not found ");
                    }
                    else
                    {
                        mesh.OriginalFilePath = _packFileService.GetFullPath(file);
                        bool useAlpha = materialConfig.Alpha;
                        if (useAlpha)
                            mesh.Material.AlphaMode = AlphaMode.Transparent;
                        else
                            mesh.Material.AlphaMode = AlphaMode.Opaque;

                        foreach (var newTexture in materialConfig.Textures)
                        {
                            mesh.Material.SetTexture(newTexture.Key, newTexture.Value);
                            mesh.UpdateTexture(newTexture.Value, newTexture.Key);
                        }
                    }
                }
                

            }
        }
    }


   
}
