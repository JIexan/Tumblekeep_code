namespace UnityEngine.UI {
	[AddComponentMenu("Layout/Reverse Horizontal Layout Group")]

	public class ReverseHorizontalLayoutGroup : HorizontalLayoutGroup {
		protected ReverseHorizontalLayoutGroup() { }

		/// <summary>
		/// This allows for UI elements to remain in the hierarchy according to draw order
		/// while allowing them to display in reversed vertical order.
		/// </summary>
		public override void SetLayoutHorizontal() {
			RectTransform[] reversedRectChildren = new RectTransform[rectChildren.Count];
			int rectChildrenCount = rectChildren.Count;
			int lastIndex = rectChildrenCount - 1;
			for (int i = lastIndex; i >= 0; i--)
				reversedRectChildren[lastIndex - i] = rectChildren[i];
			for (int i = 0; i < rectChildrenCount; i++)
				rectChildren[i] = reversedRectChildren[i];

			base.SetLayoutHorizontal();
		}
	}
}