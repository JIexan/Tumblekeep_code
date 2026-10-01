using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
	private class Point {
		public Vector3 position = Vector3.zero;
		public int vertexIndex = -1;

		public Point(Vector3 pos) {
			position = pos;
			vertexIndex = -1;
		}
	}

	public MeshFilter meshFilter;
	public CustomGradient gradient;
	public Texture2D texture;
	public Texture2D islandMask;

	//пиксели маски темнее этого порога (по всем каналам) считаются водой
	const byte blackThreshold = 25;

	public int resolution;
	public float scale;

	public ShoreSettings shoreSettings;

	Vector3 localUp;
	Vector3 axisA;
	Vector3 axisB;

	List<Vector3> vertices;
	List<int> triangles;

	List<Point> points;

	float cellSize = 0f;

	int[,] terrainMask;

	//клетки воды за границей маски, занятые блоками берега:
	//в terrainMask их не записать, а помнить нужно, иначе блоки налезут друг на друга у края
	HashSet<long> occupiedShoreCells = new HashSet<long>();

	//каждый уникальный не-чёрный цвет маски — отдельная зона (отдельный меш)
	int[,] zoneMask;
	List<Color32> zoneColors = new List<Color32>();

	public int ZoneCount { get { return zoneColors.Count; } }

	public Color32 GetZoneColor(int zone) { return zoneColors[zone]; }

	private void Awake() {
		Init();
	}

	private bool isLand(int x, int y) {
		return terrainMask[x,y] == 1;
	}

	//всё, что за пределами маски, считается водой: иначе блоки берега не находят себе места там,
	//где остров вплотную подходит к границе текстуры
	private int GetMask(int x, int y) {
		if (x < 0 || y < 0 || x >= resolution || y >= resolution)
			return occupiedShoreCells.Contains(CellKey(x, y)) ? 4 : 0;

		return terrainMask[x, y];
	}

	//пометить воду как занятую поставленным блоком (4)
	private void MarkShoreCell(int x, int y) {
		if (x < 0 || y < 0 || x >= resolution || y >= resolution)
			occupiedShoreCells.Add(CellKey(x, y));
		else
			terrainMask[x, y] = 4;
	}

	private static long CellKey(int x, int y) {
		return ((long)x << 32) | (uint)y;
	}

	private int ContainsPoint(Vector3 pos) {
		for (int i = 0; i < points.Count; i++) {
			if (points[i].position == pos)
				return i;
		}
		return -1;
	}

	private Point AddPoint(Vector3 pos) {
		int i = ContainsPoint(pos);
		if(i == -1) {
			Point p = new Point(pos);
			p.vertexIndex = vertices.Count;
			vertices.Add(pos);
			points.Add(p);

			return p;
		}

		return points[i];
	}

	private void AddBeachSegment(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4) {
		Point p1 = AddPoint(point1);
		Point p2 = AddPoint(point2);
		Point p3 = AddPoint(point3);
		Point p4 = AddPoint(point4);

		triangles.Add(p1.vertexIndex);
		triangles.Add(p2.vertexIndex);
		triangles.Add(p3.vertexIndex);

		triangles.Add(p3.vertexIndex);
		triangles.Add(p4.vertexIndex);
		triangles.Add(p1.vertexIndex);
	}

	public void Init() {
		localUp = Vector3.up;
		axisA = new Vector3(localUp.y, localUp.z, localUp.x);
		axisB = Vector3.Cross(localUp, axisA);

		terrainMask = new int[islandMask.width, islandMask.width];
		zoneMask = new int[islandMask.width, islandMask.width];
		zoneColors.Clear();
		occupiedShoreCells.Clear();
		Dictionary<int, int> zoneIdByColor = new Dictionary<int, int>();

		resolution = islandMask.width;

		for (int y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++) {
				Color32 c = islandMask.GetPixel(x, y);

				if (c.r < blackThreshold && c.g < blackThreshold && c.b < blackThreshold) {
					terrainMask[x, y] = 0;
					zoneMask[x, y] = -1;
					continue;
				}

				terrainMask[x, y] = 1;

				int key = (c.r << 16) | (c.g << 8) | c.b;
				int zoneId;
				if (!zoneIdByColor.TryGetValue(key, out zoneId)) {
					zoneId = zoneColors.Count;
					zoneColors.Add(c);
					zoneIdByColor.Add(key, zoneId);
				}
				zoneMask[x, y] = zoneId;
			}
		}

		cellSize = (1f / (resolution - 1)) * scale * 2;
	}

	//жутко медленно но это чисто эдиторный код который никогда не будет работать в игре. А уж эдитор как-нибуть справится
	public bool isPointsUnderTerrain(params Vector3[] points) {

		bool[] res = new bool[points.Length];
		for (int i = 0; i < res.Length; i++) {
			res[i] = false;
		}

		for (int y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++) {
				if (isLand(x, y)) {
					Vector3 terrainPoint = GetPoint(x, y);
					Rect square = new Rect(terrainPoint.x, terrainPoint.z, GetCellSize(), GetCellSize());

					for (int i = 0; i < points.Length; i++) {
						if(square.Contains(new Vector2(points[i].x, points[i].z + GetCellSize()))) {
							res[i] = true;
						}
					}				

				}
			}
		}
		foreach (var r in res) {
			if (r == false)
				return false;
		}
		return true;
	}

	public void ConstructTerrain(Mesh mesh) {
		ConstructTerrain(mesh, -1);
	}

	//zone == -1 — весь террейн одним мешем, иначе только квадраты зоны с этим индексом
	public void ConstructTerrain(Mesh mesh, int zone) {

		Init();

		vertices = new List<Vector3>(10000);
		triangles = new List<int>(100000);

		for (int y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++) {
				if (isLand(x, y) && (zone < 0 || zoneMask[x, y] == zone)) {
					AddSquare(x, y);
				}
			}
		}
		mesh.Clear();

		Vector3[] verticiesArray = vertices.ToArray();

		int[] trianglesArray = triangles.ToArray();

		mesh.vertices = verticiesArray;

		Vector2[] uvs = new Vector2[verticiesArray.Length];

		Vector3 maxPoint = GetPoint(resolution - 1, resolution - 1);
		for (int i = 0; i < verticiesArray.Length; i++) {
			uvs[i] = new Vector2(verticiesArray[i].x / maxPoint.x, verticiesArray[i].z / maxPoint.z);
		}

		mesh.triangles = trianglesArray;

		mesh.uv = uvs;

		Color[] colors = new Color[mesh.vertices.Length];

		//в вершины запекается не готовый цвет, а данные для шейдера Ground:
		//R — позиция в градиенте, G/B/A — случайные семена смещения S/V/H (0.5 — без смещения),
		//амплитуда смещения задаётся в материале свойством _HSVJitter
		for (int i = 0; i < trianglesArray.Length; i += 6) {

			Color c = texture.GetPixel((int)((verticiesArray[trianglesArray[i]].x / scale) * texture.width), (int)((verticiesArray[trianglesArray[i]].z / scale) * texture.height));

			Color baked = new Color(c.r, 0.5f, 0.5f, 0.5f);
			Color bakedJittered = new Color(c.r, Random.value, Random.value, Random.value);

			colors[trianglesArray[i]] = baked;
			colors[trianglesArray[i + 1]] = baked;
			colors[trianglesArray[i + 2]] = baked;

			colors[trianglesArray[i + 3]] = bakedJittered;
			colors[trianglesArray[i + 4]] = bakedJittered;
			colors[trianglesArray[i + 5]] = bakedJittered;
		}

		// assign the array of colors to the Mesh.
		mesh.colors = colors;

		mesh.RecalculateNormals();
	}

	public Vector3 GetPoint(int x, int y) {
		Vector2 percent = ((new Vector2(x, y) / resolution) - new Vector2(0.5f,0.5f)) * 2 * scale;
		return percent.x * axisA + percent.y * axisB;
	}

	public float GetCellSize() {
		return cellSize;
	}

	//обратный к GetPoint маппинг: точка в локальных координатах террейна (по XZ) -> индекс зоны.
	//-1 — вода или за пределами маски. Требует вызванного Init()
	public int GetZoneAt(Vector3 localPos) {
		float percentX = Vector3.Dot(localPos, axisA) / (2f * scale);
		float percentY = Vector3.Dot(localPos, axisB) / (2f * scale);

		int x = Mathf.FloorToInt((percentX + 0.5f) * resolution);
		int y = Mathf.FloorToInt((percentY + 0.5f) * resolution);

		if (x < 0 || y < 0 || x >= resolution || y >= resolution)
			return -1;

		return zoneMask[x, y];
	}

	private void AddSquare(int x, int y) {
		Vector3 point1 = GetPoint(x, y);
		Vector3 point2 = GetPoint(x + 1, y);
		Vector3 point3 = GetPoint(x + 1, y + 1);
		Vector3 point4 = GetPoint(x, y + 1);

		int i = triangles.Count;

		if (Random.value > .5f) {
			vertices.Add(point1);
			triangles.Add(i);
			vertices.Add(point2);
			triangles.Add(i + 1);
			vertices.Add(point4);
			triangles.Add(i + 2);

			vertices.Add(point2);
			triangles.Add(i + 3);
			vertices.Add(point3);
			triangles.Add(i + 4);
			vertices.Add(point4);
			triangles.Add(i + 5);

		} else {
			vertices.Add(point1);
			triangles.Add(i);
			vertices.Add(point3);
			triangles.Add(i + 1);
			vertices.Add(point4);
			triangles.Add(i + 2);

			vertices.Add(point1);
			triangles.Add(i + 3);
			vertices.Add(point2);
			triangles.Add(i + 4);
			vertices.Add(point3);
			triangles.Add(i + 5);
		}

	}

	private List<int[,]> shoreData = new List<int[,]>() {
	new int[6,6] {{2,2,2,1,1,2},
				  {2,2,1,1,4,3},
				  {2,1,1,4,4,2},
				  {1,1,4,4,2,2},
				  {1,4,4,2,2,2},
				  {2,3,2,2,2,2}},

	new int[4,4] {{2,2,2,3},
				  {2,1,1,4},
				  {2,1,4,4},
				  {3,4,4,2}},

	new int[4,4] {{2,1,1,3},
				  {1,1,4,4},
				  {1,4,4,2},
				  {2,3,2,2}},

	new int[4,4] {{1,1,1,2},
				  {1,4,4,3},
				  {1,4,2,2},
				  {2,3,2,2}},

	new int[3,3] {{1,1,2},
				  {1,4,3},
				  {2,3,2}},

	new int[4,2] {{2,3},
				  {1,4},
				  {1,4},
				  {2,3}},

	new int[2,2] {{1,3},
				  {3,4}},

	new int[3,2] {{2,3},
				  {1,4},
				  {2,3}},

	};

	public void GenerateShores() {
		Init();

		int i = 0;
		foreach(var s in shoreSettings.shores) {
			s.data = shoreData[i];
			FitBlock(s);
			i++;
		}
	}

	private void FitBlock(ShoreSettings.ShoreData data) {
		int width = data.data.GetUpperBound(1) + 1;
		int height = data.data.GetUpperBound(0) + 1;
		//блок может свисать за край маски (за ней вода), поэтому стартовые позиции начинаются
		//с отрицательных и доходят до последней клетки: иначе у острова, упёршегося в границу
		//текстуры, берег вдоль этой границы не ставится вообще
		int margin = Mathf.Max(width, height) - 1;
		//Пробежать по массиву острова и найти подходящие места для блока
		for (int y = -margin; y < resolution; y++) {
			for (int x = -margin; x < resolution; x++) {
				int rotation;
				if (CheckBlock(data.data, x, y, out rotation)) {
					Debug.Log("place for block found " + x + " " + y + " " + rotation);
					GameObject s = Instantiate(data.go[Random.Range(0,data.go.Length)]);
					s.transform.rotation = Quaternion.Euler(0, rotation, 0);
					//s.transform.position = GetPoint(x + 1, y + 1);
					if (rotation == 0)
						s.transform.position = GetPoint(x + (int)data.centerOffset.x, y + (int)data.centerOffset.y);
					else if (rotation == 90)
						s.transform.position = GetPoint(x + (width - (int)data.centerOffset.x), y + (int)data.centerOffset.y);
					else if (rotation == 180)
						s.transform.position = GetPoint(x + (height - (int)data.centerOffset.x), y + (width - (int)data.centerOffset.y));
					else if (rotation == 270)
						s.transform.position = GetPoint(x + (int)data.centerOffset.x, y + (height - (int)data.centerOffset.y));

				}
			}
		}
	}

	static readonly int[] blockRotations = { 0, 90, 180, 270 };

	private bool CheckBlock(int[,] block, int posX, int posY, out int rotation) {
		for (int r = 0; r < blockRotations.Length; r++) {
			rotation = blockRotations[r];

			int spanX, spanY;
			GetBlockSpan(block, rotation, out spanX, out spanY);

			bool placeFound = true;
			for (int y = 0; y < spanY && placeFound; y++) {
				for (int x = 0; x < spanX && placeFound; x++) {
					placeFound = CheckPoint(GetBlockValue(block, x, y, rotation), posX + x, posY + y);
				}
			}

			if (!placeFound)
				continue;

			for (int y = 0; y < spanY; y++) {
				for (int x = 0; x < spanX; x++) {
					if (GetBlockValue(block, x, y, rotation) == 4)
						MarkShoreCell(posX + x, posY + y);
				}
			}
			return true;
		}

		rotation = 0;
		return false;
	}

	//размер блока на сетке острова с учётом поворота
	private void GetBlockSpan(int[,] block, int rotation, out int spanX, out int spanY) {
		int width = block.GetUpperBound(1) + 1;
		int height = block.GetUpperBound(0) + 1;

		if (rotation == 90 || rotation == 270) {
			spanX = width;
			spanY = height;
		} else {
			spanX = height;
			spanY = width;
		}
	}

	//клетка блока, попадающая в (x, y) сетки при заданном повороте
	private int GetBlockValue(int[,] block, int x, int y, int rotation) {
		int width = block.GetUpperBound(1) + 1;
		int height = block.GetUpperBound(0) + 1;

		if (rotation == 90)
			return block[y, width - x - 1];

		if (rotation == 180)
			return block[height - x - 1, width - y - 1];

		if (rotation == 270)
			return block[height - y - 1, x];

		return block[x, y];
	}

	private bool CheckPoint(int value, int posX, int posY) {
		//2 — на клетку не смотрим вообще, она может быть и за пределами маски
		if (value == 2)
			return true;

		int mask = GetMask(posX, posY);

		//4 — свободная вода (её и займёт блок)
		if (value == 4)
			return mask == 0;

		//3 — что угодно, кроме земли
		if (value == 3)
			return mask != 1;

		return value == mask;
	}
}
