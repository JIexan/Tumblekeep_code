using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorUtils {

	public static Vector3 GetHSVDifference(Color color1, Color color2) {
		float H, S, V;

		Color.RGBToHSV(color1, out H, out S, out V);
		Vector3 col1 = new Vector3(H, S, V);

		Color.RGBToHSV(color2, out H, out S, out V);
		Vector3 col2 = new Vector3(H, S, V);

		return col2 - col1;
	}

	public static Color AddHSVOffsetToColor(Color color, Vector3 offset) {
		float H, S, V;

		Color.RGBToHSV(color, out H, out S, out V);

		return Color.HSVToRGB(H + offset.x, S + offset.y, V + offset.z);
	}

}
