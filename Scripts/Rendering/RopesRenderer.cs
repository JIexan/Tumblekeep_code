using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RopesRenderer : MonoBehaviour
{
    public Vector2 startRopeSize;
    public Vector2 endRopeSize;
    public List<Rope> ropes;

    [System.Serializable]
    public class Rope {
        public Transform start;
        public Transform end;
	}

    private MeshFilter meshFiler;   
    private Mesh m;
    private List<Vector3> meshPoints;

    private Vector3[] ropePoints = new Vector3[8];
    private Vector3[] initialPoints = new Vector3[8];

    private void Awake() {
        m = new Mesh();
        meshFiler = GetComponent<MeshFilter>();

        initialPoints[0] = new Vector3(-startRopeSize.x, startRopeSize.y, 0f);
        initialPoints[1] = new Vector3(startRopeSize.x, startRopeSize.y, 0f);
        initialPoints[2] = new Vector3(startRopeSize.x, -startRopeSize.y, 0f);
        initialPoints[3] = new Vector3(-startRopeSize.x, -startRopeSize.y, 0f);
        initialPoints[4] = new Vector3(-endRopeSize.x, endRopeSize.y, 0f);
        initialPoints[5] = new Vector3(endRopeSize.x, endRopeSize.y, 0f);
        initialPoints[6] = new Vector3(endRopeSize.x, -endRopeSize.y, 0f);
        initialPoints[7] = new Vector3(-endRopeSize.x, -endRopeSize.y, 0f);
    }

    private void AddQuad(List<Vector3> points, List<int> triangles, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4) {
        int i = points.Count;
        points.Add(p1);
        points.Add(p2);
        points.Add(p3);
        points.Add(p4);

        triangles.Add(i);
        triangles.Add(i + 3);
        triangles.Add(i + 1);


        triangles.Add(i + 1);
        triangles.Add(i + 3);
        triangles.Add(i + 2);
    }

	void Start()
    {
        meshFiler.mesh = m;

        meshPoints = new List<Vector3>(ropes.Count * 16);
        List<int> meshTriangles = new List<int>(ropes.Count * 8 * 3);
        List<Vector2> uv = new List<Vector2>(ropes.Count * 16);

        foreach (var rope in ropes) {
            Vector3 ropeDirection = (rope.end.position - rope.start.position).normalized;
            rope.start.forward = ropeDirection;
            rope.end.forward = ropeDirection;

            UpdateRopePoints(rope);

            AddQuad(meshPoints, meshTriangles,
                ropePoints[0],
                ropePoints[1],
                ropePoints[5],
                ropePoints[4]);

            AddQuad(meshPoints, meshTriangles,
                ropePoints[1],
                ropePoints[2],
                ropePoints[6],
                ropePoints[5]);

            AddQuad(meshPoints, meshTriangles,
                ropePoints[2],
                ropePoints[3],
                ropePoints[7],
                ropePoints[6]);

            AddQuad(meshPoints, meshTriangles,
                ropePoints[3],
                ropePoints[0],
                ropePoints[4],
                ropePoints[7]);

            for(int i = 0; i < 4; i++) {
                uv.Add(new Vector2(0f, 0f));
                uv.Add(new Vector2(1f, 0f));
                uv.Add(new Vector2(1f, 1f));
                uv.Add(new Vector2(0f, 1f));
            }


        }

        m.SetVertices(meshPoints);
        m.SetTriangles(meshTriangles, 0);
        m.RecalculateNormals();
        m.SetUVs(0, uv);
    }

    private void UpdateRopePoints(Rope rope) {
        ropePoints[0] = transform.InverseTransformPoint(rope.start.TransformPoint(initialPoints[0]));
        ropePoints[1] = transform.InverseTransformPoint(rope.start.TransformPoint(initialPoints[1]));
        ropePoints[2] = transform.InverseTransformPoint(rope.start.TransformPoint(initialPoints[2]));
        ropePoints[3] = transform.InverseTransformPoint(rope.start.TransformPoint(initialPoints[3]));
        ropePoints[4] = transform.InverseTransformPoint(rope.end.TransformPoint(initialPoints[4]));
        ropePoints[5] = transform.InverseTransformPoint(rope.end.TransformPoint(initialPoints[5]));
        ropePoints[6] = transform.InverseTransformPoint(rope.end.TransformPoint(initialPoints[6]));
        ropePoints[7] = transform.InverseTransformPoint(rope.end.TransformPoint(initialPoints[7]));
    }

    // Update is called once per frame
    void Update()
    {
        int i = 0;
        foreach (var rope in ropes) {
            Vector3 ropeDirection = (rope.end.position - rope.start.position).normalized;
            rope.start.forward = ropeDirection;
            rope.end.forward = ropeDirection;

            UpdateRopePoints(rope);


            meshPoints[i] = ropePoints[0];
            meshPoints[i + 1] = ropePoints[1];
            meshPoints[i + 2] = ropePoints[5];
            meshPoints[i + 3] = ropePoints[4];

            meshPoints[i + 4] = ropePoints[1];
            meshPoints[i + 5] = ropePoints[2];
            meshPoints[i + 6] = ropePoints[6];
            meshPoints[i + 7] = ropePoints[5];

            meshPoints[i + 8] = ropePoints[2];
            meshPoints[i + 9] = ropePoints[3];
            meshPoints[i + 10] = ropePoints[7];
            meshPoints[i + 11] = ropePoints[6];

            meshPoints[i + 12] = ropePoints[3];
            meshPoints[i + 13] = ropePoints[0];
            meshPoints[i + 14] = ropePoints[4];
            meshPoints[i + 15] = ropePoints[7];

            i += 16;
        }

        m.SetVertices(meshPoints);
    }
}
