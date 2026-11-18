#nullable enable
using RuniOS.NBS;
using RuniOS.UIElements.Grids;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// A note placed in <see cref="NBSNoteGridView"/>. It starts at the tick of the note and is one tick wide.<br/>
    /// <see cref="NBSNoteGridView"/>에 놓인 노트입니다. 노트의 틱에서 시작하고 폭은 한 틱입니다.
    /// </summary>
    /// <param name="note">
    /// The note that the item shows.<br/>
    /// 아이템이 보여주는 노트입니다.
    /// </param>
    public sealed class NBSNoteGridItem(in NBSMapNote note) : PlaneGridItem
    {
        /// <summary>
        /// Gets the note that the item shows.<br/>
        /// 아이템이 보여주는 노트를 가져옵니다.
        /// </summary>
        public NBSMapNote note { get; } = note;

        // The real time the note played at while the view plays, for the flash of its block.
        internal double playedAt = double.NegativeInfinity;

        /// <inheritdoc/>
        public override float GetWidth(GridAxis axis) => (float)axis.pixelsPerUnit;
    }
}
