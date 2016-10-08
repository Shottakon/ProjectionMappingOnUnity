//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィエディタの操作ステート(選択)
//===========================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Manipulation;

namespace Homography.Manipulation
{
	public class SelectVertex : ManipulationFormat
	{
		private HomographyEditor mHomographyEditor;
		private Vector3 mPrevMousePos;
		private List<FragmentPoint> mSelectedList;
		private HomographyEditor.ManipulationKind mMode;
		private DrawSelectVertex mDraw;

		public override void Start ()
		{
			mHomographyEditor = Controller.GetComponent<HomographyEditor> ();
			mPrevMousePos = Input.mousePosition;
			mSelectedList = new List<FragmentPoint> (mHomographyEditor.SelectedPointList);
			mMode = mHomographyEditor.EditTarget;

			mDraw = new DrawSelectVertex ();
			mHomographyEditor.ManipulationDrawer = mDraw;
		}

		public override void Update ()
		{
			Vector3 mousePos = Input.mousePosition;
			int subKey = InputHelper.GetSubKey ();

			Vector2 prev, next;
			prev = new Vector2 (mPrevMousePos.x, mPrevMousePos.y);
			next = new Vector2 (mousePos.x, mousePos.y);
			Rect rect = new Rect (prev, next - prev);
			{
				Vector2 stdPrev = mHomographyEditor.ConvertPosToUv (prev);
				Vector2 stdNext = mHomographyEditor.ConvertPosToUv (next);
				Rect stdRect = new Rect (stdPrev, stdNext - stdPrev);
				mDraw.MouseRect = stdRect;
			}
			List<FragmentPoint> selectingList = Select (rect);

			string oprName = "A";
			if (subKey == (int)InputHelper.SubKey.Shift)
				oprName = "A+B";
			if (subKey == (int)InputHelper.SubKey.Command)
				oprName = "A^B";

			List<FragmentPoint> selectedList = mHomographyEditor.SelectedPointList;
			selectedList.RemoveRange (0, selectedList.Count);
			selectedList.AddRange(SelectHelper<FragmentPoint>.IntegrateAAndB (oprName, selectingList, mSelectedList));

			if (!Input.GetMouseButton (0))
				Controller.RemoveManipulation ();
		}
		public override void Finish ()
		{
			mHomographyEditor.ManipulationDrawer = null;
		}

		//Rect付近もしくはRect内部に存在するFragmentPointを選択
		private List<FragmentPoint> Select(Rect rect)
		{
			List<FragmentPoint> selectingList = new List<FragmentPoint> ();
			{
				float validLength = 20.0f;
				List<HomographyFragment> fragmentList = mHomographyEditor.FragmentList;
				foreach (HomographyFragment hf in fragmentList)
				{
					List<FragmentPoint> pointList = new List<FragmentPoint> ();
					foreach (FragmentVertex vertex in hf.FragmentVertices)
						pointList.Add (vertex);
					foreach (FragmentAnchor anchor in hf.FragmentAnchores)
						pointList.Add (anchor);

					for (int j = 0; j < pointList.Count; j++)
					{
						Vector2 posInFragment;
						if (mMode == HomographyEditor.ManipulationKind.UV)
							posInFragment = pointList [j].UV;
						else
							posInFragment = pointList [j].Vertex;

						Vector2 pos = mHomographyEditor.ConvertUvToPos (posInFragment);
						if (rect.size.magnitude < validLength / Mathf.Sqrt(2.0f)) {
							if ((pos - rect.position).magnitude <= validLength) {
								selectingList.RemoveRange (0, selectingList.Count);
								selectingList.Add (pointList [j]);
								validLength = (pos - rect.position).magnitude;
							}
						} else {
							if ((rect.x - pos.x) * (rect.xMax - pos.x)<0.0f && (rect.y - pos.y) * (rect.yMax - pos.y)<0.0f)
								selectingList.Add (pointList [j]);
						}
					}
				}
			}
			return selectingList;
		}
	}

	public static class SelectHelper<T>
	{
		public static List<T> IntegrateAAndB (string oprName, List<T> a, List<T> b)
		{
			List<T> r = new List<T> (a);
			switch (oprName)
			{
			case "A":
				break;
			case "A+B":
				r = new List<T> (a);
				foreach (T bobj in b) {
					bool contains = false;
					foreach (T aobj in a) {
						if (aobj.Equals (bobj)) {
							contains = true;
							break;;
						}
					}
					if (!contains)
						r.Add (bobj);
				}
				break;
			case "A-B":
				r = new List<T> (a);
				foreach (T aobj in a) {
					bool contains = false;
					foreach (T bobj in b) {
						if (aobj.Equals (bobj)) {
							contains = true;
							continue;
						}
					}
					if (contains)
						r.Remove (aobj);
				}
				break;
			case "A^B":
				List<T> a_b = IntegrateAAndB ("A-B", a, b);
				List<T> b_a = IntegrateAAndB ("A-B", b, a);
				r = IntegrateAAndB ("A+B", a_b, b_a);
				break;
			default:
				Debug.Assert (false, "Invalid OperationName:" + oprName);
				break;
			}
			return r;
		}
	}

	public class DrawSelectVertex : DrawManipulation
	{
		public Rect MouseRect{ get; set; }
		private Color c0 = new Color(0.0f, 0.0f, 1.0f, 0.1f);
		private Color c1 = new Color(0.0f, 0.0f, 1.0f, 1.0f);
		public override Mesh Draw (Transform tf)
		{
			return CreateMesh (MouseRect);
		}

		private Mesh CreateMesh(Rect r)
		{
			Vector3[] vertices = new Vector3[8];
			Color[] colors = new Color[8];
			vertices [0] = new Vector3 (r.x	   , r.y	, 0.0f);	colors [0] = c0;
			vertices [1] = new Vector3 (r.xMax , r.y	, 0.0f);	colors [1] = c0;
			vertices [2] = new Vector3 (r.xMax , r.yMax , 0.0f);	colors [2] = c0;
			vertices [3] = new Vector3 (r.x	   , r.yMax , 0.0f);	colors [3] = c0;

			vertices [4] = new Vector3 (r.x	   , r.y	, 0.0f);	colors [4] = c1;
			vertices [5] = new Vector3 (r.xMax , r.y	, 0.0f);	colors [5] = c1;
			vertices [6] = new Vector3 (r.xMax , r.yMax , 0.0f);	colors [6] = c1;
			vertices [7] = new Vector3 (r.x	   , r.yMax , 0.0f);	colors [7] = c1;

			int[] indices0 = new int[6];
			indices0 [0] = 0;
			indices0 [1] = 1;
			indices0 [2] = 2;
			indices0 [3] = 2;
			indices0 [4] = 3;
			indices0 [5] = 0;

			int[] indices1 = new int[8];
			indices1 [0] = 4;
			indices1 [1] = 5;
			indices1 [2] = 5;
			indices1 [3] = 6;
			indices1 [4] = 6;
			indices1 [5] = 7;
			indices1 [6] = 7;
			indices1 [7] = 4;

			Mesh m = new Mesh ();
			m.vertices = vertices;
			m.colors = colors;
			m.subMeshCount = 2;
			m.SetIndices (indices0, MeshTopology.Triangles, 0);
			m.SetIndices (indices1, MeshTopology.Lines, 1);
			return m;
		}
	}
}