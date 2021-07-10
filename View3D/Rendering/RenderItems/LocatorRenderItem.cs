﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{
    public class LocatorRenderItem : IRenderItem
    {
        Effect _shader;
        Vector3 _pos;
        float _size;

        public LocatorRenderItem(Effect shader, Vector3 pos, float size)
        {
            _shader = shader;
            _pos = pos;
            _size = size;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Render(device, parameters, Matrix.Identity);
        }

        public void Render(GraphicsDevice device, CommonShaderParameters commonShaderParameters, Matrix ModelMatrix)
        {
            var _originalVertecies = new VertexPositionColor[6];
            Color colour = Color.Red;

            var halfLength = _size / 2;
            _originalVertecies[0] = new VertexPositionColor(_pos + new Vector3(-halfLength, 0, 0), colour);
            _originalVertecies[1] = new VertexPositionColor(_pos + new Vector3(halfLength, 0, 0), colour);
            _originalVertecies[2] = new VertexPositionColor(_pos + new Vector3(0, -halfLength, 0), colour);
            _originalVertecies[3] = new VertexPositionColor(_pos + new Vector3(0, halfLength, 0), colour);
            _originalVertecies[4] = new VertexPositionColor(_pos + new Vector3(0, 0, -halfLength), colour);
            _originalVertecies[5] = new VertexPositionColor(_pos + new Vector3(0, 0, halfLength), colour);

            _shader.Parameters["View"].SetValue(commonShaderParameters.View);
            _shader.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            _shader.Parameters["World"].SetValue(ModelMatrix);

            foreach (var pass in _shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, _originalVertecies.ToArray(), 0, _originalVertecies.Count() / 2);
            }
        }
    }
}
