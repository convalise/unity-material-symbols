
namespace com.convalise.UnityMaterialSymbols
{

[System.Serializable]
public struct MaterialSymbolData
{
	public char code;
	public bool fill;

	public MaterialSymbolData(char code, bool fill)
	{
		this.code = code;
		this.fill = fill;
	}
}

}
