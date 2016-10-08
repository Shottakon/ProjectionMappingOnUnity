//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィ変換をセーブするためのセーブフォーマット(JSONに変換)
//===========================================================


using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Homography;

namespace Homography.IO
{
	//マッピングデータ
	[Serializable]
	public class HomographySaveFormat
	{
		public bool CreateSaveDataFromFragmentList(HomographyEditor editor)
		{
			List<FragmentSaveFormat> fsfList = new List<FragmentSaveFormat> ();
			foreach (HomographyFragment hf in editor.FragmentList)
			{
				FragmentSaveFormat fsf = new FragmentSaveFormat ();
				if (!fsf.SetFragment (hf))
					return false;
				fsfList.Add (fsf);
			}
			FragmentsData = fsfList.ToArray ();
			ScaleHomography = editor.ScaleHomography;
			return true;
		}

		public bool CreateFragmentListFromSaveData(HomographyEditor editor)
		{
			FragmentSaveFormat[] fsfList = FragmentsData;
			foreach (FragmentSaveFormat fsf in fsfList)
			{
				HomographyFragment hf = editor.CreateFragment ().GetComponent<HomographyFragment> ();
				if (!fsf.GetFragment (ref hf))
					return false;
			}
			editor.ScaleHomography = ScaleHomography;
			return true;
		}

		[SerializeField]
		private FragmentSaveFormat[] FragmentsData;

		[SerializeField]
		private float ScaleHomography;

		//ヘルパークラス
		//フラグメントのデータ
		[Serializable]
		private class FragmentSaveFormat
		{
			public bool SetFragment(HomographyFragment hf)
			{
				List<PointSaveFormat> psfList = new List<PointSaveFormat> ();
				foreach (FragmentVertex fv in hf.FragmentVertices)
				{
					PointSaveFormat psf = new PointSaveFormat ();
					if (!psf.SetPoint (fv))
						return false;
					psfList.Add (psf);
				}
				foreach (FragmentAnchor fa in hf.FragmentAnchores)
				{
					PointSaveFormat psf = new PointSaveFormat ();
					if (!psf.SetPoint (fa))
						return false; 
					psfList.Add (psf);
				}
				PointsData = psfList.ToArray ();
				return true;
			}

			public bool GetFragment (ref HomographyFragment hf)
			{
				PointSaveFormat[] psfList = PointsData;
				List<FragmentPoint> fpList = new List<FragmentPoint> ();
				foreach (PointSaveFormat psf in psfList)
				{
					FragmentPoint fp;
					if (!psf.GetPoint (out fp))
						return false;
					fpList.Add (fp);
				}

				foreach (FragmentPoint fp in fpList)
				{
					if (fp is FragmentVertex)
						hf.FragmentVertices.Add (fp as FragmentVertex);
					if (fp is FragmentAnchor)
						hf.FragmentAnchores.Add (fp as FragmentAnchor);
				}
				return true;
			}

			[SerializeField]
			private PointSaveFormat[] PointsData;
		}

		//フラグメントの頂点のデータ
		[Serializable]
		private class PointSaveFormat
		{
			[SerializeField]
			private string Identifer;
			[SerializeField]
			private string Vertex;
			[SerializeField]
			private string UV;

			//頂点データをセット
			public bool SetPoint(FragmentPoint point)
			{
				if (point is FragmentVertex) {
					Identifer = "Vertex";
					ConvertVec2ToStr (point.Vertex, out Vertex);
					ConvertVec2ToStr (point.UV, out UV);
					return true;
				} else if (point is FragmentAnchor) {
					Identifer = "Anchor";
					ConvertVec2ToStr (point.Vertex, out Vertex);
					ConvertVec2ToStr (point.UV, out UV);
					return true;
				}
				Debug.Log ("フラグメントをコンバートできませんでした。");
				return false;
			}
			//頂点データをゲット
			public bool GetPoint(out FragmentPoint point)
			{
				Vector2 uv, vert;
				bool isMatched = true;
				isMatched |= ConvertStrToVec2 (UV, out uv); 
				isMatched |= ConvertStrToVec2 (Vertex, out vert);
				if (isMatched) {
					if (Identifer == "Vertex") {
						point = new FragmentVertex (uv, vert);
						return true;
					} else if (Identifer == "Anchor") {
						point = new FragmentAnchor (uv, vert);
						return true;
					}
				}
				point = null;
				Debug.Log ("フラグメントにコンバートできませんでした。");
				return false;
			}

			//ヘルパー関数(Vector2<-->stringコンバーター)
			private static void ConvertVec2ToStr(Vector2 vec2, out string str)
			{
				str = "(" +vec2.x+ ", " +vec2.y+ ")";
			}
			private static bool ConvertStrToVec2(string str, out Vector2 vec2)
			{
				string adjStr = new Regex ("\\s").Replace(str, "");
				string numRegex = "-?[0-9\\.]+";

				if (new Regex ("^\\(" + numRegex + "," + numRegex + "\\)$").IsMatch (adjStr)) {
					MatchCollection mc = new Regex (numRegex).Matches (adjStr);
					List<float> element = new List<float> ();
					bool isMatched = true;
					foreach (Match m in mc)
					{
						float val = 0.0f;
						if (float.TryParse (m.Value, out val))
							element.Add (val);
						else
							isMatched = false;
					}
					if (isMatched) {
						vec2 = new Vector2 (element [0], element [1]);
						return true;
					}
				}
				Debug.Log ("Vector2にコンバートできませんでした。(" + str + ")");
				vec2 = new Vector2 (0.0f, 0.0f);
				return false;
			}
		}
	}
}