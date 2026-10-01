using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RadialCuboidsGenerator : MonoBehaviour {
	[SerializeField, Min(0.01f)] private float maxDistanceBetweenCuboids = 0.5f;
	[SerializeField, Min(0.01f)] private float cuboidLength = 1f;
	[SerializeField, Min(0.01f)] private float cuboidWidth = 0.5f;
	[SerializeField, Min(0.01f)] private float cuboidHeight = 2f;

	private int calculatedCuboidCount = 0;
	private float actualDistanceBetween = 0f;

	private Mesh mesh;
	private Vector3[] vertices;
	private int[] triangles;
	private float radius = 5f;

	// Методы для изменения параметров из других скриптов
	public void Generate(float newRadius) {
		radius = Mathf.Max(0.1f, newRadius);
		GenerateMesh();
	}

	public void GenerateMesh() {
		// Рассчитываем количество кубоидов
		CalculateCuboidCount();

		if (calculatedCuboidCount < 1) {
			Debug.LogError("Рассчитанное количество кубоидов должно быть больше 0. Увеличьте радиус или уменьшите расстояние между кубоидами.");
			return;
		}

		InitializeMesh();
		GenerateCuboids();
		UpdateMesh();
	}

	private void CalculateCuboidCount() {
		if (radius <= 0 || maxDistanceBetweenCuboids <= 0 || cuboidWidth <= 0)
			return;

		// Рассчитываем длину дуги окружности, которую занимает один кубоид (ширина + расстояние)
		float arcPerCuboid = cuboidWidth + maxDistanceBetweenCuboids;

		// Рассчитываем длину окружности
		float circumference = 2f * Mathf.PI * radius;

		// Рассчитываем количество кубоидов (округляем вниз, чтобы обеспечить максимальное расстояние)
		calculatedCuboidCount = Mathf.FloorToInt(circumference / arcPerCuboid);

		// Минимум 1 кубоид
		calculatedCuboidCount = Mathf.Max(1, calculatedCuboidCount);

		// Рассчитываем фактическое расстояние между кубоидами
		if (calculatedCuboidCount > 1) {
			// Общая длина дуги, занимаемая всеми кубоидами
			float totalCuboidArc = cuboidWidth * calculatedCuboidCount;
			// Оставшаяся дуга для промежутков
			float remainingArc = circumference - totalCuboidArc;
			// Фактическое расстояние между центрами
			actualDistanceBetween = remainingArc / calculatedCuboidCount + cuboidWidth;
		} else {
			actualDistanceBetween = circumference;
		}
	}

	private void InitializeMesh() {
		mesh = new Mesh();
		mesh.name = $"Radial Cuboids Mesh (Radius: {radius}, Count: {calculatedCuboidCount})";

		// Каждый кубоид имеет 8 вершин
		vertices = new Vector3[calculatedCuboidCount * 8];
		// Каждый кубоид имеет 12 треугольников (2 на каждую грань) * 3 вершины на треугольник
		triangles = new int[calculatedCuboidCount * 36];

		GetComponent<MeshFilter>().mesh = mesh;
	}

	private void GenerateCuboids() {
		if (calculatedCuboidCount <= 0)
			return;

		float angleStep = 360f / calculatedCuboidCount;

		for (int i = 0; i < calculatedCuboidCount; i++) {
			float angle = i * angleStep * Mathf.Deg2Rad;

			// Центр основания кубоида на окружности
			Vector3 center = new Vector3(
				Mathf.Cos(angle) * radius,
				0,
				Mathf.Sin(angle) * radius
			);

			// Направление от центра окружности к кубоиду
			Vector3 radialDirection = center.normalized;
			// Касательное направление (перпендикулярно радиальному)
			Vector3 tangentialDirection = new Vector3(-radialDirection.z, 0, radialDirection.x);

			// Создаем вершины для текущего кубоида
			CreateCuboidVertices(i, center, radialDirection, tangentialDirection);
			// Создаем треугольники для текущего кубоида
			CreateCuboidTriangles(i);
		}
	}

	private void CreateCuboidVertices(int cuboidIndex, Vector3 center, Vector3 radialDir, Vector3 tangentDir) {
		int vertexOffset = cuboidIndex * 8;

		// Половины размеров для удобства
		float halfLength = cuboidLength * 0.5f;
		float halfWidth = cuboidWidth * 0.5f;
		float halfHeight = cuboidHeight * 0.5f;

		// Вычисляем направления с учетом ориентации
		// Длина вдоль радиального направления
		Vector3 lengthDir = radialDir * halfLength;
		// Ширина вдоль касательного направления
		Vector3 widthDir = tangentDir * halfWidth;
		// Высота вдоль вертикали
		Vector3 heightDir = Vector3.up * halfHeight;

		// Нижние вершины (y = -halfHeight)
		vertices[vertexOffset + 0] = center - lengthDir - widthDir - heightDir; // Нижний задний левый
		vertices[vertexOffset + 1] = center - lengthDir + widthDir - heightDir; // Нижний задний правый
		vertices[vertexOffset + 2] = center + lengthDir + widthDir - heightDir; // Нижний передний правый
		vertices[vertexOffset + 3] = center + lengthDir - widthDir - heightDir; // Нижний передний левый

		// Верхние вершины (y = halfHeight)
		vertices[vertexOffset + 4] = center - lengthDir - widthDir + heightDir; // Верхний задний левый
		vertices[vertexOffset + 5] = center - lengthDir + widthDir + heightDir; // Верхний задний правый
		vertices[vertexOffset + 6] = center + lengthDir + widthDir + heightDir; // Верхний передний правый
		vertices[vertexOffset + 7] = center + lengthDir - widthDir + heightDir; // Верхний передний левый
	}

	private void CreateCuboidTriangles(int cuboidIndex) {
		int triangleOffset = cuboidIndex * 36;
		int vertexOffset = cuboidIndex * 8;

		// Нижняя грань
		triangles[triangleOffset + 0] = vertexOffset + 0;
		triangles[triangleOffset + 1] = vertexOffset + 2;
		triangles[triangleOffset + 2] = vertexOffset + 1;
		triangles[triangleOffset + 3] = vertexOffset + 0;
		triangles[triangleOffset + 4] = vertexOffset + 3;
		triangles[triangleOffset + 5] = vertexOffset + 2;

		// Верхняя грань
		triangles[triangleOffset + 6] = vertexOffset + 4;
		triangles[triangleOffset + 7] = vertexOffset + 5;
		triangles[triangleOffset + 8] = vertexOffset + 6;
		triangles[triangleOffset + 9] = vertexOffset + 4;
		triangles[triangleOffset + 10] = vertexOffset + 6;
		triangles[triangleOffset + 11] = vertexOffset + 7;

		// Передняя грань (в направлении радиуса)
		triangles[triangleOffset + 12] = vertexOffset + 3;
		triangles[triangleOffset + 13] = vertexOffset + 6;
		triangles[triangleOffset + 14] = vertexOffset + 2;
		triangles[triangleOffset + 15] = vertexOffset + 3;
		triangles[triangleOffset + 16] = vertexOffset + 7;
		triangles[triangleOffset + 17] = vertexOffset + 6;

		// Задняя грань
		triangles[triangleOffset + 18] = vertexOffset + 0;
		triangles[triangleOffset + 19] = vertexOffset + 1;
		triangles[triangleOffset + 20] = vertexOffset + 5;
		triangles[triangleOffset + 21] = vertexOffset + 0;
		triangles[triangleOffset + 22] = vertexOffset + 5;
		triangles[triangleOffset + 23] = vertexOffset + 4;

		// Левая грань (по касательной против часовой стрелки)
		triangles[triangleOffset + 24] = vertexOffset + 0;
		triangles[triangleOffset + 25] = vertexOffset + 4;
		triangles[triangleOffset + 26] = vertexOffset + 7;
		triangles[triangleOffset + 27] = vertexOffset + 0;
		triangles[triangleOffset + 28] = vertexOffset + 7;
		triangles[triangleOffset + 29] = vertexOffset + 3;

		// Правая грань
		triangles[triangleOffset + 30] = vertexOffset + 1;
		triangles[triangleOffset + 31] = vertexOffset + 2;
		triangles[triangleOffset + 32] = vertexOffset + 6;
		triangles[triangleOffset + 33] = vertexOffset + 1;
		triangles[triangleOffset + 34] = vertexOffset + 6;
		triangles[triangleOffset + 35] = vertexOffset + 5;
	}

	private void UpdateMesh() {
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		// Оптимизация
		mesh.Optimize();

		// Добавляем UV координаты для текстурирования
		AddUVs();
	}

	private void AddUVs() {
		Vector2[] uvs = new Vector2[vertices.Length];

		for (int i = 0; i < vertices.Length; i += 8) {
			// Для каждого кубоида создаем простые UV координаты
			uvs[i + 0] = new Vector2(0, 0);     // Нижний задний левый
			uvs[i + 1] = new Vector2(1, 0);     // Нижний задний правый
			uvs[i + 2] = new Vector2(1, 0);     // Нижний передний правый
			uvs[i + 3] = new Vector2(0, 0);     // Нижний передний левый
			uvs[i + 4] = new Vector2(0, 1);     // Верхний задний левый
			uvs[i + 5] = new Vector2(1, 1);     // Верхний задний правый
			uvs[i + 6] = new Vector2(1, 1);     // Верхний передний правый
			uvs[i + 7] = new Vector2(0, 1);     // Верхний передний левый
		}

		mesh.uv = uvs;
	}

	private Material GetDefaultMaterial() {
		// Создаем простой материал, если нет своего
		Material material = new Material(Shader.Find("Standard"));
		material.color = Color.cyan;
		return material;
	}

}