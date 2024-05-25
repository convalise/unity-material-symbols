
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

using System.Reflection;

namespace com.convalise.UnityMaterialSymbols
{

[CustomEditor(typeof(MaterialSymbol), true), CanEditMultipleObjects]
public class MaterialSymbolEditor : UnityEditor.UI.TextEditor
{
	private SerializedProperty spSymbol;
	private SerializedProperty spFill;
	private SerializedProperty spScale;
	private SerializedProperty spAlignment;

	private FontDataDrawer pdFontData;

	private GUIStyle gsPreviewSymbol;

	protected override void OnEnable()
	{
		base.OnEnable();

		spSymbol = serializedObject.FindProperty("_symbol");
		spFill = serializedObject.FindProperty("_symbol.fill");
		spScale = serializedObject.FindProperty("_scale");
		spAlignment = serializedObject.FindProperty("m_FontData.m_Alignment");

		pdFontData = new FontDataDrawer();

		gsPreviewSymbol = new GUIStyle();
		gsPreviewSymbol.alignment = TextAnchor.MiddleCenter;
		gsPreviewSymbol.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(spSymbol);
		EditorGUILayout.PropertyField(spFill);
		EditorGUILayout.PropertyField(spScale);

		DoTextAlignmentControl(pdFontData, spAlignment);

		EditorGUILayout.Space();

		base.AppearanceControlsGUI();
		base.RaycastControlsGUI();
		#if UNITY_2019_4_OR_NEWER
		base.MaskableControlsGUI();
		#endif

		serializedObject.ApplyModifiedProperties();
	}

	public override bool HasPreviewGUI()
	{
		return true;
	}

	public override string GetInfoString()
	{
		MaterialSymbol targetMS = target as MaterialSymbol;
		return string.Format("Code: {0}", MaterialSymbol.ConvertCharToHex(targetMS.symbol.code));
	}

	public override void OnPreviewGUI(Rect drawArea, GUIStyle _)
	{
		MaterialSymbol targetMS = target as MaterialSymbol;
		int size = (int) Mathf.Min(drawArea.width, drawArea.height);

		drawArea.x += (drawArea.width * 0.5f) - (size * 0.5f);
		drawArea.y += (drawArea.height * 0.5f) - (size * 0.5f);
		drawArea.width = drawArea.height = size;

		gsPreviewSymbol.fontSize = size;
		gsPreviewSymbol.font = targetMS.font;

		EditorGUI.DrawTextureTransparent(drawArea, null, ScaleMode.StretchToFill, 1f);
		EditorGUI.LabelField(drawArea, targetMS.symbol.code.ToString(), gsPreviewSymbol);
	}

	/// <summary> Reflection for the private synonymous method from the FontDataDrawer class. </summary>
	private static void DoTextAlignmentControl(FontDataDrawer propertyDrawer, SerializedProperty property)
	{
		Rect position = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
		try
		{
			if(miDoTextAlignmentControl == null)
				miDoTextAlignmentControl = typeof(FontDataDrawer).GetMethod("DoTextAligmentControl", BindingFlags.NonPublic | BindingFlags.Instance);
			miDoTextAlignmentControl.Invoke(propertyDrawer, new object[] { position, property });
		}
		catch(System.Exception)
		{
			EditorGUI.PropertyField(position, property);
		}
	}
	private static MethodInfo miDoTextAlignmentControl;
}

}
