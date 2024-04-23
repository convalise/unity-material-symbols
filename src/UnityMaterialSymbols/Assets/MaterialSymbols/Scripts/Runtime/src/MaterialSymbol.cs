
using UnityEngine;
using UnityEngine.UI;

namespace com.convalise.UnityMaterialSymbols
{

public class MaterialSymbol : Text
{
	[SerializeField]
	private MaterialSymbolData _symbol;

	[SerializeField, Range(0f, 2f)]
	private float _scale = 1f;

	public MaterialSymbolData symbol
	{
		get { return _symbol; }
		set { _symbol = value; UpdateSymbol(); }
	}

	public char code
	{
		get { return _symbol.code; }
		set { _symbol.code = value; UpdateSymbol(); }
	}

	public bool fill
	{
		get { return _symbol.fill; }
		set { _symbol.fill = value; UpdateSymbol(); }
	}

	public float scale
	{
		get { return this._scale; }
		set { this._scale = value; UpdateFontSize(); }
	}

	private MaterialSymbolsFontRef fontRef;

	protected override void Start()
	{
		base.Start();

		if(string.IsNullOrEmpty(base.text))
		{
			Init();
		}

		if(base.font == null)
		{
			UpdateSymbol();
		}
	}

	#if UNITY_EDITOR
	protected override void Reset()
	{
		base.Reset();
		Init();
	}

	protected override void OnValidate()
	{
		base.OnValidate();
		UpdateSymbol();
		UpdateFontSize();
	}
	#endif

	/// <summary> Properly initializes base Text class. </summary>
	private void Init()
	{
		this._symbol = new MaterialSymbolData('\uef55', false);

		base.text = null;
		base.font = null;
		base.color = new Color(0.196f, 0.196f, 0.196f, 1.000f);
		base.material = null;
		base.alignment = TextAnchor.MiddleCenter;
		base.supportRichText = false;
		base.horizontalOverflow = HorizontalWrapMode.Overflow;
		base.verticalOverflow = VerticalWrapMode.Overflow;

		UpdateSymbol();
		UpdateFontSize();
	}

	/// <summary> Updates font based on fill state. </summary>
	private void UpdateSymbol()
	{
		if(fontRef == null)
			fontRef = LoadFontRef();

		if(fontRef != null)
			base.font = _symbol.fill ? fontRef.filled : fontRef.standard;

		base.text = _symbol.code.ToString();
	}

	/// <summary> Updates font size based on transform size. </summary>
	private void UpdateFontSize()
	{
		base.fontSize = Mathf.FloorToInt(Mathf.Min(base.rectTransform.rect.width, base.rectTransform.rect.height) * this._scale);
	}

	protected override void OnRectTransformDimensionsChange()
	{
		base.OnRectTransformDimensionsChange();
		UpdateFontSize();
	}

	/// <summary> Loads the font ref asset from Resources. </summary>
	public static MaterialSymbolsFontRef LoadFontRef()
	{
		return Resources.Load<MaterialSymbolsFontRef>("MaterialSymbolsFontRef");
	}

	/// <summary> Converts from unicode char to hexadecimal string representation. </summary>
	public static string ConvertCharToHex(char code)
	{
		try
		{
			return System.Convert.ToString(code, 16);
		}
		catch(System.Exception)
		{
			return default(string);
		}
	}

	/// <summary> Converts from hexadecimal string representation to unicode char. </summary>
	public static char ConvertHexToChar(string hex)
	{
		try
		{
			return System.Convert.ToChar(System.Convert.ToInt32(hex, 16));
		}
		catch(System.Exception)
		{
			return default(char);
		}
	}
}

}
