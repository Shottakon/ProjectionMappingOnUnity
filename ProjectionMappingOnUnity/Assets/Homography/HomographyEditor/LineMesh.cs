//===========================================================
//  Author  :しょった
//  Summary :任意幅のラインを作るためのメッシュ
//===========================================================

using System;
using System.Collections.Generic;
using UnityEngine;

//参考にしたとこ
// http://seiroise.hatenablog.com/entry/2016/05/29/233623

[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshRenderer))]
public class LineMeshController : MonoBehaviour
{
	public static GameObject CreateLine(string name, Shader shader)
	{
		GameObject line = new GameObject (name);
		line.AddComponent<MeshFilter> ();
		line.AddComponent<MeshRenderer> ().material = new Material(shader);
		line.AddComponent<LineMeshController> ();
		return line;
	}

	//メッシュデータ
	public class LineMesh
	{
		//頂点データ
		public Vector3[] vertices;
		//線分の色
		public Color[] colorOfEdge;
		//頂点の色
		public Color[] colorOfVertex;
		//線分のインデックス群
		public int[] indices;
		//線分メッシュの法線
		public Vector3 normalVector = new Vector3 (0.0f, 0.0f, 1.0f);
		//線幅
		public float width = 0.01f;
		//頂点サイズ
		public float vertexSize = 0.03f;
	}

	//メッシュ
	public LineMesh mesh{
		get{ return (meshs != null?meshs[0]:null); }
		set{
			LineMesh[] lm = new LineMesh[1];
			lm [0] = value;
			meshs = lm;
		}
	}
	public LineMesh[] meshs{ get; set; }

	//デフォルト色
	public Color DefaultColor = new Color (1f, 1f, 1f);

	void Update()
	{
		LineMeshData lmd = CreateLines ();
		Mesh m = GetComponent<MeshFilter> ().mesh;
		m.Clear ();
		m.vertices = lmd.vertices;
		m.colors = lmd.colors;
		m.SetIndices (lmd.indices, MeshTopology.Triangles, 0);
	}


	//実際のラインのメッシュデータを作成
	private struct LineMeshData
	{
		public Vector3[] vertices;
		public Color[] colors;
		public int[] indices;
		public static LineMeshData Combine(List<LineMeshData> meshData)
		{
			List<Vector3> vert = new List<Vector3> ();
			List<Color> col = new List<Color> ();
			List<int> ind = new List<int> ();

			int offset = 0;
			foreach (LineMeshData lmd in meshData)
			{
				vert.AddRange (lmd.vertices);
				col.AddRange (lmd.colors);
				for (int i = 0; i < lmd.indices.Length; i++)
					ind.Add (lmd.indices [i] + offset);
				offset += lmd.vertices.Length;
			}
			LineMeshData meshData_new = new LineMeshData ();
			meshData_new.vertices = vert.ToArray ();
			meshData_new.colors = col.ToArray ();
			meshData_new.indices = ind.ToArray ();
			return meshData_new;
		}
	}

	//複数のラインを作成
	private LineMeshData CreateLines()
	{
		if (meshs == null)
			return new LineMeshData ();
		List<LineMeshData> meshList = new List<LineMeshData> ();
		foreach (LineMesh lm in meshs)
			meshList.Add (CreateLine (lm));
		
		return LineMeshData.Combine (meshList);
	}
	//ラインを作成
	private LineMeshData CreateLine(LineMesh lm)
	{
		Vector3[] vert = lm.vertices;
		Color[] colorE = lm.colorOfEdge;
		Color[] colorV = lm.colorOfVertex;
		int[] ind	   = lm.indices;

		//ちゃんと正しくデータが入っているかチェック
		if (vert == null) return new LineMeshData ();
		int count = vert.Length;
		if (count == 0) return new LineMeshData ();

		bool isCorrectContents = true;
		if (colorE != null) {
			isCorrectContents &= vert.Length == colorE.Length;
		} else {
			colorE = new Color[count];
			for (int i = 0; i < count; i++)
				colorE [i] = DefaultColor;
		}
		if (colorV != null) {
			isCorrectContents &= vert.Length == colorV.Length;
		} else {
			colorV = new Color[count];
			for (int i = 0; i < count; i++)
				colorV [i] = DefaultColor;
		}

		if (!isCorrectContents) {
			Debug.Assert (false, "ラインデータが異常です");
			return new LineMeshData ();
		}

		//メッシュを作成する
		//	エッジメッシュを作成
		List<LineMeshData> meshList = new List<LineMeshData> ();
		for (int i = 0; i < ind.Length; i += 2)
		{
			int i0 = ind [i];
			int i1 = ind [i + 1];
			meshList.Add (CreateEdge (vert [i0], vert [i1], colorE[i0], colorE[i1], lm.width, lm.normalVector));
		}
		//	バーテックスメッシュを作成
		for (int i = 0; i < vert.Length; i++)
			meshList.Add (CreateVertex (vert[i], colorV[i], lm.vertexSize, lm.normalVector));
		//	メッシュを統合
		return LineMeshData.Combine (meshList);
	}

	//ラインの頂点を作成
	private LineMeshData CreateVertex(Vector3 pos, Color color, float size, Vector3 normalVec)
	{
		Vector3 dir = Vector3.right;
		Vector3 dirV = dir.normalized * size/2.0f;
		Vector3 dirH = Vector3.Cross (dir, normalVec).normalized * size/2.0f;

		Vector3 scale = transform.lossyScale;
		Vector3 scale_inv = new Vector3(1.0f/scale.x, 1.0f/scale.y, 1.0f/scale.z); 
		dirV.Scale (scale_inv);
		dirH.Scale (scale_inv);

		Vector3[] vertices = new Vector3[4];
		vertices [0] = pos - dirV - dirH;
		vertices [1] = pos - dirV + dirH;
		vertices [2] = pos + dirV + dirH;
		vertices [3] = pos + dirV - dirH;

		Color[] colors = new Color[4];
		colors [0] = color;
		colors [1] = color;
		colors [2] = color;
		colors [3] = color;

		int[] indices = new int[6];
		indices [0] = 0; indices [1] = 1; indices [2] = 2;
		indices [3] = 2; indices [4] = 3; indices [5] = 0;

		LineMeshData lmd = new LineMeshData ();
		lmd.vertices = vertices;
		lmd.colors = colors;
		lmd.indices = indices;
		return lmd;
	}

	//ラインのエッジを作成
	private LineMeshData CreateEdge(Vector3 from, Vector3 to, Color fromColor, Color toColor, float width, Vector3 normalVec)
	{
		Vector3 dir = (to - from).normalized;
		Vector3 dirV = Vector3.Cross (dir, normalVec).normalized * width/2.0f;

		Vector3 scale = transform.lossyScale;
		Vector3 scale_inv = new Vector3(1.0f/scale.x, 1.0f/scale.y, 1.0f/scale.z); 
		dirV.Scale (scale_inv);

		Vector3[] vertices = new Vector3[4];
		vertices [0] = from - dirV;
		vertices [1] = from + dirV;
		vertices [2] = to + dirV;
		vertices [3] = to - dirV;

		Color[] colors = new Color[4];
		colors [0] = fromColor;
		colors [1] = fromColor;
		colors [2] = toColor;
		colors [3] = toColor;

		int[] indices = new int[6];
		indices [0] = 0; indices [1] = 1; indices [2] = 2;
		indices [3] = 2; indices [4] = 3; indices [5] = 0;

		LineMeshData lmd = new LineMeshData ();
		lmd.vertices = vertices;
		lmd.colors = colors;
		lmd.indices = indices;
		return lmd;
	}
}