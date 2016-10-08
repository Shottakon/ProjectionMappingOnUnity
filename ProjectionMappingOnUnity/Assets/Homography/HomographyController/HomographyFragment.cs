//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィ変換のフラグメントコンポーネント
//===========================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Homography
{
	[RequireComponent (typeof(MeshRenderer), typeof(MeshFilter))]
	public class HomographyFragment : MonoBehaviour
	{
		//新規フラグメントの生成
		public static GameObject CreateFragment(string name, Shader shader, RenderTexture textureBuffer)
		{
			Material material = new Material (shader);
			material.mainTexture = textureBuffer;

			GameObject fragment = new GameObject ("fragment");
			fragment.AddComponent<MeshFilter> ();
			fragment.AddComponent<MeshRenderer> ().material = material;
			fragment.AddComponent<HomographyFragment> ();
			return fragment;
		}

		private Matrix4x4 mHomoMat;

		List<FragmentVertex> mFragmentVertices = new List<FragmentVertex> ();
		List<FragmentAnchor> mFragmentAnchores = new List<FragmentAnchor> ();
		//フラグメントの頂点変換情報
		public List<FragmentVertex> FragmentVertices{
			get{ return  mFragmentVertices; }
			set{ mFragmentVertices = value; }
		}
		//フラグメントのアンカー変換情報
		public List<FragmentAnchor> FragmentAnchores{
			get{ return mFragmentAnchores; }
			set{ mFragmentAnchores = value; }
		}

		void Start()
		{
			GetComponent<MeshFilter> ().mesh = new Mesh ();
		}

		void Update()
		{
			GetComponent<MeshRenderer> ().material.SetMatrix ("_HomographyMatrix", mHomoMat);
		}

		//ホモグラフィの情報を更新
		public void RefreshFragment()
		{
			RefreshPolygon ();
			RefreshHomographyMatrix ();
		}
		private void RefreshPolygon()
		{
			//頂点
			Vector3[] vertices;
			{
				List<Vector3> vertexList = new List<Vector3> ();
				foreach (FragmentVertex fv in FragmentVertices) {
					Vector2 pos = fv.Vertex;
					vertexList.Add (new Vector3 (pos.x, pos.y, 0.0f));
				}
				vertices = vertexList.ToArray ();
			}
			//ポリゴンの頂点インデックス
			int[] triangles = PolygonCreater.CreateIndexTriangles(vertices);

			//UV座標
			Vector2[] uv;
			{
				List<Vector2> uvList = new List<Vector2> ();
				foreach (FragmentVertex fv in FragmentVertices) {
					Vector2 uvPos = (fv.Vertex + new Vector2 (1, 1)) / 2;
					uvList.Add (uvPos);
				}
				uv = uvList.ToArray ();
			}

			Mesh m = GetComponent<MeshFilter> ().mesh;
			m.Clear ();
			m.vertices = vertices;
			m.triangles = triangles;
			m.uv = uv;
		}
		private void RefreshHomographyMatrix()
		{
			//ホモグラフィ変換用の行列計算機
			HomographyMatrix homoMat = new HomographyMatrix ();
			{
				foreach (FragmentVertex fv in FragmentVertices) {
					Vector2 uvBefore = (fv.UV + new Vector2 (1, 1)) / 2;
					Vector2 uvAfter = (fv.Vertex + new Vector2 (1, 1)) / 2;

					homoMat.AddPosition (uvAfter, uvBefore);
				}
				foreach (FragmentAnchor fa in FragmentAnchores) {
					Vector2 uvBefore, uvAfter;
					uvBefore = (fa.UV + new Vector2 (1, 1)) / 2;
					uvAfter  = (fa.Vertex + new Vector2 (1, 1)) / 2;
					homoMat.AddPosition (uvAfter, uvBefore);
				}
			}
			Matrix4x4 mat;
			if (homoMat.CreateHomographyMatrix (out mat) != 0)
			{
				Debug.Log ("ホモグラフィ変換に失敗しました。");
				return;
			}
			//Debug.Log ("ホモグラフィ行列:\n" + mat);
			mHomoMat = mat;
		}
	}

	public abstract class FragmentPoint
	{
		public virtual Vector2 UV{ get; set; }
		public virtual Vector2 Vertex { get; set; }

		public abstract FragmentPoint Copy();
	}
	//頂点の変換情報
	public class FragmentVertex : FragmentPoint
	{
		public override FragmentPoint Copy ()
		{
			return new FragmentVertex (UV, Vertex);
		}

		public FragmentVertex(Vector2 vertex)
		{
			UV = vertex;
			Vertex  = vertex;
		}
		public FragmentVertex(Vector2 uv, Vector2 vertex)
		{
			UV = uv;
			Vertex  = vertex;
		}
	}
	//アンカーの変換情報
	public class FragmentAnchor : FragmentPoint
	{
		public override FragmentPoint Copy ()
		{
			return new FragmentVertex (UV, Vertex);
		}

		public FragmentAnchor(Vector2 vertex)
		{
			UV = vertex;
			Vertex  = vertex;
		}
		public FragmentAnchor(Vector2 uv, Vector2 vertex)
		{
			UV = uv;
			Vertex  = vertex;
		}
	}

	//ポリゴンの作成用
	public static class PolygonCreater
	{
		//正n角形のポリゴンを生成
		//	n	:角の数
		public static Vector2[] CreateRegularPolygonVertices(int n)
		{
			return CreateRegularPolygonVertices(n, (2.0f * Mathf.PI) * 0.5f / n);
		}
		//	n			:角の数
		//	radOffset	:角度のオフセット
		public static Vector2[] CreateRegularPolygonVertices(int n, float radOffset)
		{
			Vector2[] vertices = new Vector2[n];
			float max = 0.0f;
			for (int i = 0; i < n; i++) {
				float rad = (2.0f * Mathf.PI) * i / n + radOffset;
				Vector2 vertex = new Vector3 (Mathf.Sin (rad), -Mathf.Cos (rad), 0.0f);
				max = Mathf.Max (max, Mathf.Max(vertex.x, vertex.y));

				vertices [i] = vertex;
			}
			for (int i = 0; i < n; i++)
				vertices [i] /= max;
			return vertices;
		}

		//面に貼る三角形のインデックス配列を作成(同一平面上に存在していることが前提)
		//	vertices	:頂点の配列
		public static int[] CreateIndexTriangles(Vector3[] vertices)
		{
			//インデックスを初期化
			List<int> sortedIndex = new List<int> ();
			for (int i = 0; i < vertices.Length; i++)
				sortedIndex.Add (i);

			//頂点数が規定に達したら終了
			List<int> triangles = new List<int> ();
			while (triangles.Count < (vertices.Length - 2) * 3)
			{
				int index0 = 0, index1 = 1, index2 = 2;

				int count = sortedIndex.Count;
				for (int i = 0; i < count; i++)
				{
					//2辺に接する三角形を選択
					index0 = (i - 1 + count) % count;
					index1 = (i     + count) % count;
					index2 = (i + 1 + count) % count;

					Vector3 o = vertices [sortedIndex [index1]];
					Vector3 oa = vertices [sortedIndex [index0]] - o;
					Vector3 ob = vertices [sortedIndex [index2]] - o;
					Vector3 oc = (oa + ob)/2;

					{
						//この三角形が面内部に内包されているものか調べる
						//	重心と注目した頂点でできる直線上の、交点の数によって判定
						//	op + s(oq-op) = toc + uN	(0f<=s<1f, Nは法線)
						//	op = s(op-oq) + toc + uN
						//	で調べられる
						int ocount = 0, ccount = 0;
						for (int j = 0; j < count; j++) {
							int j0, j1 , j2;
							j0 = (j + count - 1) % count;
							j1 = (j + count    ) % count;
							j2 = (j + count + 1) % count;

							Vector3 op = vertices [sortedIndex [j1]] - o;
							Vector3 oq = vertices [sortedIndex [j2]] - o;
							Vector3 qp = op - oq;
							Vector3 n = Vector3.Cross (oc, qp);

							MatrixNxM m = new MatrixNxM (3, 3);
							m.Mat [0, 0] = qp.x; m.Mat [0, 1] = oc.x; m.Mat [0, 2] = n.x;
							m.Mat [1, 0] = qp.y; m.Mat [1, 1] = oc.y; m.Mat [1, 2] = n.y;
							m.Mat [2, 0] = qp.z; m.Mat [2, 1] = oc.z; m.Mat [2, 2] = n.z;
							MatrixNxM v = new MatrixNxM (3, 1);
							v.Mat [0, 0] = op.x;
							v.Mat [1, 0] = op.y;
							v.Mat [2, 0] = op.z;
							MatrixNxM m_inv = m.InverseMatrix ();
							if (m_inv == null) continue;

							MatrixNxM x = m_inv * v;

							int addCount = 0;
							float s = (float)x.Mat [0, 0], t = (float)x.Mat [1, 0];

							if (s == 0f) {
								Vector3 d, e;
								d = vertices [sortedIndex [j0]] - vertices [sortedIndex [j1]];
								e = vertices [sortedIndex [j2]] - vertices [sortedIndex [j1]];
								if (Vector3.Dot (Vector3.Cross (oc, d), Vector3.Cross (oc, e)) < 0f)
									addCount = 1;
								else
									addCount = 2;
							} else if (0f < s && s < 1f) {
								addCount = 1;
							}

							if (t <= 0f)
								ocount += addCount;
							else
								ccount += addCount;
						}

						if ((ocount % 2) * (ccount % 2) == 0)
							continue;
					}

					{
						//三角形内部に他の頂点が存在しないか判定
						// soa + tob + uN = op を解く (Nは法線)

						//三角形の法線ベクトルを作成
						Vector3 n = Vector3.Cross (oa, ob);

						MatrixNxM m = new MatrixNxM (3, 3);
						m.Mat [0, 0] = oa.x; m.Mat [0, 1] = ob.x; m.Mat [0, 2] = n.x;
						m.Mat [1, 0] = oa.y; m.Mat [1, 1] = ob.y; m.Mat [1, 2] = n.y;
						m.Mat [2, 0] = oa.z; m.Mat [2, 1] = ob.z; m.Mat [2, 2] = n.z;
						MatrixNxM m_inv = m.InverseMatrix ();
						if (m_inv == null) continue;

						bool isValidTriangle = true;
						for (int j = 0; j < count; j++) {
							Vector3 op = vertices [sortedIndex [j]] - o;
							MatrixNxM v = new MatrixNxM (3, 1);
							v.Mat [0, 0] = op.x;
							v.Mat [1, 0] = op.y;
							v.Mat [2, 0] = op.z;

							MatrixNxM x = m_inv * v;

							float s = (float)x.Mat [0, 0], t = (float)x.Mat [1, 0];
							if (0f < s && s < 1f)
							if (0f < t && t < 1f)
							if (0f < s + t && s + t < 1f) {
								isValidTriangle = false;
								break;
							}
						}
						if (!isValidTriangle)
							continue;
					}
					break;
				}

				triangles.Add (sortedIndex [index0]);
				triangles.Add (sortedIndex [index1]);
				triangles.Add (sortedIndex [index2]);
				sortedIndex.RemoveAt (index1);
			}
			return triangles.ToArray ();
		}
	}
}
