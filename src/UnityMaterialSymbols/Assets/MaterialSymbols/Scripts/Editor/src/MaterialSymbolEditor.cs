
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
	private SerializedProperty spColor;
	private SerializedProperty spMaterial;
	private SerializedProperty spRaycastTarget;
	private SerializedProperty spAlignment;

	private FontDataDrawer pdFontData;

	protected override void OnEnable()
	{
		base.OnEnable();

		spSymbol = serializedObject.FindProperty("_symbol");
		spFill = serializedObject.FindProperty("_symbol.fill");
		spScale = serializedObject.FindProperty("_scale");
		spColor = serializedObject.FindProperty("m_Color");
		spMaterial = serializedObject.FindProperty("m_Material");
		spRaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
		spAlignment = serializedObject.FindProperty("m_FontData.m_Alignment");

		pdFontData = new FontDataDrawer();
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(spSymbol);
		EditorGUILayout.PropertyField(spFill);
		EditorGUILayout.PropertyField(spScale);

		DoTextAlignmentControl(pdFontData, spAlignment);

		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(spColor);
		EditorGUILayout.PropertyField(spMaterial);
		EditorGUILayout.PropertyField(spRaycastTarget);

		serializedObject.ApplyModifiedProperties();
	}

	/// <summary> Reflection for the private synonymous method from the FontDataDrawer class. </summary>
	private static void DoTextAlignmentControl(FontDataDrawer propertyDrawer, SerializedProperty property)
	{
		try
		{
			MethodInfo miDoTextAlignmentControl = typeof(FontDataDrawer).GetMethod("DoTextAligmentControl", BindingFlags.NonPublic | BindingFlags.Instance);
			if(miDoTextAlignmentControl != null)
			{
				Rect position = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
				miDoTextAlignmentControl.Invoke(propertyDrawer, new object[] { position, property });
			}
		}
		catch(System.Exception e)
		{
			Debug.LogException(e);
		}
	}
}

}
