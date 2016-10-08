//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィ変換のエディタのボタン
//===========================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Manipulation;

namespace Homography
{
	public class HomographyButtonController : MonoBehaviour
	{
		[SerializeField]
		private HomographyEditor mEditor;

		[SerializeField]
		private GameObject mButtonLayer;
		public bool IsVisibleButtonLayer{
			get{ return mButtonLayer.activeSelf; }
			set{ mButtonLayer.SetActive (value); }
		}

		void Update()
		{
			if (Input.GetMouseButtonDown (1))
			{
				IsVisibleButtonLayer ^= true;
			}
			mEditor.GetComponent<ManipulationController> ().IsActive = !IsVisibleButtonLayer;
		}

		public void InvisibleMode()
		{
			mEditor.InvisibleEditor (true);
		}
		public void VertexMode()
		{
			mEditor.InvisibleEditor (false);
			mEditor.ManipulationMode = HomographyEditor.ManipulationKind.Vertex | mEditor.EditMode;
		}
		public void UVMode()
		{
			mEditor.InvisibleEditor (false);
			mEditor.ManipulationMode = HomographyEditor.ManipulationKind.UV | mEditor.EditMode;
		}
		public void CreateMode()
		{
			mEditor.ManipulationMode = HomographyEditor.ManipulationKind.Create | mEditor.EditTarget;
		}
		public void AddMode()
		{
			mEditor.ManipulationMode = HomographyEditor.ManipulationKind.Add | mEditor.EditTarget;
		}
		public void RemoveMode()
		{
			mEditor.ManipulationMode = HomographyEditor.ManipulationKind.Remove | mEditor.EditTarget;
		}
		public void SelectMode()
		{
			mEditor.ManipulationMode = HomographyEditor.ManipulationKind.Select | mEditor.EditTarget;
		}
		public void MoveMode()
		{
			mEditor.ManipulationMode = HomographyEditor.ManipulationKind.Move | mEditor.EditTarget;
		}

		public void SaveData()
		{
			string path = EditorUtility.SaveFilePanel ("", "", "fragment", "homo");

			Debug.Log (path);
			if (!string.IsNullOrEmpty (path)) {
				string data = mEditor.GetFragmentsData ();
				FileInfo fi = new FileInfo (path);

				StreamWriter sw = fi.CreateText ();
				sw.Flush ();
				sw.Write (data);
				sw.Close ();
			}
		}

		public void LoadData()
		{
			string path = EditorUtility.OpenFilePanel ("", "", "homo");

			Debug.Log (path);
			if (!string.IsNullOrEmpty (path)) {
				FileInfo fi = new FileInfo (path);
				StreamReader sr = new StreamReader (fi.OpenRead() ,Encoding.GetEncoding("UTF-8"));
				string data = sr.ReadToEnd ();
				mEditor.SetFragmentsData (data);
				sr.Close ();
			}
		}
	}
}