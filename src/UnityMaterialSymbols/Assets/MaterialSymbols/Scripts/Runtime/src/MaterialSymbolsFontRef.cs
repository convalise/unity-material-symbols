
using UnityEngine;

namespace com.convalise.UnityMaterialSymbols
{

public class MaterialSymbolsFontRef : ScriptableObject
{
	[SerializeField]
	private Font _standard;

	[SerializeField]
	private Font _filled;

	public Font standard { get { return _standard; } }
	public Font filled { get { return (_filled != null) ? _filled : _standard; } }

	#if UNITY_EDITOR
	public string GetCodepointsEditorPath()
	{
		if(_standard != null)
			return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(_standard)), "codepoints");
		return null;
	}
	#endif
}

}
