#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// Draws plain shapes in <see cref="VisualElement.generateVisualContent"/>, as grid layers and rulers do.<br/>
    /// 그리드 레이어와 눈금자처럼 <see cref="VisualElement.generateVisualContent"/>에서 단순한 도형을 그립니다.
    /// </summary>
    public static class GridMeshUtility
    {
        // A mesh uses 16-bit indices, so one allocation holds at most this many rectangles.
        const int maximumRectsPerMesh = ushort.MaxValue / 4;

        /// <summary>
        /// Draws filled rectangles. Rectangles that are fully transparent are skipped.<br/>
        /// 채워진 사각형들을 그립니다. 완전히 투명한 사각형은 건너뜁니다.
        /// </summary>
        /// <param name="context">
        /// The context passed to <see cref="VisualElement.generateVisualContent"/>.<br/>
        /// <see cref="VisualElement.generateVisualContent"/>에 전달된 컨텍스트입니다.
        /// </param>
        /// <param name="rects">
        /// The rectangles in the local pixels of the element being drawn, with their colors.<br/>
        /// 그리는 요소의 로컬 픽셀 사각형과 그 색입니다.
        /// </param>
        public static void DrawRects(this MeshGenerationContext context, ReadOnlySpan<(Rect rect, Color color)> rects)
        {
            int count = 0;
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].color.a > 0)
                    count++;
            }

            int index = 0;
            while (count > 0)
            {
                int meshCount = Math.Min(count, maximumRectsPerMesh);
                MeshWriteData mesh = context.Allocate(meshCount * 4, meshCount * 6);

                for (int written = 0; written < meshCount; index++)
                {
                    (Rect rect, Color color) = rects[index];
                    if (color.a <= 0)
                        continue;

                    Color32 tint = color;
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ), tint = tint });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ), tint = tint });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ), tint = tint });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ), tint = tint });

                    ushort first = (ushort)(written * 4);
                    mesh.SetNextIndex(first);
                    mesh.SetNextIndex((ushort)(first + 1));
                    mesh.SetNextIndex((ushort)(first + 2));
                    mesh.SetNextIndex(first);
                    mesh.SetNextIndex((ushort)(first + 2));
                    mesh.SetNextIndex((ushort)(first + 3));
                    written++;
                }

                count -= meshCount;
            }
        }
    }
}
