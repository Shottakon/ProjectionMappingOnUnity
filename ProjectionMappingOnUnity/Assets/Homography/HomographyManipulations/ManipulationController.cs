//===========================================================
//  Author  :しょった
//  Summary :操作のコントローラー及び操作ステートのベース
//===========================================================

using System;
using UnityEngine;
using System.Collections.Generic;

namespace Manipulation{
	//操作受付用のコントローラー
	public class ManipulationController : MonoBehaviour
	{
		protected internal interface ControllerSetter
		{
			void SetController(ManipulationController controller);
		}

		private class ManipulationData
		{
			private ManipulationFormat mManipulation;
			private bool mDidStartUp;

			public ManipulationData(ManipulationFormat manipulation)
			{
				mManipulation = manipulation;
				mDidStartUp = false;
				mManipulation.Awake ();
			}
			~ManipulationData() { mManipulation.Finish (); }

			public void Run() {
				if (!mDidStartUp) {
					mManipulation.Start ();
					mDidStartUp = true;
				}
				mManipulation.Update ();
			}
 		}

		private List<ManipulationData> mManipulationStack = new List<ManipulationData> ();
		public bool IsActive { get; set; }

		void Start()
		{
			IsActive = true;
		}

		void Update()
		{
			if (!IsActive)
				return;

			if (mManipulationStack.Count > 0)
				mManipulationStack [0].Run ();
		}

		//操作を追加
		public void AddManipulation (ManipulationFormat manipulation)
		{
			if (manipulation == null) return;
			((ControllerSetter)manipulation).SetController (this);
			ManipulationData md = new ManipulationData (manipulation);
			mManipulationStack.Insert (0, md);
		}
		//操作を入れ替え
		public void ReplaceManipulation (ManipulationFormat manipulation)
		{
			RemoveManipulation ();
			AddManipulation (manipulation);
		}
		//操作を削除
		public void RemoveManipulation ()
		{
			if (mManipulationStack.Count > 0)
				mManipulationStack.RemoveAt (0);
		}
	}

	//操作のフォーマット
	public abstract class ManipulationFormat : ManipulationController.ControllerSetter
	{
		public ManipulationController Controller{ get; private set; }
		public virtual void Awake () {}
		public virtual void Start () {}
		public virtual void Update () {}
		public virtual void Finish () {}

		void ManipulationController.ControllerSetter.SetController (ManipulationController controller)
		{
			Controller = controller;
		}
	}
}