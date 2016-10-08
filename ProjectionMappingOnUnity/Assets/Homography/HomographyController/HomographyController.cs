//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィ変換のコントローラー
//===========================================================

using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent (typeof(Camera))]
public class HomographyController : MonoBehaviour
{
	private Camera mCamera;

	[SerializeField]
	private float mScale = 1.0f;
	public float Scale {
		get{ return mScale; }
		set{ mScale = value; }
	}

	void Start () {
		mCamera = GetComponent<Camera> ();
	}

	void Update () {
		transform.localScale = new Vector3 (mCamera.aspect*Scale, Scale, 1.0f);
		RefreshLayerPosition ();
		//Debug.Log ("" + Time.deltaTime + "");
	}

	public void RefreshLayerPosition ()
	{
		float near = mCamera.nearClipPlane + 0.01f;
		float far = mCamera.farClipPlane - 0.01f;
		int count = transform.childCount;
		for (int i = 0; i < count; i++)
		{
			Transform tf = transform.GetChild (i);
			float per = (count<=1?0.5f : (float)i / (count - 1));
			float z = (far - near) * per + near;
			tf.localPosition = new Vector3(0, 0, z);
			tf.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
		}
	}

}
