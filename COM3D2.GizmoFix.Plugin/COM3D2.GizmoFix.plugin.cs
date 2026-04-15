using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.GizmoFix.Plugin
{
	/// <summary>
	/// プラグイン本体
	/// </summary>
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class GizmoRenderFix : BaseUnityPlugin
	{
		/// <summary>
		/// プラグイン名
		/// </summary>
		public const string PluginName = "Gizmo Render Fix Plug-In";

		/// <summary>
		/// プラグインGuid
		/// </summary>
		public const string PluginGuid = "jp.fumble.GizmoRenderFix";

		/// <summary>
		/// プラグインカンパニー
		/// </summary>
		public const string CompanyName = "Fumble Warez";

		/// <summary>
		/// プラグインコピーライト
		/// </summary>
		public const string Copyright = "Copyright © Fumble Warez 2026";

		/// <summary>
		/// プラグインバージョン
		/// </summary>
		public const string PluginVersion = "0.3.3.0";

		/// <summary>
		/// HarmonyLib(HarmonyX)のインスタンス
		/// </summary>
		private static Harmony harmony;


		/// <summary>
		/// IKDragPointのサイズを固定するかどうか
		/// </summary>
		private ConfigEntry<bool> dragPointFix;

		/// <summary>
		/// IKDragPointのサイズ
		/// </summary>
		private ConfigEntry<float> dragPointSize;

		/// <summary>
		/// IKDragPointのサイズ
		/// </summary>
		private ConfigEntry<float> ikDragPointSize;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		static GizmoRenderFix()
		{
			// パッチする
			if (harmony == null)
				harmony = new Harmony(PluginGuid);
		}

		public void Awake()
		{
			DontDestroyOnLoad(this);
			this.dragPointFix = this.Config.Bind("DragPoint Settings", "Fix", true, "DragPointのサイズを固定するかどうか");
			this.dragPointSize = this.Config.Bind("DragPoint Settings", "Size", 0.1f, "IKDragPoint以外のDragPointの固定サイズ");
			this.ikDragPointSize = this.Config.Bind("DragPoint Settings", "IKSize", 0.04f, "IKDragPointの固定サイズ");
			harmony.PatchAll(typeof(Patch_GizmoRender_RenderGizmos));
			harmony.PatchAll(typeof(Patch_GizmoRender_Awake));
			Patch_WorldTransformAxis_ConstantScreenSize.dragPointFix = this.dragPointFix;
			Patch_WorldTransformAxis_ConstantScreenSize.dragPointSize = this.dragPointSize;
			Patch_WorldTransformAxis_ConstantScreenSize.ikDragPointSize = this.ikDragPointSize;
			harmony.PatchAll(typeof(Patch_WorldTransformAxis_ConstantScreenSize));
		}

		/// <summary>
		/// [外部インターフェース]DragPointのサイズを固定するかどうかの設定
		/// </summary>
		/// <param name="dragPointFix">サイズ</param>
		public void GRFSetDragPointFix(object dragPointFix)
		{
			try
			{
				switch (dragPointFix)
				{
					case bool value:
						this.dragPointFix.Value = value;
						break;
					case bool[] valueRef when valueRef.Length != 0:
						this.dragPointFix.Value = valueRef[0];
						break;
					default:
						break;
				}
			}
			catch { /* ignored */}
		}

		/// <summary>
		/// [外部インターフェース]DragPointのサイズを固定するかどうかの取得
		/// </summary>
		/// <param name="dragPointFix">サイズの入れ物</param>
		public void GRFGetDragPointFix(object dragPointFix)
		{
			try
			{
				if (dragPointFix is bool[] valueRef && valueRef.Length != 0)
				{
					valueRef[0] = this.dragPointFix.Value;
				}
			}
			catch { /* ignored */}
		}

		/// <summary>
		/// [外部インターフェース]DragPointのサイズ変更
		/// </summary>
		/// <param name="dragPointSize">サイズ</param>
		public void GRFSetDragPointSize(object dragPointSize)
		{
			try
			{
				switch (dragPointSize)
				{
					case float value:
						this.dragPointSize.Value = value;
						break;
					case float[] valueRef when valueRef.Length != 0:
						this.dragPointSize.Value = valueRef[0];
						break;
					default:
						break;
				}
			}
			catch { /* ignored */}
		}

		/// <summary>
		/// [外部インターフェース]DragPointのサイズ取得
		/// </summary>
		/// <param name="dragPointSize">サイズの入れ物</param>
		public void GRFGetDragPointSize(object dragPointSize)
		{
			try
			{
				if (dragPointSize is float[] valueRef && valueRef.Length != 0)
				{
					valueRef[0] = this.dragPointSize.Value;
				}
			}
			catch { /* ignored */}
		}

		/// <summary>
		/// [外部インターフェース]IKDragPointのサイズ変更
		/// </summary>
		/// <param name="ikDragPointSize">サイズ</param>
		public void GRFSetIKDragPointSize(object ikDragPointSize)
		{
			try
			{
				switch (ikDragPointSize)
				{
					case float value:
						this.ikDragPointSize.Value = value;
						break;
					case float[] valueRef when valueRef.Length != 0:
						this.ikDragPointSize.Value = valueRef[0];
						break;
					default:
						break;
				}
			}
			catch { /* ignored */}
		}

		/// <summary>
		/// [外部インターフェース]IKDragPointのサイズ取得
		/// </summary>
		/// <param name="ikDragPointSize">サイズの入れ物</param>
		public void GRFGetIKDragPointSize(object ikDragPointSize)
		{
			try
			{
				if (ikDragPointSize is float[] valueRef && valueRef.Length != 0)
				{
					valueRef[0] = this.ikDragPointSize.Value;
				}
			}
			catch { /* ignored */}
		}

		/// <summary>
		/// GizmoRender.RenderGizmosパッチ
		/// 半円描画を全円に差し替え、あたり判定も全周に拡張する
		/// </summary>
		[HarmonyPatch(typeof(GizmoRender), "RenderGizmos")]
		public static class Patch_GizmoRender_RenderGizmos
		{
			private static readonly FieldInfo _uForward = AccessTools.Field(typeof(GizmoRender), "uForward");
			private static readonly FieldInfo _rForward = AccessTools.Field(typeof(GizmoRender), "rForward");
			private static readonly FieldInfo _fForward = AccessTools.Field(typeof(GizmoRender), "fForward");

			/// <summary>ホバー中の軸 (-1=なし, 0=X, 1=Y, 2=Z)</summary>
			private static int _hoveredAxis = -1;

			/// <summary>
			/// DrawCircleHalf呼び出しをDrawCircleに差し替える
			/// </summary>
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var half = AccessTools.Method(typeof(GizmoRender), "DrawCircleHalf");
				var full = AccessTools.Method(typeof(Patch_GizmoRender_RenderGizmos), "DrawCircle");
				foreach (var inst in instructions)
				{
					if (inst.opcode == OpCodes.Call && inst.operand as MethodBase == half)
					{
						yield return new CodeInstruction(OpCodes.Call, full);
					}
					else
					{
						yield return inst;
					}
				}
			}

			/// <summary>
			/// OnRenderObjectと同じ優先順位(RY→RZ→RX)でホバー軸を1つ確定する
			/// </summary>
			[HarmonyPrefix]
			public static void Prefix(GizmoRender __instance)
			{
				_hoveredAxis = -1;

				if (NInput.GetMouseButton(0) || GizmoRender.control_lock
					|| !__instance.Visible || !__instance.eRotate || !GizmoRender.UIVisible)
					return;

				Camera cam = Camera.main;
				if (cam == null) return;

				Ray ray = cam.ScreenPointToRay(Input.mousePosition);

				// RenderGizmosと同じ式でgeneralLensを計算
				float mag = (cam.transform.position - __instance.transform.position).magnitude;
				float gl = -2f * Mathf.Tan(0.5f * cam.fieldOfView) * mag / 50f * __instance.offsetScale;

				// rotationMatrix = TRS(pos, rot, (1,1,1)) の逆行列
				Matrix4x4 rotInv = Matrix4x4.TRS(
					__instance.transform.position,
					__instance.transform.rotation,
					Vector3.one).inverse;

				float thick = __instance.lineRSelectedThick;

				// RY (Y軸): planeXZ
				if (__instance.VisibleRotateY)
				{
					float enter;
					if (new Plane(__instance.transform.up, __instance.transform.position).Raycast(ray, out enter))
					{
						Vector3 p = rotInv.MultiplyPoint(ray.GetPoint(enter));
						float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
						if (r > (1f - thick) * gl && r < (1f + thick) * gl) { _hoveredAxis = 1; return; }
					}
				}

				// RZ (Z軸): planeXY
				if (__instance.VisibleRotateZ)
				{
					float enter;
					if (new Plane(__instance.transform.forward, __instance.transform.position).Raycast(ray, out enter))
					{
						Vector3 p = rotInv.MultiplyPoint(ray.GetPoint(enter));
						float r = Mathf.Sqrt(p.x * p.x + p.y * p.y);
						if (r > (1f - thick) * gl && r < (1f + thick) * gl) { _hoveredAxis = 2; return; }
					}
				}

				// RX (X軸): planeYZ
				if (__instance.VisibleRotateX)
				{
					float enter;
					if (new Plane(__instance.transform.right, __instance.transform.position).Raycast(ray, out enter))
					{
						Vector3 p = rotInv.MultiplyPoint(ray.GetPoint(enter));
						float r = Mathf.Sqrt(p.z * p.z + p.y * p.y);
						if (r > (1f - thick) * gl && r < (1f + thick) * gl) { _hoveredAxis = 0; return; }
					}
				}
			}

			[HarmonyPostfix]
			public static void Postfix(GizmoRender __instance)
			{
				_uForward.SetValue(__instance, Vector3.zero);
				_rForward.SetValue(__instance, Vector3.zero);
				_fForward.SetValue(__instance, Vector3.zero);
			}

			public static void DrawCircle(GizmoRender gizmoRender, Color col, Vector3 vtxLocal, Vector3 vtyLocal)
			{
				// 色から軸を特定（非ドラッグ中: 赤=X, 緑=Y, 青=Z）
				int axisIndex = (col.r > col.g && col.r > col.b) ? 0
									  : (col.g > col.r && col.g > col.b) ? 1
												  : 2;

				// Prefixで確定したホバー軸のみハイライト（それ以外は元色）
				Color drawCol = (_hoveredAxis == axisIndex) ? new Color(1f, 1f, 0f, 0.5f) : col;

				GL.Begin(GL.LINES);
				for (int index = 0; index < 100; index++)
				{
					if (index == 0)
						GL.Color(drawCol);
					else if (index == 50)
						GL.Color(new Color(drawCol.r * 0.5f, drawCol.g * 0.5f, drawCol.b * 0.5f, drawCol.a * 0.5f));
					// noelse
					Vector3 vector = vtxLocal * Mathf.Cos((float)Math.PI / 50f * (float)index);
					vector += vtyLocal * Mathf.Sin((float)Math.PI / 50f * (float)index);
					GL.Vertex3(vector.x, vector.y, vector.z);
					vector = vtxLocal * Mathf.Cos((float)Math.PI / 50f * (float)(index + 1));
					vector += vtyLocal * Mathf.Sin((float)Math.PI / 50f * (float)(index + 1));
					GL.Vertex3(vector.x, vector.y, vector.z);
				}
				GL.End();
			}

		}

		/// <summary>
		/// GizmoRender.Awakeパッチ
		/// 回転ハンドルのあたり判定幅を広げる（移動ハンドルには影響しない）
		/// </summary>
		[HarmonyPatch(typeof(GizmoRender), "Awake")]
		public static class Patch_GizmoRender_Awake
		{
			/// <summary>
			/// lineRSelectedThickを既定の0.1fから0.2fに拡張する
			/// </summary>
			[HarmonyPostfix]
			public static void Postfix(GizmoRender __instance)
			{
				__instance.lineRSelectedThick = 0.2f;
			}
		}

		[HarmonyPatch(typeof(WorldTransformAxis), "Update")]
		public static class Patch_WorldTransformAxis_ConstantScreenSize
		{
			static readonly FieldInfo _parentObj = AccessTools.Field(typeof(WorldTransformAxis), "parent_obj_");


			/// <summary>
			/// IKDragPointのサイズを固定するかどうか
			/// </summary>
			public static ConfigEntry<bool> dragPointFix;

			/// <summary>
			/// IKDragPointのサイズ
			/// </summary>
			public static ConfigEntry<float> dragPointSize { get; set; }

			/// <summary>
			/// IKDragPointのサイズ
			/// </summary>
			public static ConfigEntry<float> ikDragPointSize { get; set; }

			[HarmonyPostfix]
			public static void Postfix(WorldTransformAxis __instance)
			{

				// 設定無効化中なら抜ける
				if (!dragPointFix.Value)
					return;
				// parent_obj_ != null は子軸オブジェクト（X/Y/Z）なのでスキップ
				if (_parentObj.GetValue(__instance) != null)
					return;

				Camera camera = Camera.main;
				if (camera == null)
					return;

				var dist = (__instance && __instance.TargetObject && __instance.TargetObject.name != null && __instance.TargetObject.name.StartsWith("IKDragPoint_Bip01")) ?
						Vector3.Distance(camera.transform.position, __instance.transform.position) * ikDragPointSize.Value :
						Vector3.Distance(camera.transform.position, __instance.transform.position) * dragPointSize.Value;
				__instance.transform.localScale = new Vector3(dist, dist, dist);
			}
		}
	}
}
