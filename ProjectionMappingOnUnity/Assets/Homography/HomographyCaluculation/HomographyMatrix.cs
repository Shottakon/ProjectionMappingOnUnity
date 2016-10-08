//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィ行列を生成するクラス
//===========================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Homography
{
	//ホモグラフィの行列データ
	public class HomographyMatrix
	{
		private List<Vector2> mPositionList = new List<Vector2> ();

		public void AddPosition(Vector2 posBefore, Vector2 posAfter)
		{
			mPositionList.Add (posBefore);
			mPositionList.Add (posAfter);
		}

		//ホモグラフィの変換行列を取得
		public int CreateHomographyMatrix (out Matrix4x4 matrix)
		{
			matrix = Matrix4x4.identity;
			//標本点の数
			int count = mPositionList.Count/2;
			if (count < 4) return 2;

			//最小二乗法を利用してホモグラフィ変換行列を取得
			MatrixNxM a = MatrixNxM.ZeroMatrix (count * 2, 8);
			MatrixNxM b = MatrixNxM.ZeroMatrix (count * 2, 1);
			for (int i = 0; i < count; i++) {
				int index0 = i * 2;
				int index1 = i * 2 + 1;
				Vector2 posBefore = mPositionList [i * 2];
				Vector2 posAfter  = mPositionList [i * 2 + 1];

				a.Mat [index0, 0] = posBefore.x;
				a.Mat [index0, 1] = posBefore.y;
				a.Mat [index0, 2] = 1.0f;
				a.Mat [index0, 3] = 0.0f;
				a.Mat [index0, 4] = 0.0f;
				a.Mat [index0, 5] = 0.0f;
				a.Mat [index0, 6] = -posBefore.x * posAfter.x;
				a.Mat [index0, 7] = -posBefore.y * posAfter.x;

				a.Mat [index1, 0] = 0.0f;
				a.Mat [index1, 1] = 0.0f;
				a.Mat [index1, 2] = 0.0f;
				a.Mat [index1, 3] = posBefore.x;
				a.Mat [index1, 4] = posBefore.y;
				a.Mat [index1, 5] = 1.0f;
				a.Mat [index1, 6] = -posBefore.x * posAfter.y;
				a.Mat [index1, 7] = -posBefore.y * posAfter.y;

				b.Mat [index0, 0] = posAfter.x;
				b.Mat [index1, 0] = posAfter.y;
			}

			MatrixNxM a_inv = a.InverseMatrix ();
			if (a_inv != null) {
				MatrixNxM h = a_inv * b;

				Matrix4x4 homoMat = Matrix4x4.identity;
				homoMat.m00 = (float)h.Mat [0, 0];
				homoMat.m01 = (float)h.Mat [1, 0];
				homoMat.m03 = (float)h.Mat [2, 0];
				homoMat.m10 = (float)h.Mat [3, 0];
				homoMat.m11 = (float)h.Mat [4, 0];
				homoMat.m13 = (float)h.Mat [5, 0];
				homoMat.m30 = (float)h.Mat [6, 0];
				homoMat.m31 = (float)h.Mat [7, 0];
				homoMat.m33 = 1.0f;
				matrix = homoMat;
				return 0;
			} else {
				return 1;
			}
		}
	}
}