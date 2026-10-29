#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Resource.VectorImages
{
    public sealed class VectorImageAssetHandle(IONode node, FileMetaData fileMetaData) : AssetHandle<VectorImage>(node, fileMetaData)
    {
        protected override async UniTask<VectorImage?> Load()
        {
            string text = await node.file.ReadAllText();

            // 아이콘처럼 정해진 캔버스 안에 그려진 이미지가 내용 크기로 잘리지 않도록 SVG의 뷰포트를 유지합니다.
            SVGParser.SceneInfo sceneInfo = SVGParser.ImportSVG(new StringReader(text), ViewportOptions.PreserveViewport);
            VectorImage vectorImage = VectorUtils.BuildVectorImage(sceneInfo, sceneInfo.SceneViewport);

            vectorImage.name = node.path.GetFileNameWithoutExtension();
            vectorImage.hideFlags = HideFlags.HideAndDontSave;

            return vectorImage;
        }

        protected override void Unload(VectorImage unloadedAsset) => Object.DestroyImmediate(unloadedAsset);
    }
}
