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
		public const string PluginVersion = "0.5.5.0";

		/// <summary>
		/// HarmonyLib(HarmonyX)のインスタンス
		/// </summary>
		private static Harmony _harmony;

		/// <summary>
		/// IKDragPointのサイズを固定するかどうか
		/// </summary>
		private ConfigEntry<bool> _dragPointFix;

		/// <summary>
		/// IKDragPointのサイズ
		/// </summary>
		private ConfigEntry<float> _dragPointSize;

		/// <summary>
		/// IKDragPointのサイズ
		/// </summary>
		private ConfigEntry<float> _ikDragPointSize;

		/// <summary>
		/// 線のホバー時の色
		/// </summary>
		private ConfigEntry<Color> _hoverColor;

		/// <summary>
		/// 線のドラッグ中の色
		/// </summary>
		private ConfigEntry<Color> _dragColor;

		/// <summary>
		/// 線の幅[表]
		/// </summary>
		private ConfigEntry<float> _lineWidthFront;

		/// <summary>
		/// 線の幅[裏]
		/// </summary>
		private ConfigEntry<float> _lineWidthBack;

		/// <summary>
		/// 線の非透明度[表]
		/// </summary>
		private ConfigEntry<float> _lineOpacityFront;

		/// <summary>
		/// 線の非透明度[裏]
		/// </summary>
		private ConfigEntry<float> _lineOpacityBack;

		/// <summary>
		/// 線の非透明度[ドラッグ中の他軸]
		/// </summary>
		private ConfigEntry<float> _lineOpacityInactive;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		static GizmoRenderFix()
		{
			// パッチする
			if (_harmony == null)
				_harmony = new Harmony(PluginGuid);
		}

		public void Awake()
		{
			DontDestroyOnLoad(this);
			this._dragPointFix = this.Config.Bind("DragPoint Settings", "Fix", true, "DragPointのサイズを固定するかどうか");
			this._dragPointSize = this.Config.Bind("DragPoint Settings", "Size", 0.1f, "IKDragPoint以外のDragPointの固定サイズ");
			this._ikDragPointSize = this.Config.Bind("DragPoint Settings", "IKSize", 0.04f, "IKDragPointの固定サイズ");
			this._hoverColor = this.Config.Bind("DrawCircle Settings", "HoverColor", new Color(1.0f, 1.0f, 0.0f), "軸回転用円のホバー色");
			this._dragColor = this.Config.Bind("DrawCircle Settings", "DragColor", new Color(1.0f, 1.0f, 0.0f), "軸回転用円のドラッグ中の色");
			this._lineWidthFront = this.Config.Bind("DrawCircle Settings", "WidthFront", 0.04f, "軸回転用円の表側線幅");
			this._lineWidthBack = this.Config.Bind("DrawCircle Settings", "WidthBack", 0.03f, "軸回転用円の裏側線幅");
			this._lineOpacityFront = this.Config.Bind("DrawCircle Settings", "OpacityFront", 0.6f, "軸回転用円の表側不透明度");
			this._lineOpacityBack = this.Config.Bind("DrawCircle Settings", "OpacityBack", 0.3f, "軸回転用円の裏側不透明度");
			this._lineOpacityInactive = this.Config.Bind("DrawCircle Settings", "OpacityInactive", 0.3f, "ドラッグ中他軸の不透明度");

			_harmony.PatchAll(typeof(Patch_GizmoRender_RenderGizmos));
			_harmony.PatchAll(typeof(Patch_GizmoRender_Awake));
			_harmony.PatchAll(typeof(Patch_GizmoRender_OnRenderObject));

			Patch_GizmoRender_RenderGizmos._hoverColor = this._hoverColor;
			Patch_GizmoRender_RenderGizmos._dragColor = this._dragColor;
			Patch_GizmoRender_RenderGizmos._lineWidthFront = this._lineWidthFront;
			Patch_GizmoRender_RenderGizmos._lineWidthBack = this._lineWidthBack;
			Patch_GizmoRender_RenderGizmos._lineOpacityFront = this._lineOpacityFront;
			Patch_GizmoRender_RenderGizmos._lineOpacityBack = this._lineOpacityBack;
			Patch_GizmoRender_RenderGizmos._lineOpacityInactive = this._lineOpacityInactive;
			Patch_WorldTransformAxis_ConstantScreenSize._dragPointFix = this._dragPointFix;
			Patch_WorldTransformAxis_ConstantScreenSize._dragPointSize = this._dragPointSize;
			Patch_WorldTransformAxis_ConstantScreenSize._ikDragPointSize = this._ikDragPointSize;
			_harmony.PatchAll(typeof(Patch_WorldTransformAxis_ConstantScreenSize));
		}

		public void Update()
		{
			Patch_GizmoRender_RenderGizmos._hoverActive = null;
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
						this._dragPointFix.Value = value;
						break;
					case bool[] valueRef when valueRef.Length != 0:
						this._dragPointFix.Value = valueRef[0];
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
					valueRef[0] = this._dragPointFix.Value;
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
						this._dragPointSize.Value = value;
						break;
					case float[] valueRef when valueRef.Length != 0:
						this._dragPointSize.Value = valueRef[0];
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
					valueRef[0] = this._dragPointSize.Value;
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
						this._ikDragPointSize.Value = value;
						break;
					case float[] valueRef when valueRef.Length != 0:
						this._ikDragPointSize.Value = valueRef[0];
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
					valueRef[0] = this._ikDragPointSize.Value;
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
			private static readonly FieldInfo _beSelectedType = AccessTools.Field(typeof(GizmoRender), "beSelectedType");

			/// <summary>
			/// 線のホバー時の色
			/// </summary>
			internal static ConfigEntry<Color> _hoverColor;
			/// <summary>
			/// 線のドラッグ中の色
			/// </summary>
			internal static ConfigEntry<Color> _dragColor;
			/// <summary>
			/// 線の幅[表]
			/// </summary>
			internal static ConfigEntry<float> _lineWidthFront;
			/// <summary>
			/// 線の幅[裏]
			/// </summary>
			internal static ConfigEntry<float> _lineWidthBack;
			/// <summary>
			/// 線の非透明度[表]
			/// </summary>
			internal static ConfigEntry<float> _lineOpacityFront;
			/// <summary>
			/// 線の非透明度[裏]
			/// </summary>
			internal static ConfigEntry<float> _lineOpacityBack;
			/// <summary>
			/// 線の非透明度[ドラッグ中の他軸]
			/// </summary>
			internal static ConfigEntry<float> _lineOpacityInactive;

			/// <summary>
			/// いずれかのGizmoRenderがホバー中
			/// </summary>
			internal static GizmoRender _hoverActive;

			/// <summary>
			/// ホバー中の軸 (-1=なし, 0=X, 1=Y, 2=Z)
			/// </summary>
			private static int _hoveredAxis = -1;

			/// <summary>
			/// ドラッグ中の軸 (-1=なし, 0=X, 1=Y, 2=Z)
			/// </summary>
			private static int _draggingAxis = -1;

			/// <summary>
			/// DrawCircleHalf呼び出しをDrawCircleに差し替える
			/// </summary>
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var half = AccessTools.Method(typeof(GizmoRender), "DrawCircleHalf");
				var full = AccessTools.Method(typeof(Patch_GizmoRender_RenderGizmos), "DrawCircle");
				var tanMethod = AccessTools.Method(typeof(Mathf), "Tan", new Type[] { typeof(float) });
				var callCount = 0;
				foreach (var inst in instructions)
				{
					if (inst.opcode == OpCodes.Call && inst.operand as MethodBase == half)
					{
						yield return new CodeInstruction(OpCodes.Ldc_I4, callCount++);
						yield return new CodeInstruction(OpCodes.Call, full);
					}
					else
					{
						if (inst.opcode == OpCodes.Call && inst.operand as MethodBase == tanMethod)
						{
							// -2 * Tan(0.5*fov*(-Deg2Rad)) = 2 * Tan(fov/2_rad) > 0
							yield return new CodeInstruction(OpCodes.Ldc_R4, -Mathf.Deg2Rad);
							yield return new CodeInstruction(OpCodes.Mul);
							yield return inst;
						}
						else if (inst.opcode == OpCodes.Ldc_R4
								 && inst.operand is float
								 && Mathf.Approximately((float)inst.operand, 50f))
						{
							// /50f → /5f: FOV60°での元サイズを維持しつつFOV変化に正しく追従
							yield return new CodeInstruction(OpCodes.Ldc_R4, 5f);
						}
						else
						{
							yield return inst;
						}
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

				// beSelectedType の enum 値 (RX=10, RY=11, RZ=12)
				var bst = Convert.ToInt32(_beSelectedType.GetValue(__instance));
				_draggingAxis = bst == 10 ? 0 : bst == 11 ? 1 : bst == 12 ? 2 : -1;

				if (NInput.GetMouseButton(0) || GizmoRender.control_lock
					|| !__instance.Visible || !__instance.eRotate || !GizmoRender.UIVisible)
					return;

				var cam = Camera.main;
				if (cam == null) return;

				var ray = cam.ScreenPointToRay(Input.mousePosition);

				// RenderGizmosと同じ式でgeneralLensを計算
				var mag = (cam.transform.position - __instance.transform.position).magnitude;
				// ホバー判定は競合プラグインのドラッグ判定と同じ正しい式を使用
				var gl = 2f * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad) * mag / 5f * __instance.offsetScale;

				// rotationMatrix = TRS(pos, rot, (1,1,1)) の逆行列
				var rotInv = Matrix4x4.TRS(
					__instance.transform.position,
					__instance.transform.rotation,
					Vector3.one).inverse;

				var thick = __instance.lineRSelectedThick;

				// RY (Y軸): planeXZ
				if (__instance.VisibleRotateY)
				{
					float enter;
					if (new Plane(__instance.transform.up, __instance.transform.position).Raycast(ray, out enter))
					{
						var p = rotInv.MultiplyPoint(ray.GetPoint(enter));
						var r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
						if (r > (1f - thick) * gl && r < (1f + thick) * gl) { _hoveredAxis = 1; return; }
					}
				}

				// RZ (Z軸): planeXY
				if (__instance.VisibleRotateZ)
				{
					float enter;
					if (new Plane(__instance.transform.forward, __instance.transform.position).Raycast(ray, out enter))
					{
						var p = rotInv.MultiplyPoint(ray.GetPoint(enter));
						var r = Mathf.Sqrt(p.x * p.x + p.y * p.y);
						if (r > (1f - thick) * gl && r < (1f + thick) * gl) { _hoveredAxis = 2; return; }
					}
				}

				// RX (X軸): planeYZ
				if (__instance.VisibleRotateX)
				{
					float enter;
					if (new Plane(__instance.transform.right, __instance.transform.position).Raycast(ray, out enter))
					{
						var p = rotInv.MultiplyPoint(ray.GetPoint(enter));
						var r = Mathf.Sqrt(p.z * p.z + p.y * p.y);
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

			public static void DrawCircle(GizmoRender gizmoRender, Color col, Vector3 vtxLocal, Vector3 vtyLocal, int axisIndex)
			{
				Color drawColor;
				var opacity = 1.0f;
				if (0 <= _draggingAxis)
				{
					if (_draggingAxis == axisIndex)
					{
						drawColor = _dragColor.Value;
					}
					else
					{
						drawColor = col;
						opacity = _lineOpacityInactive.Value;
					}
				}
				else if (GizmoRender.control_lock)
				{
					// 他GizmoRenderにドラッグされてる軸がある
					drawColor = col;
					opacity = _lineOpacityInactive.Value;
				}
				else if (_hoveredAxis == axisIndex && (_hoverActive == null || _hoverActive == gizmoRender))
				{
					// ホバー中
					drawColor = _hoverColor.Value;
					_hoverActive = gizmoRender;
				}
				else
				{
					// 通常
					drawColor = col;
				}

				Color drawColorFront = new Color(drawColor.r, drawColor.g, drawColor.b, drawColor.a * _lineOpacityFront.Value * opacity);
				Color drawColorBack = new Color(drawColor.r, drawColor.g, drawColor.b, drawColor.a * _lineOpacityBack.Value * opacity);

				var radius = vtxLocal.magnitude;
				var halfWidthFront = radius * _lineWidthFront.Value;   // 線の半幅（radius比で調整）
				var halfWidthBack = radius * _lineWidthBack.Value;   // 線の半幅（radius比で調整）

				// カメラ位置をgizmoローカル空間へ（matScal=1前提）
				var cam = Camera.main;
				var camLocal = cam != null
					? Quaternion.Inverse(gizmoRender.transform.rotation)
					  * (cam.transform.position - gizmoRender.transform.position)
					: Vector3.back * 1000f;

				GL.Begin(GL.TRIANGLES);
				for (var index = 0; index < 100; index++)
				{
					var a0 = (float)Math.PI / 50f * index;
					var a1 = (float)Math.PI / 50f * (index + 1);

					var A = vtxLocal * Mathf.Cos(a0) + vtyLocal * Mathf.Sin(a0);
					var B = vtxLocal * Mathf.Cos(a1) + vtyLocal * Mathf.Sin(a1);

					// 線分方向とカメラ方向の外積 → 線に垂直でカメラに向く方向
					var lineDir = (B - A).normalized;
					var viewDir = (camLocal - (A + B) * 0.5f).normalized;
					var perp = Vector3.Cross(lineDir, viewDir).normalized * (index < 50 ? halfWidthFront : halfWidthBack);

					GL.Color(index < 50 ? drawColorFront : drawColorBack);

					// 四角形 = 三角形 × 2
					GL.Vertex(A + perp);
					GL.Vertex(A - perp);
					GL.Vertex(B - perp);

					GL.Vertex(A + perp);
					GL.Vertex(B - perp);
					GL.Vertex(B + perp);
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

		/// <summary>
		/// GizmoRender.OnRenderObjectパッチ
		/// ドラッグ判定前にgeneralLensをFOVラジアン補正式で再計算する
		/// </summary>
		[HarmonyPatch(typeof(GizmoRender), "OnRenderObject")]
		public static class Patch_GizmoRender_OnRenderObject
		{
			private static readonly FieldInfo _generalLens = AccessTools.Field(typeof(GizmoRender), "generalLens");

			[HarmonyPrefix]
			public static void Prefix(GizmoRender __instance)
			{
				if (!__instance.Visible || !__instance.eRotate)
					return;

				var cam = Camera.main;
				if (cam == null)
					return;

				var mag = (cam.transform.position - __instance.transform.position).magnitude;
				// ドラッグ判定用: FOVを正しくラジアン変換して generalLens を補正
				var correctedLens = 2f * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad)
					* mag / 5f * __instance.offsetScale;
				_generalLens.SetValue(__instance, correctedLens);
				// ※ RenderGizmos() が後で元の式で再計算するため描画サイズは変わらない
			}
		}

		[HarmonyPatch(typeof(WorldTransformAxis), "Update")]
		public static class Patch_WorldTransformAxis_ConstantScreenSize
		{
			static readonly FieldInfo _parentObj = AccessTools.Field(typeof(WorldTransformAxis), "parent_obj_");


			/// <summary>
			/// IKDragPointのサイズを固定するかどうか
			/// </summary>
			internal static ConfigEntry<bool> _dragPointFix;

			/// <summary>
			/// IKDragPointのサイズ
			/// </summary>
			internal static ConfigEntry<float> _dragPointSize;

			/// <summary>
			/// IKDragPointのサイズ
			/// </summary>
			internal static ConfigEntry<float> _ikDragPointSize;

			[HarmonyPostfix]
			public static void Postfix(WorldTransformAxis __instance)
			{

				// 設定無効化中なら抜ける
				if (!_dragPointFix.Value)
					return;
				// parent_obj_ != null は子軸オブジェクト（X/Y/Z）なのでスキップ
				if (_parentObj.GetValue(__instance) != null)
					return;

				var camera = Camera.main;
				if (camera == null)
					return;

				var dist = (__instance && __instance.TargetObject && __instance.TargetObject.name != null && __instance.TargetObject.name.StartsWith("IKDragPoint_Bip01")) ?
						Vector3.Distance(camera.transform.position, __instance.transform.position) * _ikDragPointSize.Value :
						Vector3.Distance(camera.transform.position, __instance.transform.position) * _dragPointSize.Value;
				__instance.transform.localScale = new Vector3(dist, dist, dist);
			}
		}
	}
}
