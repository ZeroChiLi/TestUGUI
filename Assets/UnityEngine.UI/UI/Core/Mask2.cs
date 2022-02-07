using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Mask2", 13)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    /// <summary>
    /// A component for masking children elements.
    /// </summary>
    /// <remarks>
    /// By using this element any children elements that have masking enabled will mask where a sibling Graphic would write 0 to the stencil buffer.
    /// </remarks>
    public class Mask2 : Mask
    {
        
        [NonSerialized]
        private Material m_MaskMaterial;

        [NonSerialized]
        private Material m_UnmaskMaterial;

        protected Mask2()
        {}
        
        /// Stencil calculation time!
        override public Material GetModifiedMaterial(Material baseMaterial)
        {

            if (!MaskEnabled())
                return baseMaterial;

            var rootSortCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
            var stencilDepth = MaskUtilities.GetStencilDepth(transform, rootSortCanvas);
            if (stencilDepth >= 8)
            {
                Debug.LogWarning("Attempting to use a stencil mask with depth > 8", gameObject);
                return baseMaterial;
            }

            int desiredStencilBit = 1 << stencilDepth;

            // if we are at the first level...
            // we want to destroy what is there
            if (desiredStencilBit == 1)
            {
                var maskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Replace, CompareFunction.Always, showMaskGraphic ? ColorWriteMask.All : 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMaterial;

                var unmaskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Zero, CompareFunction.Always, 0);
                StencilMaterial.Remove(m_UnmaskMaterial);
                m_UnmaskMaterial = unmaskMaterial;
                graphic.canvasRenderer.popMaterialCount = 1;
                graphic.canvasRenderer.SetPopMaterial(m_UnmaskMaterial, 0);

                var rect = m_UnmaskMaterial.GetVector("_ClipRect");
                Debug.LogError($"_ClipRect : {rect}");

                return m_MaskMaterial;
            }

            //otherwise we need to be a bit smarter and set some read / write masks
            var maskMaterial2 = StencilMaterial.Add(baseMaterial, desiredStencilBit | (desiredStencilBit - 1), StencilOp.Replace, CompareFunction.Equal, showMaskGraphic ? ColorWriteMask.All : 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = maskMaterial2;

            graphic.canvasRenderer.hasPopInstruction = true;
            var unmaskMaterial2 = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Replace, CompareFunction.Equal, 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
            StencilMaterial.Remove(m_UnmaskMaterial);
            m_UnmaskMaterial = unmaskMaterial2;
            graphic.canvasRenderer.popMaterialCount = 1;
            graphic.canvasRenderer.SetPopMaterial(m_UnmaskMaterial, 0);

            return m_MaskMaterial;
        }
    }
}
