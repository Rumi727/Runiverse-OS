using UnityEngine.UIElements;

namespace RuniOS
{
    public static class IMGUIUtility
    {
        public static IMGUIContainer? currentIMGUIContainer { get; internal set; }

        public static void UpdateContainerHeight(float height)
        {
            if (currentIMGUIContainer != null)
            {
                StyleLength lastHeight = currentIMGUIContainer.style.height;
                currentIMGUIContainer.style.height = new Length(height);
                currentIMGUIContainer.style.height = lastHeight;
            }
        }
    }
}