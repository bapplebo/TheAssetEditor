﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Rendering
{
    public class CommonShaderParameters
    {
        public Matrix View { get; set; }
        public Matrix Projection { get; set; }
        public Vector3 CameraPosition { get; set; }
        public Vector3 CameraLookAt { get; set; }
        public float LightRotationRadians { get; set; }
        public float LightIntensityMult { get; set; }
    }

    public enum RenderFormats
    { 
        SpecGloss,
        MetalRoughness
    }

}
