// Copyright 2024 Argus Labs
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

namespace ArgusLabs.DF.Core
{
    public class OverlayController
    {
        private readonly Camera _camera;
        private bool _isDirty;

        private readonly Mesh _squareMesh;
        private readonly Material _squareMaterial;

        private readonly Mesh _lineMesh;
        private readonly Material _lineMaterial;

        private readonly Mesh _ringMesh;
        private readonly Material _ringMaterial;
        private readonly Material _ringSegmentMaterial;

        private GUIStyle _textRenderingStyle; // <-- Caution: Don't try to initialize this here.
        private GUIContent _textContent;
        private readonly Queue<TextCommand> _imGuiTextCommandQueue = new();
        private readonly Queue<RectCommand> _imGuiRectCommandQueue = new();
        
        private readonly MaterialPropertyBlock _lineMaterialPropertyBlock = new();
        private readonly MaterialPropertyBlock _ringMaterialPropertyBlock = new();
        private readonly MaterialPropertyBlock _squareMaterialPropertyBlock = new();
        private readonly int _colorId = Shader.PropertyToID("_Color");
        private readonly int _p1Id = Shader.PropertyToID("_P1");
        private readonly int _p2Id = Shader.PropertyToID("_P2");
        private readonly int _radiusId = Shader.PropertyToID("_Radius");
        private readonly int _innerIsovalueId = Shader.PropertyToID("_InnerIsovalue");
        private readonly int _outerIsovalueId = Shader.PropertyToID("_OuterIsovalue");
        private readonly int _progressId = Shader.PropertyToID("_Progress");
        private readonly int _segmentCountId = Shader.PropertyToID("_SegmentCount");
        private readonly int _segmentAngleOffsetId = Shader.PropertyToID("_SegmentAngleOffset");
        private readonly int _frequencyId = Shader.PropertyToID("_Frequency");
        private readonly int _strokeWidthId = Shader.PropertyToID("_StrokeWidth");
        private readonly int _uniformSizeId = Shader.PropertyToID("_UniformSizeId");
        
        private readonly int _angleId = Shader.PropertyToID("_Angle");
        private readonly int _dashScaleId = Shader.PropertyToID("_DashScale");
        private readonly int _dashToGapRatioId = Shader.PropertyToID("_DashToGapRatio");
        private readonly int _distanceId = Shader.PropertyToID("_Distance");
        
        private readonly Texture _whiteTexture;
        
        public Font GuiFont { get; set; }
        public bool IsDebugTextEnabled { get; set; }
        
        /// <summary> Multiply by this to scale with the camera. </summary>
        private float ScreenScale => 1f / (.5f * (Screen.height / _camera.orthographicSize));
        
        private struct TextCommand
        {
            public int FrameIndex;
            public Rect Aabb;
            public int FontSize;
            public Color Color;
            public string Text;
            public bool IsCentered;
        }

        private struct RectCommand
        {
            public int FrameIndex;
            public Rect Aabb;
            public Material Material;
        }
        
        public OverlayController(Camera camera, GameObject squarePrefab, GameObject linePrefab, GameObject ringPrefab, GameObject ringSegmentPrefab)
        {
            _camera = camera;

            _squareMesh = squarePrefab.GetComponent<MeshFilter>().sharedMesh;
            _squareMaterial = new Material(squarePrefab.GetComponent<Renderer>().sharedMaterial);

            _lineMesh = linePrefab.GetComponent<MeshFilter>().sharedMesh;
            _lineMaterial = new Material(linePrefab.GetComponent<Renderer>().sharedMaterial);

            _ringMesh = ringPrefab.GetComponent<MeshFilter>().sharedMesh;
            _ringMaterial = new Material(ringPrefab.GetComponent<Renderer>().sharedMaterial);
            _ringSegmentMaterial = new Material(ringSegmentPrefab.GetComponent<Renderer>().sharedMaterial);
            
            _whiteTexture = Texture2D.whiteTexture;

            IsDebugTextEnabled = Application.isEditor;
        }
        
        // Sorry this is messy. It'd make sense to use a GUIStyle if GUIStyles weren't heap allocated, but they are.
        public void DrawImGuiText(Vector2 position, int fontSize, Color color, string text, bool isDebugText,
            bool canScaleToFit = true, bool isCentered = false)
        {
            if (isDebugText && !IsDebugTextEnabled)
                return;
            
            if (canScaleToFit)
                fontSize = Mathf.RoundToInt(fontSize * Screen.height / 1080f);
            
            var cmd = new TextCommand
            {
                FrameIndex = Time.frameCount,
                Aabb = new Rect(new Vector2(position.x, Screen.height - position.y - fontSize), new Vector2(Screen.width - (int)position.x, Screen.height - (int)position.y)),
                FontSize = fontSize, // <-- TODO: Pull magic 1080 from somewhere reasonable.
                Color = color,
                Text = text,
                IsCentered = isCentered
            };
            
            _imGuiTextCommandQueue.Enqueue(cmd);
        }

        public void DrawScreenRectangle(Rect aabb, Material material)
            => _imGuiRectCommandQueue.Enqueue(new RectCommand { FrameIndex = Time.frameCount, Aabb = aabb, Material = material});

        public void ProcessImGuiCommands()
        {
            _textRenderingStyle ??= new(GUI.skin.label);
            _textContent ??= new GUIContent("");

            for (int i = 0; i < _imGuiTextCommandQueue.Count; ++i)
            {
                var cmd = _imGuiTextCommandQueue.Dequeue();

                if (cmd.FrameIndex < Time.frameCount)
                    continue;
                
                _textRenderingStyle.font = GuiFont;
                _textRenderingStyle.fontSize = cmd.FontSize;
                _textRenderingStyle.active.textColor = cmd.Color;
                _textRenderingStyle.focused.textColor = cmd.Color;
                _textRenderingStyle.hover.textColor = cmd.Color;
                _textRenderingStyle.normal.textColor = cmd.Color;

                Rect aabb = cmd.Aabb;
                
                if (cmd.IsCentered)
                {
                    _textContent.text = cmd.Text;
                    var size = _textRenderingStyle.CalcSize(_textContent);
                    
                    aabb.size = size;
                    aabb.center = cmd.Aabb.position + Vector2.down * .5f * size.y;
                }
                
                GUI.Label(aabb, cmd.Text, _textRenderingStyle);
                
                _imGuiTextCommandQueue.Enqueue(cmd);
            }
            
            for (int i = 0; i < _imGuiRectCommandQueue.Count; ++i)
            {
                var cmd = _imGuiRectCommandQueue.Dequeue();
                
                if (cmd.FrameIndex < Time.frameCount)
                    continue;
                
                Graphics.DrawTexture(cmd.Aabb, _whiteTexture, cmd.Material);
                _imGuiRectCommandQueue.Enqueue(cmd);
            }
        }

        public void DrawWorldSquare(Vector3 center, float size, float strokeWidth, Color color)
        {
            Assert.IsTrue(size > 0f);
            var worldMatrix = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one*size);
            var rp = new RenderParams(_squareMaterial);
            rp.matProps = _squareMaterialPropertyBlock;
            rp.matProps.SetColor(_colorId, color);
            rp.matProps.SetFloat(_uniformSizeId, size);
            rp.matProps.SetFloat(_strokeWidthId, strokeWidth);
            rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
            
            Graphics.RenderMesh(rp, _squareMesh, 0, worldMatrix);
        }

        /// <summary>
        /// Draw a line in world space.
        /// Note: While the points are used by the shader, this could have been implemented without them, and it would have been simpler. It's fine for now.
        /// </summary>
        /// <param name="a">starting point (P1 in the shader)</param>
        /// <param name="b">ending point (P2 in the shader)</param>
        /// <param name="color">color with alpha</param>
        /// <param name="dashScreenScale">Scale in screen space</param>
        /// <param name="dashToGapRatio">How long should the dashes be compared to empty space?</param>
        /// <param name="bevelScale">Scale up to pack dashes closer together. Proportions are maintained.</param>
        public void DrawWorldLine(Vector2 a, Vector2 b, Color color, Vector2 dashScreenScale, float dashToGapRatio, float bevelScale = 0f)
        {
            Assert.IsTrue(dashScreenScale.x > 0f);
            Assert.IsTrue(dashScreenScale.y > 0f);
            Assert.IsTrue(dashToGapRatio >= 0f);
            Assert.IsTrue(dashToGapRatio <= 1f);
            Assert.IsTrue(bevelScale >= 0f);

            Vector2 normal = (b - a).normalized;
            float angle = atan2(normal.y, normal.x);
            
            Vector2 dashScale = dashScreenScale * ScreenScale;
            Vector3 scale = Vector3.one;
            
            float d = (b - a).magnitude;
            Matrix4x4 worldMatrix = Matrix4x4.TRS(a, Quaternion.identity, scale);
            
            var rp = new RenderParams(_lineMaterial);
            rp.matProps = _lineMaterialPropertyBlock;
            rp.matProps.SetFloat(_angleId, angle);
            rp.matProps.SetColor(_colorId, color);
            rp.matProps.SetFloat(_distanceId, d);
            rp.matProps.SetVector(_dashScaleId, dashScale);
            rp.matProps.SetFloat(_dashToGapRatioId, dashToGapRatio);
            rp.matProps.SetFloat(_radiusId, bevelScale);
            rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
            
            Graphics.RenderMesh(rp, _lineMesh, 0, worldMatrix);
        }

        public void DrawWorldDotWithSsr(Vector3 position, float screenSpaceRadius, Color color)
        {
            screenSpaceRadius *= ScreenScale;
            
            var scale = Vector3.one;
            
            if (screenSpaceRadius > 1f)
            {
                scale.x = scale.y = screenSpaceRadius;
                screenSpaceRadius = 1f;
            }
            
            var worldMatrix = Matrix4x4.TRS(position, Quaternion.identity, scale);
            var rp = new RenderParams(_ringMaterial);
            rp.matProps = _ringMaterialPropertyBlock;
            
            rp.matProps.SetFloat(_innerIsovalueId, 0f);
            rp.matProps.SetFloat(_outerIsovalueId, screenSpaceRadius);
            rp.matProps.SetColor(_colorId, color);
            rp.matProps.SetFloat(_progressId, 1f);
            rp.matProps.SetFloat(_segmentCountId, 0);
            rp.matProps.SetFloat(_segmentAngleOffsetId, 0f);
            rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
            
            Graphics.RenderMesh(rp, _ringMesh, 0, worldMatrix);
        }
        
        /// <summary>
        /// Draw a ring in world space.
        /// </summary>
        /// <param name="position">world space position</param>
        /// <param name="radius">world space radius</param>
        /// <param name="color">color with alpha</param>
        /// <param name="strokeWidth">line width</param>
        /// <param name="progress">normalized progress (for a radial progress bar)</param>
        public void DrawWorldRing(
            Vector3 position,
            float radius,
            Color color,
            float strokeWidth = 1f,
            float progress = 1f)
        {
            DrawWorldUberRing(position, radius, color, strokeWidth, progress);
        }

        /// <summary>
        /// Draw a ring in world space.
        /// </summary>
        /// <param name="position">world space position</param>
        /// <param name="radius">world space radius</param>
        /// <param name="color">color with alpha</param>
        /// <param name="strokeWidth">line width</param>
        /// <param name="minSegments">minimum number of segments when a length > 0 is specified</param>
        /// <param name="desiredSegmentScreenSpaceLength">How long should each segment roughly be in screen space? (0 will force use of minSegments.)</param>
        public void DrawWorldRing(
            Vector3 position,
            float radius,
            Color color,
            float strokeWidth,
            int minSegments,
            int desiredSegmentScreenSpaceLength = 0)
        {
            DrawWorldUberRing(position, radius, color, strokeWidth, 1f, true, minSegments, desiredSegmentScreenSpaceLength);
        }
        
        private void DrawWorldUberRing(
            Vector3 position,
            float radius,
            Color color,
            float strokeWidth = 1f,
            float progress = 1f,
            bool willCenterOnRadius = true,
            int minSegments = 0,
            int desiredSegmentScreenSpaceLength = 0,
            float segmentAngleOffset = 0f)
        {
            var material = desiredSegmentScreenSpaceLength == 0
                ? _ringMaterial
                : _ringSegmentMaterial;

            strokeWidth *= ScreenScale;
            radius *= 2f; // Twice world space to match what we currently have. Might be better correct?
            
            int segmentCount = 0;
            
            switch (desiredSegmentScreenSpaceLength)
            {
                case 0 when (minSegments == 0):
                    DrawRing();
                    return;
                case 0:
                    segmentCount = minSegments;
                    DrawRing();
                    return;
            }

            float initialSegmentCount = (PI * radius * .5f / ScreenScale / desiredSegmentScreenSpaceLength);
            int potSegmentCount = Mathf.NextPowerOfTwo(max(minSegments, Mathf.FloorToInt(initialSegmentCount * .5f)));
            int nextSegmentCount = Mathf.NextPowerOfTwo(potSegmentCount + 1);
            
            segmentCount = potSegmentCount;
            progress = Mathf.InverseLerp(potSegmentCount, nextSegmentCount, initialSegmentCount);
            DrawRing();
            
            void DrawRing()
            {
                float innerRadius = willCenterOnRadius
                    ? max(0f, radius - strokeWidth)
                    : max(0f, radius);

                float outerRadius = willCenterOnRadius
                    ? radius + strokeWidth
                    : radius + strokeWidth * 2f;

                var scale = Vector3.one;

                if (outerRadius > 1f)
                {
                    scale.x = scale.y = outerRadius;
                    innerRadius = unlerp(0f, outerRadius, innerRadius);
                    outerRadius = 1f;
                }

                var rotation = progress >= 0f ? Quaternion.identity : Quaternion.Euler(0f, -180f, 0f);
                var worldMatrix = Matrix4x4.TRS(position, rotation, scale);
                var rp = new RenderParams(material);
                rp.matProps = _ringMaterialPropertyBlock;

                rp.matProps.SetFloat(_innerIsovalueId, innerRadius);
                rp.matProps.SetFloat(_outerIsovalueId, outerRadius);
                rp.matProps.SetColor(_colorId, color);
                rp.matProps.SetFloat(_progressId, abs(progress));
                rp.matProps.SetFloat(_segmentCountId, segmentCount);
                rp.matProps.SetFloat(_segmentAngleOffsetId, segmentAngleOffset);
                rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);

                Graphics.RenderMesh(rp, _ringMesh, 0, worldMatrix);
            }
        }
    }
}