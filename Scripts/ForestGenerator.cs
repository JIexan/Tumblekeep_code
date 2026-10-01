using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestGenerator : MonoBehaviour
{
	public float treeRadius = 1;
	public Vector2 regionSize = Vector2.one;
	public int rejectionSamples = 30;
	public Texture2D mask;
	public TerrainGenerator tg;

	List<Vector2> points;

	public List<GameObject> trees;

	public ForestRenderer fr;

	void Start()
    {

	}

	public void Generate() {
		points = PoissonDiscSampling.GeneratePoints(treeRadius, regionSize, rejectionSamples);
		int resolution = mask.width;
		tg.Init();
		for (int y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++) {
				if (mask.GetPixel(x,y) != new Color(1f,1f,0f)) {
					Vector3 point = transform.InverseTransformPoint(tg.GetPoint(x, y));
					//point.x += tg.GetCellSize();
					point.z -= tg.GetCellSize();
					Rect square = new Rect(point.x, point.z, tg.GetCellSize(), tg.GetCellSize());
					for(int i = points.Count - 1; i >= 0; i--) {
						if(square.Contains(points[i])) {
							points.RemoveAt(i);
						}
					}

				}
			}
		}

		fr.SetTreesCount(trees.Count);
		foreach (var point in points) {
			fr.AddTree(Random.Range(0, trees.Count), transform.position + new Vector3(point.x, 0f, point.y), Quaternion.Euler(0f, Random.Range(0f, 180f), 0f), Vector3.one);
			//Instantiate(trees[Random.Range(0, trees.Count)], transform.position + new Vector3(point.x, 0f, point.y), Quaternion.Euler(0f,Random.Range(0f,180f), 0f), transform);
		}
	}

}
