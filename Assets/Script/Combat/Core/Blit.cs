using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BOTF3D.Core
{

    /*
	 * Blit Renderer Feature                                                https://github.com/Cyanilux/URP_BlitRenderFeature
	 * ------------------------------------------------------------------------------------------------------------------------
	 * Based on the Blit from the UniversalRenderingExamples
	 * https://github.com/Unity-Technologies/UniversalRenderingExamples/tree/master/Assets/Scripts/Runtime/RenderPasses
	 * 
	 * Extended to allow for :
	 * - Specific access to selecting a source and _destination (via current GalaxyEventCamera's color / texture id / render texture object
	 * - Automatic switching to using _AfterPostProcessTexture for After Rendering event, in order to correctly handle the blit after post processing is applied
	 * - Setting a _InverseView matrix (cameraToWorldMatrix), for shaders that might need it to handle calculations from screen space to world.
	 * 		e.g. Reconstruct world pos from depth : https://www.cyanilux.com/tutorials/depth/#blit-perspective 
	 * - (URP v10) Enabling generation of DepthNormals (_CameraNormalsTexture)
	 * 		This will only include shaders who have a DepthNormals pass (mostly Lit Shaders / Graphs)
			(workaround for Unlit Shaders / Graphs: https://gist.github.com/Cyanilux/be5a796cf6ddb20f20a586b94be93f2b)
	 * ------------------------------------------------------------------------------------------------------------------------
	 * @Cyanilux
	*/
    [CreateAssetMenu(menuName = "Feature/Blit")]
    public class Blit : ScriptableRendererFeature
    {
#if !UNITY_6000_0_OR_NEWER
        public class BlitPass : ScriptableRenderPass
        {

            public Material blitMaterial = null;
            public FilterMode filterMode { get; set; }

            private BlitSettings settings;

            private RTHandle source { get; set; }
            private RTHandle destination { get; set; }

            RTHandle m_TemporaryColorTexture;
            RTHandle m_DestinationTexture;
            string m_ProfilerTag;

            public BlitPass(RenderPassEvent renderPassEvent, BlitSettings settings, string tag)
            {
                this.renderPassEvent = renderPassEvent;
                this.settings = settings;
                blitMaterial = settings.blitMaterial;
                m_ProfilerTag = tag;
                m_TemporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture");
                if (settings.dstType == Target.TextureID)
                {
                    m_DestinationTexture = RTHandles.Alloc(settings.dstTextureId);
                }
            }

            public void Setup(RTHandle source, RTHandle destination)
            {
                this.source = source;
                this.destination = destination;

#if UNITY_2020_1_OR_NEWER
                if (settings.requireDepthNormals)
                    ConfigureInput(ScriptableRenderPassInput.Normal);
#endif
            }

            [System.Obsolete]
            public override void Execute(ScriptableRenderContext context, in RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

                if (source != null)
                {
                    Blitter.BlitCameraTexture(cmd, source, m_TemporaryColorTexture, new Vector4(1, 1, 0, 0), 0);
                    Blitter.BlitCameraTexture(cmd, source, m_DestinationTexture, new Vector4(1, 1, 0, 0), 0);
                }
                if (destination != null)
                {
                    Blitter.BlitCameraTexture(cmd, source, m_TemporaryColorTexture, new Vector4(1, 1, 0, 0), 0);
                    Blitter.BlitCameraTexture(cmd, source, m_DestinationTexture, new Vector4(1, 1, 0, 0), 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            [System.Obsolete]
            public override void OnCameraSetup(CommandBuffer cmd, in RenderingData renderingData)
            {
                m_TemporaryColorTexture = RTHandles.Alloc("m_TemporaryColorTexture", name: "m_TemporaryColorTexture");
                m_DestinationTexture = RTHandles.Alloc("m_DestinationTexture", name: "m_DestinationTexture");
            }
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                source?.Release();
                destination?.Release(); 
            }
            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (settings.dstType == Target.TextureID)
                {
                    m_DestinationTexture?.Release();
                }
                if (source == destination || (settings.srcType == settings.dstType && settings.srcType == Target.CameraColor))
                {
                    m_TemporaryColorTexture?.Release(); 
                }
            }
        }
#else
        // Unity 6000+ compatibility stub - methods are no longer overridable
        public class BlitPass : ScriptableRenderPass
        {
            public Material blitMaterial = null;
            public FilterMode filterMode { get; set; }

            private BlitSettings settings;

            RTHandle m_TemporaryColorTexture;
            RTHandle m_DestinationTexture;
            string m_ProfilerTag;

            public BlitPass(RenderPassEvent renderPassEvent, BlitSettings settings, string tag)
            {
                this.renderPassEvent = renderPassEvent;
                this.settings = settings;
                blitMaterial = settings.blitMaterial;
                m_ProfilerTag = tag;
                m_TemporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture");
                if (settings.dstType == Target.TextureID)
                {
                    m_DestinationTexture = RTHandles.Alloc(settings.dstTextureId);
                }
            }

            public void Setup(RTHandle source, RTHandle destination)
            {
                if (settings.requireDepthNormals)
                    ConfigureInput(ScriptableRenderPassInput.Normal);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                m_TemporaryColorTexture?.Release();
                m_DestinationTexture?.Release();
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (settings.dstType == Target.TextureID)
                {
                    m_DestinationTexture?.Release();
                }
                m_TemporaryColorTexture?.Release();
            }
        }
#endif

        [System.Serializable]
        public class BlitSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public Material blitMaterial = null;
            public int blitMaterialPassIndex = 0;
            public bool setInverseViewMatrix = false;
            public bool requireDepthNormals = false;

            public Target srcType = Target.CameraColor;
            public string srcTextureId = "_CameraColorTexture";
            public RenderTexture srcTextureObject;

            public Target dstType = Target.CameraColor;
            public string dstTextureId = "_BlitPassTexture";
            public RenderTexture dstTextureObject;

            public bool overrideGraphicsFormat = false;
            public UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat;
        }

        public enum Target
        {
            CameraColor,
            TextureID,
            RenderTextureObject
        }

        public BlitSettings settings = new BlitSettings();

        public BlitPass blitPass;

        private RTHandle srcIdentifier, dstIdentifier;

        public override void Create()
        {
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            blitPass = new BlitPass(settings.Event, settings, name);

            if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing)
            {
                Debug.LogWarning("Note that the \"After Rendering Post Processing\"'s Color _destination doesn't seem to work? (or might work, but doesn't contain the post processing) :( -- Use \"After Rendering\" instead!");
            }

            if (settings.graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.None)
            {
                settings.graphicsFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
            }

            //UpdateSrcIdentifier();
            //UpdateDstIdentifier();
        }

        //private void UpdateSrcIdentifier()
        //{
        //    srcIdentifier = UpdateIdentifier(settings.srcType, settings.srcTextureId, settings.srcTextureObject);
        //}

        //private void UpdateDstIdentifier()
        //{
        //    dstIdentifier = UpdateIdentifier(settings.dstType, settings.dstTextureId, settings.dstTextureObject);
        //}
        //private RTHandle UpdateID(Target type, string s, RenderTexture obj)
        //{
        //    if (type == Target.RenderTextureObject)
        //    {
        //        return obj;
        //    }
        //}
        private RenderTargetIdentifier UpdateIdentifier(Target type, string s, RenderTexture obj)
        {
            if (type == Target.RenderTextureObject)
            {
                return obj;
            }
            else if (type == Target.TextureID)
            {
                //RTHandle m_RTHandle = new RTHandle();
                //m_RTHandle.Init(s);
                //return m_RTHandle.Identifier();
                return s;
            }
            return new RenderTargetIdentifier();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {

            if (settings.blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing)
            {
            }
            else if (settings.Event == RenderPassEvent.AfterRendering && renderingData.postProcessingEnabled)
            {
                // If event is AfterRendering, and src/dst is using CameraColor, switch to _AfterPostProcessTexture instead.
                if (settings.srcType == Target.CameraColor)
                {
                    settings.srcType = Target.TextureID;
                    settings.srcTextureId = "_AfterPostProcessTexture";
                    //UpdateSrcIdentifier();
                }
                if (settings.dstType == Target.CameraColor)
                {
                    settings.dstType = Target.TextureID;
                    settings.dstTextureId = "_AfterPostProcessTexture";
                    //UpdateDstIdentifier();
                }
            }
            else
            {
                // If src/dst is using _AfterPostProcessTexture, switch back to CameraColor
                if (settings.srcType == Target.TextureID && settings.srcTextureId == "_AfterPostProcessTexture")
                {
                    settings.srcType = Target.CameraColor;
                    settings.srcTextureId = "";
                    //UpdateSrcIdentifier();
                }
                if (settings.dstType == Target.TextureID && settings.dstTextureId == "_AfterPostProcessTexture")
                {
                    settings.dstType = Target.CameraColor;
                    settings.dstTextureId = "";
                    //UpdateDstIdentifier();
                }
            }

            //var src = (settings.srcType == Target.CameraColor) ? renderer.cameraColorTarget : srcIdentifier;
            //var dest = (settings.dstType == Target.CameraColor) ? renderer.cameraColorTarget : dstIdentifier;

            //blitPass.Setup(src, dest);
            renderer.EnqueuePass(blitPass);
        }
    }
}
