//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィのエディタ
//===========================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Homography.IO;
using Manipulation;

namespace Homography
{
	[RequireComponent (typeof(ManipulationController))]
	public class HomographyEditor : MonoBehaviour
	{
		//ホモグラフィコントローラー
		[SerializeField]
		private HomographyController mHomographyController;

		//フラグメント生成用の素材
		[SerializeField]
		private Shader mHomographyShader;
		[SerializeField]
		private RenderTexture mTextureBuffer;
		//頂点表示の線分用のシェーダー
		[SerializeField]
		private Shader mLineShader;
		//操作描画用のシェーダー
		[SerializeField]
		private Shader mManipulationDrawShader;

		//エディターのカラー設定(Vertex)
		[SerializeField]
		private Color mNormalVertexColor   = new Color(0.0f, 1.0f, 1.0f);
		[SerializeField]
		private Color mSelectedVertexColor = new Color(1.0f, 1.0f, 0.0f);
		//エディターのカラー設定(UV)
		[SerializeField]
		private Color mNormalUVColor   = new Color(0.0f, 1.0f, 1.0f);
		[SerializeField]
		private Color mSelectedUVColor = new Color(1.0f, 1.0f, 0.0f);

		//拡大縮小率
		[SerializeField]
		private float mScaleBias = 0.1f;

		//操作種類
		public enum ManipulationKind
		{
			Create		= 1,
			Add			= 2,
			Remove		= 3,
			Select		= 4,
			Move		= 5
				,
			UV			= 0,
			Vertex		= 8
		};
		private ManipulationKind mManipulationMode = ManipulationKind.Vertex|ManipulationKind.Select;
		//UV or Vertex or Invisibleのターゲット
		public ManipulationKind EditTarget{
			get{ return (ManipulationKind)((int)mManipulationMode & 8); }
		}
		//UV or Vertexのモード
		public ManipulationKind EditMode{
			get{ return (ManipulationKind)((int)mManipulationMode & 7); }
		}
		//操作のモード
		public ManipulationKind ManipulationMode{
			get{ return mManipulationMode; }
			set{ mManipulationMode = value; }
		}

		//選択物保持スペース
		private List<FragmentPoint> mSelectedPointList	= new List<FragmentPoint> ();
		public List<FragmentPoint> SelectedPointList{
			get{ return mSelectedPointList; }
		}
		//フラグメントを取得
		private List<HomographyFragment> mFragmentList = new List<HomographyFragment> ();
		public List<HomographyFragment> FragmentList{
			get{ return mFragmentList; }
		}

		//操作を描画するための部分
		private DrawManipulation mManipulationDrawer = null;
		public DrawManipulation ManipulationDrawer{
			get{
				DrawManipulation dm = mManipulationDrawer;
				return (dm == null ? new DrawManipulation () : dm);
			}
			set{ mManipulationDrawer = value; }
		}

		//線分表示用
		private LineMeshController mLineMesh;
		//操作を描画するためのメッシュ
		private MeshFilter mDrawManipulationCanvas;
		private Material mDMMaterial;
		//UV描画用のメッシュ
		private MeshFilter mUVmesh;

		void Start()
		{
			//操作:	待機
			GetComponent<ManipulationController> ().AddManipulation (new Homography.Manipulation.WaitInputEvent());

			{//線分のレンダラを作成
				GameObject l = LineMeshController.CreateLine ("line", mLineShader);
				mLineMesh = l.GetComponent<LineMeshController> ();
				l.transform.SetParent (mHomographyController.transform);
			}
			{//操作を描画するための場所を用意
				GameObject dm = new GameObject ("manipulation");
				Mesh m = new Mesh ();
				dm.AddComponent<MeshFilter> ().mesh = m;
				dm.AddComponent<MeshRenderer> ();
				dm.transform.SetParent (mHomographyController.transform);
				mDrawManipulationCanvas = dm.GetComponent<MeshFilter> ();
				mDMMaterial = new Material (mManipulationDrawShader);
			}
			{//操作を描画するための場所を用意
				Shader uvShader = Shader.Find ("Unlit/Texture");
				GameObject uvMesh = new GameObject ("uvMesh");
				Material mat = new Material (uvShader);
				mat.mainTexture = mTextureBuffer;
				uvMesh.AddComponent<MeshFilter> ().mesh = CreatePlainMesh ();
				uvMesh.AddComponent<MeshRenderer> ().material = mat;
				uvMesh.transform.SetParent (mHomographyController.transform);
				mUVmesh = uvMesh.GetComponent<MeshFilter> ();
			}
		}

		void Update()
		{
			//操作を描画
			{
				Mesh m = ManipulationDrawer.Draw (mDrawManipulationCanvas.transform);
				if (m != null) {
					List<Material> mats = new List<Material> ();
					for (int i = 0; i < m.subMeshCount; i++)
						mats.Add (mDMMaterial);
					mDrawManipulationCanvas.GetComponent<MeshRenderer> ().materials = mats.ToArray ();
				}
				mDrawManipulationCanvas.mesh = m;
			}

			//フラグメントを更新
			List<HomographyFragment> shouldDeleteFragment = new List<HomographyFragment> ();
			foreach (HomographyFragment fragment in FragmentList)
			{
				if (fragment.FragmentVertices.Count == 0 && fragment.FragmentAnchores.Count == 0)
					shouldDeleteFragment.Add (fragment);
				fragment.RefreshFragment ();
			}
			foreach (HomographyFragment fragment in shouldDeleteFragment)
			{
				FragmentList.Remove (fragment);
				Destroy (fragment.gameObject);
			}

			ManipulationKind editMode = this.EditTarget;
			if (editMode == ManipulationKind.UV)
				RefreshUVEditor ();
			if (editMode == ManipulationKind.Vertex)
				RefreshVertexEditor ();
			
			mLineMesh.transform.SetAsFirstSibling ();
			mDrawManipulationCanvas.transform.SetAsFirstSibling ();
			mUVmesh.transform.SetAsLastSibling ();

			//
			mScaleDif_tmp *= 0.75f;
		}

		//ホモグラフィコントローラーの拡大縮小
		private float mScaleDif_tmp = 0.0f;
		public float ScaleHomography{
			get{
				return mHomographyController.Scale + mScaleDif_tmp;
			}
			set{
				float bias = mScaleBias;
				float val = value;
				float calc = Mathf.Floor (val / bias + 0.5f) * bias;
				mHomographyController.Scale = calc;
				mScaleDif_tmp = val - calc;
			}
		}

		//エディターを非表示にする
		public void InvisibleEditor(bool isInvisible)
		{
			this.gameObject.SetActive (!isInvisible);
			mLineMesh.gameObject.SetActive (!isInvisible);
			mDrawManipulationCanvas.gameObject.SetActive (!isInvisible);
			mUVmesh.gameObject.SetActive (false);

			foreach (HomographyFragment fragment in FragmentList)
				fragment.gameObject.SetActive (true);

			System.GC.Collect ();
		}

		//フラグメントを作成
		public GameObject CreateFragment()
		{
			GameObject f = HomographyFragment.CreateFragment ("fragment", mHomographyShader, mTextureBuffer);
			f.transform.SetParent (mHomographyController.transform);
			FragmentList.Add (f.GetComponent<HomographyFragment> ());
			return f;
		}

		public void DestroyAllFragment()
		{
			foreach (HomographyFragment frag in FragmentList)
			{
				frag.FragmentVertices.RemoveRange (0, frag.FragmentVertices.Count);
				frag.FragmentAnchores.RemoveRange (0, frag.FragmentAnchores.Count);
			}
		}

		//バーテックスモードをセット
		public void RefreshVertexEditor()
		{
			mUVmesh.gameObject.SetActive (false);
			mLineMesh.gameObject.SetActive (true);

			foreach (HomographyFragment fragment in FragmentList)
				fragment.gameObject.SetActive (true);
			
			RefreshLineMesh (FragmentList, "Vertex", mSelectedVertexColor, mNormalVertexColor);
		}

		//UVの線を描画
		public void RefreshUVEditor()
		{
			mUVmesh.gameObject.SetActive (true);
			mLineMesh.gameObject.SetActive (true);

			foreach (HomographyFragment fragment in FragmentList)
				fragment.gameObject.SetActive (false);

			RefreshLineMesh (FragmentList, "UV", mSelectedUVColor, mNormalUVColor);
		}
		//ヘルパー関数
		private void RefreshLineMesh (List<HomographyFragment> fragmentList, string mode, Color select, Color unselect)
		{
			List<LineMeshController.LineMesh> meshList = new List<LineMeshController.LineMesh> ();
			foreach (HomographyFragment hf in fragmentList)
			{
				List<Vector3> posList 	= new List<Vector3> ();
				List<Color> colorVList 	= new List<Color> ();
				List<int> indicesList 	= new List<int> ();
				for (int i = 0; i < hf.FragmentVertices.Count; i++)
				{
					FragmentVertex fv = hf.FragmentVertices [i];
					Vector2 pos;
					switch (mode) {
					case "Vertex":
					default:
						pos = fv.Vertex;
						break;
					case "UV":
						pos = fv.UV;
						break;
					}
					posList.Add (new Vector3(pos.x, pos.y, 0f));
					bool isSelected = mSelectedPointList.Contains (fv);
					colorVList.Add (isSelected ? select : unselect);
					indicesList.Add (i);
					indicesList.Add ((i+1)%hf.FragmentVertices.Count);
				}
				LineMeshController.LineMesh lm = new LineMeshController.LineMesh ();
				lm.vertices = posList.ToArray ();
				lm.colorOfVertex = colorVList.ToArray ();
				lm.indices = indicesList.ToArray ();
				meshList.Add (lm);
			}
			mLineMesh.meshs = meshList.ToArray ();
			mLineMesh.DefaultColor = unselect;
		}

		//セーブ用のデータを作成
		public string GetFragmentsData()
		{
			string json = null;
			HomographySaveFormat hsf = new HomographySaveFormat ();
			if (hsf.CreateSaveDataFromFragmentList (this))
				json = JsonUtility.ToJson (hsf, true);
			else
				Debug.Log ("セーブデータが生成できませんでした。");

			Debug.Log (json);
			return json;
		}
		//データを解析してフラグメントを作成
		public void SetFragmentsData(string json)
		{
			DestroyAllFragment ();
			HomographySaveFormat hsf = JsonUtility.FromJson<HomographySaveFormat> (json);
			if (hsf.CreateFragmentListFromSaveData (this))
				return;
			else
				Debug.Log ("データが破損しています。");
		}

		//スクリーン座標系との相互変換
		public Vector2 ConvertUvToPos (Vector2 uv)
		{
			Vector2 standardPos = (uv * mHomographyController.Scale + new Vector2 (1.0f, 1.0f)) / 2.0f;
			return Vector2.Scale(standardPos, new Vector2 (Screen.width, Screen.height));
		}
		public Vector2 ConvertPosToUv (Vector2 pos)
		{
			Vector2 standardPos = Vector2.Scale(pos, new Vector2 (1.0f/Screen.width, 1.0f/Screen.height));
			return (standardPos * 2.0f - new Vector2 (1.0f, 1.0f)) / mHomographyController.Scale;
		}

		//平面メッシュ
		private Mesh CreatePlainMesh()
		{
			Vector3[] vert = new Vector3[4];
			Vector2[] uvs = new Vector2[4];
			vert [0] = new Vector3 ( 1,  1, 0); uvs [0] = new Vector2 (1, 1);
			vert [1] = new Vector3 ( 1, -1, 0); uvs [1] = new Vector2 (1, 0);
			vert [2] = new Vector3 (-1, -1, 0); uvs [2] = new Vector2 (0, 0);
			vert [3] = new Vector3 (-1,  1, 0); uvs [3] = new Vector2 (0, 1);

			int[] ind = new int[6];
			ind [0] = 0; ind [1] = 1; ind [2] = 2;
			ind [3] = 2; ind [4] = 3; ind [5] = 0;
			Mesh m = new Mesh ();
			m.vertices = vert;
			m.uv = uvs;
			m.triangles = ind;
			return m;
		}
	}

	//操作を描画
	public class DrawManipulation
	{
		public virtual Mesh Draw(Transform tf){
			return null;
		}
	}

	static class InputHelper
	{
		public enum SubKey
		{
			Shift	= 1,
			Command = 2,
			Control = 4,
			Alt		= 8
		}
		public static int GetSubKey()
		{
			bool shift	 = Input.GetKey (KeyCode.LeftShift)   || Input.GetKey (KeyCode.RightShift);
			bool command = Input.GetKey (KeyCode.LeftCommand) || Input.GetKey (KeyCode.RightCommand);
			bool control = Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl);
			bool alt	 = Input.GetKey (KeyCode.LeftAlt) 	  || Input.GetKey (KeyCode.RightAlt);

			return  (shift?1:0) | ((command?1:0) << 1) | ((control?1:0) << 2) | ((alt?1:0) << 3);
		}
	}
}
