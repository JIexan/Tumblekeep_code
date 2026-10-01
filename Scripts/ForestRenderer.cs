using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

//[ExecuteInEditMode]
public class ForestRenderer : MonoBehaviour
{
	[System.Serializable]
	public class TreeList {
		public int mId;
		public List<Matrix4x4> trees = new List<Matrix4x4>();

		public void Add(Matrix4x4 m) {
			trees.Add(m);
		}
	}

	public Material mat;
	public int tmp;
	public bool packed;
	public List<TreeList> trees;
	public Mesh[] meshes;

	public void SetTreesCount(int count) {
		trees = new List<TreeList>(count);
		for (int i = 0; i < count; i++) {
			trees.Add(new TreeList());
			trees[i].mId = i;
		}
	}

	public void AddTree(int treeIndex, Vector3 position, Quaternion rotation, Vector3 scale) {
		if(treeIndex < trees.Count) {
			Matrix4x4 m = Matrix4x4.identity;
			m.SetTRS(position, rotation, scale);
			trees[treeIndex].Add(m);
		}
	}

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
	if(EditorApplication.isPlaying)
#endif
	{
			foreach(var list in trees) {
				foreach(var tree in list.trees)
					Graphics.DrawMesh(meshes[list.mId], tree, mat, 0);
			}

	}
	}
}
