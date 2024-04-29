
using UnityEngine;
using UnityEditor;

namespace com.convalise.UnityMaterialSymbols
{

[CustomPropertyDrawer(typeof(MaterialSymbolData))]
public class MaterialSymbolDataDrawer : PropertyDrawer
{
	private Styles styles;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight * 3;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if(this.styles == null)
			this.styles = new Styles();

		if(styles.fontRef == null)
		{
			EditorGUI.LabelField(position, label, styles.gcFontError);
			return;
		}

		SerializedProperty spCode = property.FindPropertyRelative("code");
		SerializedProperty spFill = property.FindPropertyRelative("fill");
		char charValue = System.Convert.ToChar(spCode.intValue);

		label = EditorGUI.BeginProperty(position, label, property);
		GUI.SetNextControlName(label.text);
		position = EditorGUI.PrefixLabel(position, label);
		position.width = position.height;

		if(GUI.Button(position, GUIContent.none))
		{
			GUI.FocusControl(label.text);
			MaterialSymbolSelectionWindow.Init(charValue, spFill.boolValue, (code, fill) => {
				spCode.intValue = code;
				spFill.boolValue = fill;
				property.serializedObject.ApplyModifiedProperties();
			});
		}

		if(property.hasMultipleDifferentValues)
		{
			GUI.Label(position, styles.gcMixedValues, styles.gsMixedValues);
		}
		else
		{
			styles.gcSymbol.text = charValue.ToString();
			styles.gcSymbol.tooltip = MaterialSymbol.ConvertCharToHex(charValue);
			styles.gsSymbol.font = spFill.boolValue ? styles.fontRef.filled : styles.fontRef.standard;

			GUI.Label(position, styles.gcSymbol, styles.gsSymbol);
		}

		EditorGUI.EndProperty();
	}

	private class Styles
	{
		public MaterialSymbolsFontRef fontRef { get; private set; }

		public GUIContent gcSymbol { get; private set; }
		public GUIContent gcMixedValues { get; private set; }
		public GUIContent gcFontError { get; private set; }

		public GUIStyle gsSymbol { get; private set; }
		public GUIStyle gsMixedValues { get; private set; }

		public Styles()
		{
			this.fontRef = MaterialSymbol.LoadFontRef();

			this.gcSymbol = new GUIContent();
			this.gcMixedValues = new GUIContent("\u2014", "Mixed Values");
			this.gcFontError = new GUIContent("Could not find fonts reference.", EditorGUIUtility.IconContent("console.erroricon").image);

			this.gsSymbol = new GUIStyle("ControlLabel");
			this.gsSymbol.font = null;
			this.gsSymbol.fontSize = 42;
			this.gsSymbol.alignment = TextAnchor.MiddleCenter;

			this.gsMixedValues = new GUIStyle("Label");
			this.gsMixedValues.alignment = TextAnchor.MiddleCenter;
		}
	}
}

}
