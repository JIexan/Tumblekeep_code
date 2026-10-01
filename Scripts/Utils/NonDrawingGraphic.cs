using UnityEngine;
using UnityEngine.UI;

/// A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
/// Useful for providing a raycast target without actually drawing anything.
/// RequireComponent is NOT inherited from Graphic, so declare it here explicitly —
/// without CanvasRenderer the canvas throws MissingComponentException on enable.
[RequireComponent(typeof(CanvasRenderer))]
public class NonDrawingGraphic : Graphic
{
    public override void SetMaterialDirty()
    {
    }

    public override void SetVerticesDirty()
    {
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
    }
}