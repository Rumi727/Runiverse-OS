#nullable enable
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Rendering;

namespace RuniOS.Effects
{
    public enum OutlineShape { Square, Octagon, CircleJFA, SoftBlur }

    public enum OutlineVisibility
    {
        Normal = CompareFunction.LessEqual,
        AlwaysOnTop = CompareFunction.Always,
        OccludedOnly = CompareFunction.Greater
    }

    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    public class MergeOutlineEffect : MonoBehaviour
    {
        [Serializable]
        public class OutlineProfile
        {
            public string name = "New Profile";
            public Color color = Color.white;
            [Range(0, 100)] public float width = 5f;
            
            [Header("Shape Settings")]
            public OutlineShape shape = OutlineShape.CircleJFA;
            [Range(0, 1)] [Tooltip("CircleJFA 전용")] public float softness = 0.5f;
            [Range(1, 10)] [Tooltip("SoftBlur 전용 (선명도)")] public float hardness = 3f;
            [Range(1, 4)] [Tooltip("SoftBlur 전용 (반복횟수)")] public int iterations = 1;
            
            public OutlineVisibility visibility = OutlineVisibility.AlwaysOnTop;
            
            [Header("Optimization")]
            [Range(1, 8)] public int downsample = 1;
        }

        [Header("Settings List")]
        public List<OutlineProfile> profiles => _profiles;
        [SerializeField] List<OutlineProfile> _profiles = new List<OutlineProfile>();

        // --- Resources ---
        [SerializeField, HideInInspector] Shader? maskS, dilateS, compS, minS;
        [SerializeField, HideInInspector] Shader? jfaInitS, jfaFloodS, jfaCompS;
        [SerializeField, HideInInspector] Shader? blurS, compBlurS;
        
        // Materials
        Material? maskM, dilateM, compM, minM;
        Material? jfaInitM, jfaFloodM, jfaCompM;
        Material? blurM, compBlurM;

        CommandBuffer? cmd;
        RenderTexture? _maskRT;
        Camera? _cam;
        
        // --- Property IDs ---
        static readonly int _ObjectTex = Shader.PropertyToID("_ObjectTex");
        static readonly int _Color = Shader.PropertyToID("_Color");
        static readonly int _Width = Shader.PropertyToID("_Width");
        static readonly int _Softness = Shader.PropertyToID("_Softness");
        static readonly int _Hardness = Shader.PropertyToID("_Hardness");
        static readonly int _OriginalMask = Shader.PropertyToID("_OriginalMask");
        static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        static readonly int _SubTex = Shader.PropertyToID("_SubTex");
        static readonly int _Direction = Shader.PropertyToID("_Direction");
        static readonly int _StepSize = Shader.PropertyToID("_StepSize");

        void OnEnable()
        {
            _cam = GetComponent<Camera>();
            _cam.depthTextureMode |= DepthTextureMode.Depth;
            CheckResources();
        }

        void OnDisable()
        {
            if (cmd != null) { cmd.Release(); cmd = null; }
            if (_maskRT) { _maskRT.Release(); _maskRT = null; }
            Cleanup();
        }

        void Cleanup()
        {
            if(maskM) DestroyImmediate(maskM); if(dilateM) DestroyImmediate(dilateM);
            if(compM) DestroyImmediate(compM); if(minM) DestroyImmediate(minM);
            if(jfaInitM) DestroyImmediate(jfaInitM); if(jfaFloodM) DestroyImmediate(jfaFloodM);
            if(jfaCompM) DestroyImmediate(jfaCompM);
            if(blurM) DestroyImmediate(blurM); if(compBlurM) DestroyImmediate(compBlurM);
        }

        [MemberNotNullWhen(true, nameof(maskS), nameof(dilateS), nameof(compS), nameof(minS))]
        [MemberNotNullWhen(true, nameof(jfaInitS), nameof(jfaFloodS), nameof(jfaCompS))]
        [MemberNotNullWhen(true, nameof(blurS), nameof(compBlurS))]
        [MemberNotNullWhen(true, nameof(maskM), nameof(dilateM), nameof(compM), nameof(minM))]
        [MemberNotNullWhen(true, nameof(jfaInitM), nameof(jfaFloodM), nameof(jfaCompM))]
        [MemberNotNullWhen(true, nameof(blurM), nameof(compBlurM))]
        [MemberNotNullWhen(true, nameof(cmd))]
        bool CheckResources()
        {
            // Standard
            if (!maskS) maskS = Shader.Find("Hidden/RuniOS/MergeOutline/Mask");
            if (!dilateS) dilateS = Shader.Find("Hidden/RuniOS/MergeOutline/Dilate");
            if (!compS) compS = Shader.Find("Hidden/RuniOS/MergeOutline/Composite");
            if (!minS) minS = Shader.Find("Hidden/RuniOS/MergeOutline/Min");
            // JFA
            if (!jfaInitS) jfaInitS = Shader.Find("Hidden/RuniOS/MergeOutline/JFA_Init");
            if (!jfaFloodS) jfaFloodS = Shader.Find("Hidden/RuniOS/MergeOutline/JFA_Flood");
            if (!jfaCompS) jfaCompS = Shader.Find("Hidden/RuniOS/MergeOutline/JFA_Composite");
            // Blur
            if (!blurS) blurS = Shader.Find("Hidden/RuniOS/MergeOutline/Blur");
            if (!compBlurS) compBlurS = Shader.Find("Hidden/RuniOS/MergeOutline/CompositeBlur");

            if (!maskS || !dilateS || !compS || !minS || !jfaInitS || !jfaFloodS || !jfaCompS || !blurS || !compBlurS)
                return false;

            if (!maskM) maskM = new Material(maskS);
            if (!dilateM) dilateM = new Material(dilateS);
            if (!compM) compM = new Material(compS);
            if (!minM) minM = new Material(minS);
            if (!jfaInitM) jfaInitM = new Material(jfaInitS);
            if (!jfaFloodM) jfaFloodM = new Material(jfaFloodS);
            if (!jfaCompM) jfaCompM = new Material(jfaCompS);
            if (!blurM) blurM = new Material(blurS);
            if (!compBlurM) compBlurM = new Material(compBlurS);

            cmd ??= new CommandBuffer { name = "MergeOutlineRender" };
            return true;
        }

        void SetKeywords(Material mat, OutlineVisibility vis)
        {
            mat.DisableKeyword("MODE_ALWAYS");
            mat.DisableKeyword("MODE_OCCLUDED");
            switch (vis) {
                case OutlineVisibility.AlwaysOnTop: mat.EnableKeyword("MODE_ALWAYS"); break;
                case OutlineVisibility.OccludedOnly: mat.EnableKeyword("MODE_OCCLUDED"); break;
            }
        }

        [ImageEffectOpaque]
        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!CheckResources() || profiles.Count == 0) {
                Graphics.Blit(src, dest); return;
            }

            int fullW = src.width;
            int fullH = src.height;

            // 마스크 생성 (ARGBHalf, Point Filter)
            if (_maskRT == null || _maskRT.width != fullW || _maskRT.height != fullH)
            {
                if (_maskRT)
                    _maskRT.Release();
                
                _maskRT = new RenderTexture(fullW, fullH, 0, RenderTextureFormat.ARGBHalf) { filterMode = FilterMode.Point };
            }

            // 누적용 버퍼
            RenderTexture accumRT = RenderTexture.GetTemporary(src.width, src.height, 0, src.format);
            Graphics.Blit(src, accumRT);

            // 임시 RT 선언
            RenderTexture? jfa1 = null, jfa2 = null;
            RenderTexture? rt1 = null, rt2 = null, diaRT = null;

            // === 프로필 반복 ===
            for (int i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                var targets = MergeOutlineManager.GetTargets(i);

                if (targets == null || targets.Count == 0)
                    continue;
                
                int lowW = fullW / profile.downsample;
                int lowH = fullH / profile.downsample;

                // 1. 마스크 그리기
                cmd.Clear();
                cmd.SetRenderTarget(_maskRT);
                cmd.ClearRenderTarget(false, true, Color.clear);

                for (int j = 0; j < targets.Count; j++)
                {
                    MergeOutline t = targets[j];
                    if (!t || !t.renderer || !t.renderer.isVisible) continue;

                    Texture objTex = Texture2D.whiteTexture;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (t.renderer is SpriteRenderer sr && sr.sprite) objTex = sr.sprite.texture;
                    else if (t.renderer.sharedMaterial && t.renderer.sharedMaterial.mainTexture) objTex = t.renderer.sharedMaterial.mainTexture;

                    cmd.SetGlobalTexture(_ObjectTex, objTex);
                    cmd.DrawRenderer(t.renderer, maskM, 0, 0);
                }
                Graphics.ExecuteCommandBuffer(cmd);

                // 2. 셰이더 설정
                SetKeywords(jfaCompM, profile.visibility);
                SetKeywords(compM, profile.visibility);
                SetKeywords(compBlurM, profile.visibility); 

                if (profile.shape == OutlineShape.SoftBlur)
                {
                    // === Soft Blur ===
                    if (rt1 == null) {
                        rt1 = RenderTexture.GetTemporary(lowW, lowH, 0, RenderTextureFormat.ARGBHalf);
                        rt1.filterMode = FilterMode.Bilinear;
                    }
                    if (rt2 == null) {
                        rt2 = RenderTexture.GetTemporary(lowW, lowH, 0, RenderTextureFormat.ARGBHalf);
                        rt2.filterMode = FilterMode.Bilinear;
                    }
                    
                    blurM.SetFloat(_Width, profile.width / profile.downsample);
                    Graphics.Blit(_maskRT, rt1); // Copy Mask to LowRes

                    for (int k = 0; k < profile.iterations; k++)
                    {
                        Graphics.Blit(rt1, rt2, blurM, 0); // Horiz
                        Graphics.Blit(rt2, rt1, blurM, 1); // Vert
                    }
                    
                    compBlurM.SetTexture(_OriginalMask, _maskRT); // High-Res Mask
                    compBlurM.SetColor(_Color, profile.color);
                    compBlurM.SetFloat(_Hardness, profile.hardness);
                    
                    Graphics.Blit(rt1, accumRT, compBlurM);
                }
                else if (profile.shape == OutlineShape.CircleJFA)
                {
                    if (jfa1 == null) {
                        jfa1 = RenderTexture.GetTemporary(lowW, lowH, 0, RenderTextureFormat.ARGBHalf);
                        jfa1.filterMode = FilterMode.Point;
                    }
                    if (jfa2 == null) {
                        jfa2 = RenderTexture.GetTemporary(lowW, lowH, 0, RenderTextureFormat.ARGBHalf);
                        jfa2.filterMode = FilterMode.Point;
                    }

                    Graphics.Blit(_maskRT, jfa1, jfaInitM);
                    int passes = Mathf.CeilToInt(Mathf.Log(Mathf.Max(lowW, lowH), 2.0f));
                    
                    RenderTexture curr = jfa1, next = jfa2;
                    for (int p=0; p<passes; p++) {
                        float step = Mathf.Pow(2, passes - 1 - p);
                        jfaFloodM.SetFloat(_StepSize, step);
                        Graphics.Blit(curr, next, jfaFloodM);
                        (curr, next) = (next, curr);
                    }

                    jfaCompM.SetColor(_Color, profile.color);
                    jfaCompM.SetFloat(_Width, profile.width);
                    jfaCompM.SetFloat(_Softness, profile.softness);
                    jfaCompM.SetTexture(_OriginalMask, _maskRT);

                    Graphics.Blit(curr, accumRT, jfaCompM);
                }
                else
                {
                    if (rt1 == null) {
                        rt1 = RenderTexture.GetTemporary(lowW, lowH, 0, RenderTextureFormat.ARGBHalf);
                        rt1.filterMode = FilterMode.Point;
                    }
                    if (rt2 == null) {
                        rt2 = RenderTexture.GetTemporary(lowW, lowH, 0, RenderTextureFormat.ARGBHalf);
                        rt2.filterMode = FilterMode.Point;
                    }

                    float scaledWidth = profile.width / profile.downsample;
                    
                    dilateM.SetVector(_Direction, new Vector2(1, 0));
                    dilateM.SetFloat(_Width, scaledWidth);
                    Graphics.Blit(_maskRT, rt1, dilateM);
                    
                    dilateM.SetVector(_Direction, new Vector2(0, 1));
                    Graphics.Blit(rt1, rt2, dilateM);

                    RenderTexture finalRT = rt2;

                    if (profile.shape == OutlineShape.Octagon) {
                        if (diaRT == null) {
                            diaRT = RenderTexture.GetTemporary(lowW, lowH, 0, RenderTextureFormat.ARGBHalf);
                            diaRT.filterMode = FilterMode.Point;
                        }
                        float diagDist = scaledWidth * 0.7071f;
                        
                        dilateM.SetVector(_Direction, new Vector2(1, 1));
                        dilateM.SetFloat(_Width, diagDist);
                        Graphics.Blit(_maskRT, rt1, dilateM);
                        dilateM.SetVector(_Direction, new Vector2(1, -1));
                        Graphics.Blit(rt1, diaRT, dilateM);

                        minM.SetTexture(_SubTex, diaRT);
                        Graphics.Blit(rt2, rt1, minM);
                        finalRT = rt1;
                    }

                    compM.SetColor(_Color, profile.color);
                    compM.SetTexture(_OriginalMask, _maskRT);
                    compM.SetTexture(_MainTex, finalRT);
                    
                    Graphics.Blit(finalRT, accumRT, compM);
                }
            }

            Graphics.Blit(accumRT, dest);

            RenderTexture.ReleaseTemporary(accumRT);
            if(jfa1) RenderTexture.ReleaseTemporary(jfa1);
            if(jfa2) RenderTexture.ReleaseTemporary(jfa2);
            if(rt1) RenderTexture.ReleaseTemporary(rt1);
            if(rt2) RenderTexture.ReleaseTemporary(rt2);
            if(diaRT) RenderTexture.ReleaseTemporary(diaRT);
        }
    }
}