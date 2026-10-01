using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
	private const string GrayscaleProperty = "_Grayscale";
	// Шейдер-замена для материалов без _Grayscale (стоковые URP Lit / Simple Lit): у них в сохранённых свойствах
	// лежат те же _MainTex/_Color, что читает Generic
	private const string FallbackShaderName = "Shader Graphs/Generic";

	// Все рендереры острова. Заполняется в редакторе: контекстное меню компонента или Tools/Levels/Collect Map Island Renderers
	public Renderer[] renderers;
    public string lvlId;
    private LevelStatus status;

	// Серые копии общие для всех закрытых островов: один клон на исходный материал, батчинг не ломается
	private static readonly Dictionary<Material, Material> grayscaleMaterials = new();
	private static readonly int GrayscaleId = Shader.PropertyToID(GrayscaleProperty);
	private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
	private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
	private static readonly int ColorId = Shader.PropertyToID("_Color");
	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

	private Material[][] originalMaterials;
	private bool locked;

    internal LevelStatus Status { get => status; set => status = value; }

    public void ShowTower(bool show)
    {

    }

	public void SetLevelLocked(bool locked) {
		if (this.locked == locked)
			return;
		this.locked = locked;

		if (renderers == null)
			return;
		if (originalMaterials == null) {
			originalMaterials = new Material[renderers.Length][];
			for (int i = 0; i < renderers.Length; i++)
				originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterials : null;
		}

		for (int i = 0; i < renderers.Length; i++) {
			if (renderers[i] == null)
				continue;
			renderers[i].sharedMaterials = locked ? GetGrayscale(originalMaterials[i]) : originalMaterials[i];
		}
	}

	// Верхняя точка острова над его центром: сюда карта ставит надпись закрытого острова
	public Vector3 GetTopPoint() {
		var point = transform.position;
		if (renderers == null)
			return point;
		foreach (var r in renderers) {
			if (r != null && r.enabled && r.gameObject.activeInHierarchy)
				point.y = Mathf.Max(point.y, r.bounds.max.y);
		}
		return point;
	}

	private static Material[] GetGrayscale(Material[] source) {
		var res = new Material[source.Length];
		for (int i = 0; i < source.Length; i++)
			res[i] = GetGrayscale(source[i]);
		return res;
	}

	private static Material GetGrayscale(Material source) {
		if (source == null)
			return null;
		// Клоны живут в статике: после выхода из play mode без перезагрузки домена они уничтожены — пересоздаём
		if (grayscaleMaterials.TryGetValue(source, out var gray) && gray != null)
			return gray;

		gray = new Material(source) { name = source.name + " (Grayscale)" };
		if (!gray.HasProperty(GrayscaleId)) {
			var fallback = Shader.Find(FallbackShaderName);
			if (fallback != null) {
				gray.shader = fallback;
				if (source.HasProperty(BaseMapId))
					gray.SetTexture(MainTexId, source.GetTexture(BaseMapId));
				if (source.HasProperty(BaseColorId))
					gray.SetColor(ColorId, source.GetColor(BaseColorId));
			}
		}
		gray.SetFloat(GrayscaleId, 1f);
		grayscaleMaterials[source] = gray;
		return gray;
	}

#if UNITY_EDITOR
	// Частицы не берём: у них свой шейдер без грейскейла, а на мини-островах они выключены (эффекты урона замка)
	// Имя одиновского Button пишется полностью: в глобальном неймспейсе есть свой
	// ButtonAttribute (IconsRenderer.cs), и он тихо перекрывает одиновский при обычном using
	[Sirenix.OdinInspector.Button("Collect Renderers")]
	[ContextMenu("Collect Renderers")]
	public void CollectRenderers() {
		var list = new List<Renderer>();
		foreach (var r in GetComponentsInChildren<Renderer>(true)) {
			if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
				continue;
			list.Add(r);
		}
		renderers = list.ToArray();
		UnityEditor.EditorUtility.SetDirty(this);
	}
#endif
}


enum LevelStatus
{
    open,
    current,
    close
}
