#nullable enable
using UnityEngine.Rendering;

namespace RuniOS
{
    [RequireComponent(typeof(Camera))]
    public sealed class OutlineTest : MonoBehaviour
    {
        public Renderer[] targets = [];

        public Color outlineColor = new(1f, 0.4f, 0f, 1f);

        [Range(0, 8)]
        public int thickness = 2;

        [Range(0, 4)]
        public float softness = 0.75f;

        Camera targetCamera = null!;
        Material material = null!;
        CommandBuffer? commandBuffer;

        void Awake()
        {
            commandBuffer = new CommandBuffer();
            targetCamera = GetComponent<Camera>();

            Shader shader = Shader.Find("Hidden/OutlineTest");
            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        void OnDestroy()
        {
            if (material != null)
                Destroy(material);

            commandBuffer?.Dispose();
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (targets.Length == 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            RenderTexture mask = RenderTexture.GetTemporary
            (
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32
            );

            RenderTexture temp = RenderTexture.GetTemporary
            (
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32
            );

            RenderTexture dilated = RenderTexture.GetTemporary
            (
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32
            );

            mask.filterMode = FilterMode.Bilinear;
            temp.filterMode = FilterMode.Bilinear;
            dilated.filterMode = FilterMode.Bilinear;

            mask.wrapMode = TextureWrapMode.Clamp;
            temp.wrapMode = TextureWrapMode.Clamp;
            dilated.wrapMode = TextureWrapMode.Clamp;

            DrawMask(mask);

            material.SetInt("_OutlineWidth", thickness);
            material.SetFloat("_OutlineSoftness", softness);

            // Width
            Graphics.Blit(mask, temp, material, 1);
            Graphics.Blit(temp, dilated, material, 2);

            // Softness
            Graphics.Blit(dilated, temp, material, 3);
            Graphics.Blit(temp, dilated, material, 4);

            // Composite
            material.SetTexture("_MaskTex", mask);
            material.SetTexture("_OutlineTex", dilated);
            material.SetColor("_OutlineColor", outlineColor);

            Graphics.Blit(source, destination, material, 5);

            RenderTexture.ReleaseTemporary(mask);
            RenderTexture.ReleaseTemporary(temp);
            RenderTexture.ReleaseTemporary(dilated);
        }

        void DrawMask(RenderTexture mask)
        {
            if (commandBuffer == null || material == null)
                return;

            commandBuffer.Clear();

            commandBuffer.SetRenderTarget(mask);
            commandBuffer.SetViewport(new Rect(0, 0, mask.width, mask.height));
            commandBuffer.ClearRenderTarget(false, true, Color.clear);

            commandBuffer.SetViewProjectionMatrices
            (
                targetCamera.worldToCameraMatrix,
                targetCamera.projectionMatrix
            );

            foreach (Renderer renderer in targets)
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                int submeshCount = GetSubmeshCount(renderer);

                for (int i = 0; i < submeshCount; i++)
                    commandBuffer.DrawRenderer(renderer, material, i, 0);
            }

            Graphics.ExecuteCommandBuffer(commandBuffer);
        }

        static int GetSubmeshCount(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned &&
                skinned.sharedMesh != null)
            {
                return skinned.sharedMesh.subMeshCount;
            }

            if (renderer is MeshRenderer meshRenderer &&
                meshRenderer.TryGetComponent(out MeshFilter meshFilter) &&
                meshFilter.sharedMesh != null)
            {
                return meshFilter.sharedMesh.subMeshCount;
            }

            return 1;
        }
    }
}