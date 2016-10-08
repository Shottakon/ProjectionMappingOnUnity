//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィエディタの操作ステート
//===========================================================

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Manipulation;

namespace Homography.Manipulation
{
	public class WaitInputEvent : ManipulationFormat
	{
		private HomographyEditor mHomographyEditor;
		public override void Awake ()
		{
			mHomographyEditor = Controller.GetComponent<HomographyEditor> ();
		}
		public override void Update ()
		{
			Stack<ManipulationFormat> manipulation = new Stack<ManipulationFormat> ();
			if (Input.GetMouseButtonDown (0)) {
				switch (mHomographyEditor.EditMode)
				{
				case HomographyEditor.ManipulationKind.Create:
					manipulation.Push (new CreateVertex ());
					break;
				case HomographyEditor.ManipulationKind.Add:
					manipulation.Push (new AddVertex ());
					break;
				case HomographyEditor.ManipulationKind.Remove:
					manipulation.Push (new SelectVertex ());
					manipulation.Push (new RemoveVertex ());
					break;
				case HomographyEditor.ManipulationKind.Select:
					manipulation.Push (new SelectVertex ());
					break;
				case HomographyEditor.ManipulationKind.Move:
					manipulation.Push (new MoveVertex ());
					break;
				default:
					break;
				}
			}

			while (manipulation.Count > 0) {
				Controller.AddManipulation (manipulation.Pop ());
			}

			float axis = Input.GetAxis ("Mouse ScrollWheel");
			mHomographyEditor.ScaleHomography += axis;

		}
	}

	public class CreateVertex : ManipulationFormat
	{
		private HomographyEditor mHomographyEditor;

		public override void Awake ()
		{
			mHomographyEditor = Controller.GetComponent<HomographyEditor> ();
		}
		public override void Update ()
		{
			HomographyFragment hf = mHomographyEditor.CreateFragment ().GetComponent<HomographyFragment> ();
			Vector3 mousePos = Input.mousePosition;
			Vector2 stdPos = mHomographyEditor.ConvertPosToUv (new Vector2(mousePos.x, mousePos.y));
			FragmentVertex fv = new FragmentVertex (stdPos);
			hf.FragmentVertices.Add (fv);

			List<FragmentPoint> selectedList = mHomographyEditor.SelectedPointList;
			selectedList.RemoveRange (0, selectedList.Count);
			selectedList.Add (fv);

			mHomographyEditor.ManipulationMode = mHomographyEditor.EditTarget | HomographyEditor.ManipulationKind.Add;
			Controller.ReplaceManipulation (new MoveVertex ());
		}
	}

	public class AddVertex : ManipulationFormat
	{
		private HomographyEditor mHomographyEditor;

		public override void Awake ()
		{
			mHomographyEditor = Controller.GetComponent<HomographyEditor> ();
		}
		public override void Update ()
		{
			bool isFinishedAdd = false;

			HomographyFragment hf;
			int index;
			List<FragmentPoint> selectedList = mHomographyEditor.SelectedPointList;

			if (selectedList.Count == 1)
			if (selectedList [0] is FragmentVertex)
			if (FindFragmentVertex (selectedList [0] as FragmentVertex, out hf, out index)) {
				Vector3 mousePos = Input.mousePosition;
				Vector2 stdPos = mHomographyEditor.ConvertPosToUv (new Vector2 (mousePos.x, mousePos.y));
				FragmentVertex fv = new FragmentVertex (stdPos);
				hf.FragmentVertices.Insert (index + 1, fv);

				selectedList.RemoveRange (0, selectedList.Count);
				selectedList.Add (fv);

				isFinishedAdd = true;
			}

			if (isFinishedAdd)
				Controller.ReplaceManipulation (new MoveVertex ());
			else
				Controller.RemoveManipulation ();
		}

		//フラグメントの頂点を用意
		private bool FindFragmentVertex(FragmentVertex vertex, out HomographyFragment fragment, out int index)
		{
			fragment = null;
			index = -1;
			foreach (HomographyFragment hf in mHomographyEditor.FragmentList) {
				for (int i = 0; i < hf.FragmentVertices.Count; i++) {
					if (hf.FragmentVertices [i] == vertex)
					{
						fragment = hf;
						index = i;
						return true;
					}
				}
			}
			return false;
		}
	}

	public class RemoveVertex : ManipulationFormat
	{
		private HomographyEditor mHomographyEditor;

		public override void Awake ()
		{
			mHomographyEditor = Controller.GetComponent<HomographyEditor> ();
		}

		public override void Update () {
			HomographyFragment hf;
			int index;
			List<FragmentPoint> selectedList = mHomographyEditor.SelectedPointList;

			for (int i = 0; i < selectedList.Count; i++) {
				FragmentPoint fp = selectedList[i];
				if (fp is FragmentVertex)
				if (FindFragmentVertex (fp as FragmentVertex, out hf, out index)) {
					hf.FragmentVertices.RemoveAt (index);

				}
			}

			selectedList.RemoveRange (0, selectedList.Count);
			Controller.RemoveManipulation ();
		}

		//フラグメントの頂点を用意
		private bool FindFragmentVertex(FragmentVertex vertex, out HomographyFragment fragment, out int index)
		{
			fragment = null;
			index = -1;
			foreach (HomographyFragment hf in mHomographyEditor.FragmentList) {
				for (int i = 0; i < hf.FragmentVertices.Count; i++) {
					if (hf.FragmentVertices [i] == vertex)
					{
						fragment = hf;
						index = i;
						return true;
					}
				}
			}
			return false;
		}
	}

	public class MoveVertex : ManipulationFormat
	{
		private HomographyEditor mHomographyEditor;
		private Vector3 mPrevMousePos;
		private HomographyEditor.ManipulationKind mMode;
		private List<FragmentPoint> mPrevFP;
		private List<FragmentPoint> mCurrFP;

		public override void Awake ()
		{
			mHomographyEditor = Controller.GetComponent<HomographyEditor> ();
		}

		public override void Start ()
		{
			mPrevMousePos = Input.mousePosition;
			mMode = mHomographyEditor.EditTarget;

			mPrevFP = new List<FragmentPoint> ();
			mCurrFP = new List<FragmentPoint> (mHomographyEditor.SelectedPointList);
			for (int i = 0; i < mCurrFP.Count; i++)
				mPrevFP.Add (mCurrFP [i].Copy ());
		}
		public override void Update ()
		{
			Vector3 mousePos = Input.mousePosition;
			int subKey = InputHelper.GetSubKey ();

			Vector2 prev, next;
			prev = new Vector2 (mPrevMousePos.x, mPrevMousePos.y);
			next = new Vector2 (mousePos.x, mousePos.y);
			Vector2 stdPrev = mHomographyEditor.ConvertPosToUv (prev);
			Vector2 stdNext = mHomographyEditor.ConvertPosToUv (next);

			for (int i = 0; i < mCurrFP.Count; i++)
			{
				if (mMode == HomographyEditor.ManipulationKind.UV)
					mCurrFP [i].UV = mPrevFP [i].UV + (stdNext - stdPrev);
				else
					mCurrFP [i].Vertex = mPrevFP [i].Vertex + (stdNext - stdPrev);
			}

			if (!Input.GetMouseButton (0))
				Controller.RemoveManipulation ();
		}
	}

}