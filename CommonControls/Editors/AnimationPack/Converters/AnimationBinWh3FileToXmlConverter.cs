﻿using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public class AnimationBinWh3FileToXmlConverter : BaseAnimConverter<AnimationBinWh3FileToXmlConverter.XmlFormat, FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3>
    {
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public AnimationBinWh3FileToXmlConverter(SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
        }


        protected override string CleanUpXml(string xmlText)
        {
            xmlText = xmlText.Replace("</BinEntry>", "</BinEntry>\n");
            xmlText = xmlText.Replace("<Bin>", "<Bin>\n");
            xmlText = xmlText.Replace("</GeneralBinData>", "</GeneralBinData>\n");
            return xmlText;
        }

        protected override XmlFormat ConvertBytesToXmlClass(byte[] bytes)
        {
            var binFile = new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3("", bytes);
            var outputBin = new XmlFormat();

            outputBin.Version = "Wh3";
            outputBin.Data = new GeneralBinData()
            {
                TableVersion = binFile.TableVersion,
                TableSubVersion = binFile.TableSubVersion,
                Name = binFile.Name,
                MountBin = binFile.MountBin,
                SkeletonName = binFile.SkeletonName,
                LocomotionGraph = binFile.LocomotionGraph,
                UnknownValue1_RelatedToFlight = binFile.UnknownValue1
            };

            foreach (var animation in binFile.AnimationTableEntries)
            {
                outputBin.Animations.Add(new Animation()
                {
                    Slot = AnimationSlotTypeHelperWh3.GetFromId((int)animation.AnimationId).Value,
                    BlendId = animation.BlendIn,
                    BlendOut = animation.SelectionWeight,
                    Unk = animation.Unk,
                    WeaponBone = ConvertIntToBoolArray((int)animation.WeaponBools),
                });

                foreach (var animationRef in animation.AnimationRefs)
                {
                    outputBin.Animations.Last().Ref.Add(new Instance()
                    {
                        File = animationRef.AnimationFile,
                        Meta = animationRef.AnimationMetaFile,
                        Sound = animationRef.AnimationSoundMetaFile
                    });
                }
            }

            return outputBin;
        }

        protected override byte[] ConvertToAnimClassBytes(XmlFormat xmlBin, string fileName)
        {
            var binFile = new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3("", null);

            binFile.TableVersion = xmlBin.Data.TableVersion;
            binFile.TableSubVersion = xmlBin.Data.TableSubVersion;
            binFile.Name = xmlBin.Data.Name;
            binFile.MountBin = xmlBin.Data.MountBin;
            binFile.SkeletonName = xmlBin.Data.SkeletonName;
            binFile.LocomotionGraph = xmlBin.Data.LocomotionGraph;
            binFile.UnknownValue1 = xmlBin.Data.UnknownValue1_RelatedToFlight;
            binFile.Unkown = "";

            foreach (var animationEntry in xmlBin.Animations)
            {
                binFile.AnimationTableEntries.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry() 
                { 
                    AnimationId = (uint)AnimationSlotTypeHelperWh3.GetfromValue(animationEntry.Slot).Id,
                    BlendIn = animationEntry.BlendId,
                    SelectionWeight = animationEntry.BlendOut,
                    WeaponBools = CreateWeaponFlagInt(animationEntry.WeaponBone),
                    Unk = animationEntry.Unk,
                });

                foreach (var animationInstance in animationEntry.Ref)
                {
                    binFile.AnimationTableEntries.Last().AnimationRefs.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = animationInstance.File,
                        AnimationMetaFile = animationInstance.Meta,
                        AnimationSoundMetaFile = animationInstance.Sound
                    });
                }
            }

            return binFile.ToByteArray();
        }

        protected override ITextConverter.SaveError Validate(XmlFormat type, string s, PackFileService pfs)
        {
            try
            {
                if (type.Data == null)
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Data section of xml missing" };

                if (type.Animations == null)
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Animation section of xml missing" };

                if (type.Data.TableVersion != 4)
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Incorrect TableVersion - must be 4" };

                if (type.Data.TableSubVersion != 3)
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Incorrect TableSubVersion - must be 3" };

                if (string.IsNullOrWhiteSpace(type.Data.SkeletonName))
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Missing skeleton item on root" };

                if (string.IsNullOrWhiteSpace(type.Data.LocomotionGraph))
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Missing skeleton item on root" };


                var errorList = new ErrorList();
                if (_skeletonAnimationLookUpHelper.GetSkeletonFileFromName(pfs, type.Data.SkeletonName) == null)
                    errorList.Error("Skeleton", $"Skeleton {type.Data.SkeletonName} is not found");

                if (pfs.FindFile(type.Data.LocomotionGraph) == null)
                    errorList.Error("LocomotionGraph", $"LocomotionGraph {type.Data.LocomotionGraph} is not found");

                foreach (var animation in type.Animations)
                {
                    var slot = AnimationSlotTypeHelperWh3.GetfromValue(animation.Slot);
                    if (slot == null)
                        errorList.Error(animation.Slot, $"Not a valid animation slot");

                    if (animation.Ref == null || animation.Ref.Count == 0)
                        errorList.Error(animation.Slot, "Slot does not have any animations");

                    foreach (var animationRef in animation.Ref)
                    {
                        if (pfs.FindFile(animationRef.File) == null)
                            errorList.Warning(animation.Slot, $"Animation file {animationRef.File} is not found");

                        if (animationRef.Meta != "" && pfs.FindFile(animationRef.Meta) == null)
                            errorList.Warning(animation.Slot, $"Meta file {animationRef.Meta} is not found");

                        if (animationRef.Sound != "" && pfs.FindFile(animationRef.Sound) == null)
                            errorList.Warning(animation.Slot, $"Sound file {animationRef.Sound} is not found");
                    }
                }

                if (errorList.Errors.Count != 0)
                    ErrorListWindow.ShowDialog("Errors", errorList, false);
            }
            catch (Exception e)
            {
                return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = $"Unknown save error - {e.Message}" };
            }

            return null;
        }


        /*
         
         

            var lastIndex = 0;
          
            for(int i = 0; i < xmlAnimation.AnimationFragmentEntry.Count; i++)
            {
                var item = xmlAnimation.AnimationFragmentEntry[i];
                lastIndex = text.IndexOf("<AnimationFragmentEntry", lastIndex + 1, StringComparison.InvariantCultureIgnoreCase);

                if (item.Slot == null)
                    return GenerateError(text, lastIndex, "No slot provided");

                var slot = AnimationSlotTypeHelper.GetfromValue(item.Slot);
                if (slot == null)
                    return GenerateError(text, lastIndex, $"{item.Slot} is an invalid animation slot.");

                if (item.File == null)
                    return GenerateError(text, lastIndex, "No file item provided");

                if (item.Meta == null)
                    return GenerateError(text, lastIndex, "No meta item provided");

                if (item.Sound == null)
                    return GenerateError(text, lastIndex, "No sound item provided");

                if (item.BlendInTime == null)
                    return GenerateError(text, lastIndex, "No BlendInTime item provided");

                if (item.SelectionWeight == null)
                    return GenerateError(text, lastIndex, "No SelectionWeight item provided");

                if (item.WeaponBone == null)
                    return GenerateError(text, lastIndex, "No WeaponBone item provided");

                if (ValidateBoolArray(item.WeaponBone) == false)
                    return GenerateError(text, lastIndex, "WeaponBone bool array contains invalid values. Should contain 6 true/false values");
            }

            var errorList = new ErrorList();
            if(_skeletonAnimationLookUpHelper.GetSkeletonFileFromName(pfs, xmlAnimation.Skeleton) == null)
                errorList.Warning("Root", $"Skeleton {xmlAnimation.Skeleton} is not found");

            foreach (var item in xmlAnimation.AnimationFragmentEntry)
            {
                if (string.IsNullOrWhiteSpace(item.File.Value))
                    errorList.Warning(item.Slot, "Item does not have an animation");

                if(pfs.FindFile(item.File.Value) == null)
                    errorList.Warning(item.Slot, $"Animation {item.File.Value} is not found");

                if (item.Meta.Value != "" && pfs.FindFile(item.Meta.Value) == null)
                    errorList.Warning(item.Slot, $"Meta {item.Meta.Value} is not found");

                if (item.Sound.Value != "" && pfs.FindFile(item.Sound.Value) == null)
                    errorList.Warning(item.Slot, $"Sound {item.Sound.Value} is not found");
            }

            if(errorList.Errors.Count != 0)
                ErrorListWindow.ShowDialog("Errors", errorList, false);

            return null;
        }
         */


        [XmlRoot(ElementName = "Instance")]
        public class Instance
        {
            [XmlAttribute(AttributeName = "File")]
            public string File { get; set; }
            [XmlAttribute(AttributeName = "Meta")]
            public string Meta { get; set; }
            [XmlAttribute(AttributeName = "Sound")]
            public string Sound { get; set; }
        }

        [XmlRoot(ElementName = "Animation")]
        public class Animation
        {
            [XmlElement(ElementName = "Instance")]
            public List<Instance> Ref { get; set; } = new List<Instance>();
            [XmlAttribute(AttributeName = "Slot")]
            public string Slot { get; set; }
            [XmlAttribute(AttributeName = "BlendId")]
            public float BlendId { get; set; }
            [XmlAttribute(AttributeName = "SelectionWeight")]
            public float BlendOut { get; set; }
            [XmlAttribute(AttributeName = "WeaponBone")]
            public string WeaponBone { get; set; }
            [XmlAttribute(AttributeName = "Unk")]
            public bool Unk { get; set; }
        }


        [XmlRoot(ElementName = "GeneralBinData")]
        public class GeneralBinData
        {
            public uint TableVersion { get; set; }
            public uint TableSubVersion { get; set; }

            public string Name { get; set; }
            public string MountBin { get; set; }
            public string SkeletonName { get; set; }
            public string LocomotionGraph { get; set; }
            public short UnknownValue1_RelatedToFlight { get; set; }
        }

        [XmlRoot(ElementName = "Bin")]
        public class XmlFormat
        {
            [XmlElement(ElementName = "Version")]
            public string Version { get; set; }

            [XmlElement(ElementName = "GeneralBinData")]
            public GeneralBinData Data { get; set; }

            [XmlElement(ElementName = "Animation")]
            public List<Animation> Animations { get; set; } = new List<Animation>();
        }
    }
}
