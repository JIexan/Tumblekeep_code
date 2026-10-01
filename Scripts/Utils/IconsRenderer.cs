using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ButtonAttribute : PropertyAttribute {
    public string MethodName { get; }
    public ButtonAttribute(string methodName) {
        MethodName = methodName;
    }
}

public class IconsRenderer : MonoBehaviour
{
    [SerializeField]
    RenderTexture rt;

    [SerializeField]
    string fileName;

    [Button(nameof(SavePNG))]
    public bool buttonField;

    public void SavePNG() {
        var tex = new Texture2D(rt.width, rt.height);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        var path = "Assets/" + fileName + ".png";

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("Saved file to: " + path);
    }
}
