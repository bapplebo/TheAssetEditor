﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Rendering;
using View3D.Utility;

namespace View3D.Rendering
{
    public class LineMeshRender : IDisposable
    {
        Effect _shader;
        List<VertexPositionColor> _originalVertecies = new List<VertexPositionColor>();

        public LineMeshRender(ResourceLibary resourceLibary)
        {
            _shader = resourceLibary.GetStaticEffect(ShaderTypes.Line);
        }

        public void Clear()
        {
            _originalVertecies = new List<VertexPositionColor>();
        }

        public void CreateLineList(List<(Vector3, Vector3)> lines)
        {
            Clear();
            for (int i = 0; i < lines.Count; i++)
                AddLine(lines[i].Item1, lines[i].Item2);
        }

        public void AddCube(Matrix transform, Color color)
        {
            var offset = new Vector3(-0.5f, -0.5f, -0.5f);
            var a0 = Vector3.Transform(new Vector3(0, 0, 0) + offset, transform);
            var b0 = Vector3.Transform(new Vector3(1, 0, 0) + offset, transform);
            var c0 = Vector3.Transform(new Vector3(1, 1, 0) + offset, transform);
            var d0 = Vector3.Transform(new Vector3(0, 1, 0) + offset, transform);

            var a1 = Vector3.Transform(new Vector3(0, 0, 1) + offset, transform);
            var b1 = Vector3.Transform(new Vector3(1, 0, 1) + offset, transform);
            var c1 = Vector3.Transform(new Vector3(1, 1, 1) + offset, transform);
            var d1 = Vector3.Transform(new Vector3(0, 1, 1) + offset, transform);

            AddLine(a0, b0, color);
            AddLine(c0, d0, color);
            AddLine(b0, c0, color);
            AddLine(a0, d0, color);

            AddLine(a1, b1, color);
            AddLine(c1, d1, color);
            AddLine(b1, c1, color);
            AddLine(a1, d1, color);

            AddLine(a0, a1, color);
            AddLine(b0, b1, color);
            AddLine(c0, c1, color);
            AddLine(d0, d1, color);
        }

        public void AddLine(Vector3 pointA, Vector3 pointB)
        {
            _originalVertecies.Add(new VertexPositionColor(pointA, Color.Black));
            _originalVertecies.Add(new VertexPositionColor(pointB, Color.Black));
        }

        public void AddLine(Vector3 pointA, Vector3 pointB, Color color)
        {
            _originalVertecies.Add(new VertexPositionColor(pointA, color));
            _originalVertecies.Add(new VertexPositionColor(pointB, color));
        }

        public void CreateGrid()
        {
            int lineCount = 10;
            float spacing = 1;
            float length = 10;
            float offset = (lineCount * spacing) / 2;

            var list = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= lineCount; i++)
            {
                var start = new Vector3((i * spacing) - offset, 0, -length * 0.5f);
                var stop = new Vector3((i * spacing) - offset, 0, length * 0.5f);
                list.Add((start, stop));
            }

            for (int i = 0; i <= lineCount; i++)
            {
                var start = new Vector3(-length * 0.5f, 0, (i * spacing) - offset);
                var stop = new Vector3(length * 0.5f, 0, (i * spacing) - offset);
                list.Add((start, stop));
            }
            CreateLineList(list);
        }

        public void AddBoundingBox(BoundingBox b)
        {
            var corners = b.GetCorners();
            AddLine(corners[0], corners[1]);
            AddLine(corners[2], corners[3]);
            AddLine(corners[0], corners[3]);
            AddLine(corners[1], corners[2]);
            
            AddLine(corners[4], corners[5]);
            AddLine(corners[6], corners[7]);
            AddLine(corners[4], corners[7]);
            AddLine(corners[5], corners[6]);
            
            AddLine(corners[0], corners[4]);
            AddLine(corners[1], corners[5]);
            AddLine(corners[2], corners[6]);
            AddLine(corners[3], corners[7]);
        }

        public void Render(GraphicsDevice device, CommonShaderParameters commonShaderParameters, Matrix ModelMatrix)
        {
            if (_originalVertecies.Count() != 0)
            {
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

        public void Dispose()
        {
            //_shader.Dispose();
            _shader = null;
        }
    }
}
