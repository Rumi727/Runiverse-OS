#nullable enable
using UnityEngine;

namespace RuniEngine
{
    public static class ResourceUtility
    {
        /// <summary>
        /// 빈 게임 오브젝트
        /// </summary>
        public static Transform emptyTransform
        {
            get
            {
                if (_emptyTransform == null)
                    _emptyTransform = Resources.Load<Transform>("Empty Transform");

                return _emptyTransform;
            }
        }
        static Transform? _emptyTransform;

        /// <summary>
        /// 사각 트랜스폼이 추가된 빈 게임 오브젝트
        /// </summary>
        public static RectTransform emptyRectTransform
        {
            get
            {
                if (_emptyRectTransform == null)
                    _emptyRectTransform = Resources.Load<RectTransform>("Empty Rect Transform");

                return _emptyRectTransform;
            }
        }
        static RectTransform? _emptyRectTransform;



        /// <summary>
        /// 기본 메테리얼
        /// </summary>
        public static Material defaultMaterial
        {
            get
            {
                if (_defaultMaterial == null)
                    _defaultMaterial = Resources.Load<Material>("Default Material");

                return _defaultMaterial;
            }
        }
        static Material? _defaultMaterial;

        /// <summary>
        /// 단색 메테리얼
        /// </summary>
        public static Material coloredMaterial
        {
            get
            {
                if (_coloredMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                    _coloredMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    _coloredMaterial.SetInt(srcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _coloredMaterial.SetInt(dstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _coloredMaterial.SetInt(cull, (int)UnityEngine.Rendering.CullMode.Off);
                    _coloredMaterial.SetInt(zWrite, 0);
                }

                return _coloredMaterial;
            }
        }
        static Material? _coloredMaterial;
        static readonly int srcBlend = Shader.PropertyToID("_SrcBlend");
        static readonly int dstBlend = Shader.PropertyToID("_DstBlend");
        static readonly int cull = Shader.PropertyToID("_Cull");
        static readonly int zWrite = Shader.PropertyToID("_ZWrite");
    }
}
