﻿using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using Filetypes.ByteParsing;
using System;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V122
{
    public class CAkSound_V122 : CAkSound
    {
        public AkBankSourceData AkBankSourceData { get; set; }
        public NodeBaseParams NodeBaseParams { get; set; }

        protected override void Create(ByteChunk chunk)
        {
            AkBankSourceData = AkBankSourceData.Create(chunk);
            NodeBaseParams = NodeBaseParams.Create(chunk);
        }

        public override uint GetParentId() => NodeBaseParams.DirectParentID;
        public override uint GetSourceId() => AkBankSourceData.akMediaInformation.SourceId;
    }

    public class AkBankSourceData
    {
        public uint PluginId { get; set; }
        public ushort PluginId_type { get; set; }
        public ushort PluginId_company { get; set; }
        public SourceType StreamType { get; set; }

        public AkMediaInformation akMediaInformation { get; set; }
        public uint uSize { get; set; }
        public static AkBankSourceData Create(ByteChunk chunk)
        {
            var output = new AkBankSourceData()
            {
                PluginId = chunk.ReadUInt32(),
                //PluginId_type = chunk.ReadUShort(),
                //PluginId_company = chunk.ReadUShort(),
                StreamType = (SourceType)chunk.ReadByte()
            };


            output.PluginId_type = (ushort)(output.PluginId >> 0 & 0x000F);
            output.PluginId_company = (ushort)(output.PluginId >> 4 & 0x03FF);

            if (output.StreamType != SourceType.Straming)
            {
                //   throw new Exception();
            }

            if (output.PluginId_type == 0x02)
                output.uSize = chunk.ReadUInt32();

            output.akMediaInformation = AkMediaInformation.Create(chunk);

            return output;
        }
    }


    public class AkMediaInformation
    {
        public uint SourceId { get; set; }
        public uint uInMemoryMediaSize { get; set; }
        public byte uSourceBits { get; set; }

        public static AkMediaInformation Create(ByteChunk chunk)
        {
            return new AkMediaInformation()
            {
                SourceId = chunk.ReadUInt32(),
                uInMemoryMediaSize = chunk.ReadUInt32(),
                uSourceBits = chunk.ReadByte(),
            };
        }
    }
}
