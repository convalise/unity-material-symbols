﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace com.convalise.UnityMaterialSymbols
{

public class MaterialSymbolSelectionWindow : EditorWindow
{
	private CodepointData[] codepointsCollection;
	private CodepointData[] filteredCollection;
	private MaterialSymbolsFontRef fontRef;

	private CodepointData selected;
	private System.Action<char, bool> OnSelectionChanged;

	private SearchField searchField;
	private string searchString = string.Empty;
	private Vector2 scrollPos = Vector2.zero;
	private int undoGroup;

	private bool showNames = true;
	private bool allowFocusSearchField = true;
	private bool allowKeepActiveInView = true;
	private bool enableRegexSearch = false;
	private int sortId = 1;

	private bool fill = false;
	private bool focusSearchField = false;
	private bool keepActiveInView = false;

	private int columns;
	private int rows;

	private Styles styles;

	private readonly string showNamesEPK = typeof(MaterialSymbolSelectionWindow) + ".showNames";
	private readonly string focusSearchFieldEPK = typeof(MaterialSymbolSelectionWindow) + ".focusSearchField";
	private readonly string keepActiveInViewEPK = typeof(MaterialSymbolSelectionWindow) + ".keepActiveInView";
	private readonly string enableRegexSearchEPK = typeof(MaterialSymbolSelectionWindow) + ".enableRegexSearch";
	private readonly string sortIdEPK = typeof(MaterialSymbolSelectionWindow) + ".sortId";

	public static void Init(char preSelected, bool preFilled, System.Action<char, bool> onSelectionChanged)
	{
		MaterialSymbolSelectionWindow window = GetWindow<MaterialSymbolSelectionWindow>(true);
		window.wantsMouseMove = true;
		window.LoadDependencies(preSelected, preFilled, onSelectionChanged);
		window.ShowAuxWindow();
	}

	private void LoadDependencies(char preSelected, bool preFilled, System.Action<char, bool> onSelectionChanged)
	{
		searchField = new SearchField();
		searchField.downOrUpArrowKeyPressed += () => SelectRelative(0);

		fontRef = MaterialSymbol.LoadFontRef();

		if((fontRef == null) || string.IsNullOrEmpty(fontRef.GetCodepointsEditorPath()))
			return;

		showNames = EditorPrefs.GetBool(showNamesEPK, showNames);
		allowFocusSearchField = EditorPrefs.GetBool(focusSearchFieldEPK, allowFocusSearchField);
		allowKeepActiveInView = EditorPrefs.GetBool(keepActiveInViewEPK, allowKeepActiveInView);
		enableRegexSearch = EditorPrefs.GetBool(enableRegexSearchEPK, enableRegexSearch);
		sortId = EditorPrefs.GetInt(sortIdEPK, sortId);

		focusSearchField = allowFocusSearchField;
		keepActiveInView = allowKeepActiveInView;

		codepointsCollection = File.ReadAllLines(fontRef.GetCodepointsEditorPath()).Select((codepoint, index) => new CodepointData(codepoint, index)).ToArray();
		RunSorter();

		OnSelectionChanged = onSelectionChanged;

		selected = codepointsCollection.FirstOrDefault(data => data.code == preSelected);
		fill = preFilled;
	}

	private void OnEnable()
	{
		base.titleContent = new GUIContent("Material Symbol Selection");
		base.minSize = new Vector2((Styles.iconSize + Styles.labelHeight + Styles.spacing) * 5f + Styles.verticalScrollbarFixedWidth + 1f, (Styles.iconSize + Styles.labelHeight + Styles.spacing) * 6f + Styles.toolbarFixedHeight);

		undoGroup = Undo.GetCurrentGroup();
	}

	private void OnDisable()
	{
		codepointsCollection = null;
		filteredCollection = null;
		fontRef = null;
		System.GC.Collect();

		Undo.SetCurrentGroupName(Regex.Replace(Undo.GetCurrentGroupName(), "\\.code|\\.fill", string.Empty));
		Undo.CollapseUndoOperations(undoGroup);
	}

	private void OnGUI()
	{
		if(fontRef == null)
		{
			EditorGUILayout.HelpBox("Could not find fonts reference.", MessageType.Error);
			return;
		}

		if((codepointsCollection == null) || (codepointsCollection.Length == 0))
		{
			EditorGUILayout.HelpBox("Could not find codepoints data.", MessageType.Error);
			return;
		}

		if(styles == null)
		{
			styles = new Styles();
			styles.gsIconImage.font = fill ? fontRef.filled : fontRef.standard;
		}

		OnHeaderGUI();
		OnBodyGUI();

		if(Event.current.type == EventType.KeyDown)
		{
			if(Event.current.keyCode == KeyCode.LeftArrow)
			{
				SelectRelative(-1);
				Event.current.Use();
			}

			if(Event.current.keyCode == KeyCode.RightArrow)
			{
				SelectRelative(+1);
				Event.current.Use();
			}

			if(Event.current.keyCode == KeyCode.UpArrow)
			{
				SelectRelative(-columns);
				Event.current.Use();
			}

			if(Event.current.keyCode == KeyCode.DownArrow)
			{
				SelectRelative(+columns);
				Event.current.Use();
			}

			if(Event.current.keyCode == KeyCode.PageUp)
			{
				SelectRelative(-(columns * 6));
				Event.current.Use();
			}

			if(Event.current.keyCode == KeyCode.PageDown)
			{
				SelectRelative(+(columns * 6));
				Event.current.Use();
			}

			if(Event.current.keyCode == KeyCode.Home)
			{
				SelectAbsolute(0);
				Event.current.Use();
			}

			if(Event.current.keyCode == KeyCode.End)
			{
				SelectAbsolute(filteredCollection.Length - 1);
				Event.current.Use();
			}

			if((Event.current.keyCode == KeyCode.Return) || (Event.current.keyCode == KeyCode.KeypadEnter))
			{
				if(!searchField.HasFocus())
				{
					EditorApplication.delayCall += base.Close;
					GUIUtility.ExitGUI();
					Event.current.Use();
				}
			}

			if(Event.current.keyCode == KeyCode.Escape)
			{
				Undo.RevertAllDownToGroup(undoGroup);
				// Event.current.Use();
			}
		}
	}

	private void OnHeaderGUI()
	{
		EditorGUILayout.BeginHorizontal(styles.gsToolbar);
		Rect rectSearchField = GUILayoutUtility.GetRect(GUIContent.none, styles.gsToolbarSearchField, GUILayout.ExpandWidth(true));
		Rect rectFillButton = GUILayoutUtility.GetRect(styles.gcLabelFill, styles.gsToolbarButton, GUILayout.Width(64f));
		Rect rectSettingsButton = GUILayoutUtility.GetRect(Styles.toolbarFixedHeight, Styles.toolbarFixedHeight, GUILayout.ExpandWidth(false));
		EditorGUILayout.EndHorizontal();

		EditorGUI.BeginChangeCheck();
		searchString = searchField.OnToolbarGUI(rectSearchField, searchString);
		if(EditorGUI.EndChangeCheck())
		{
			RunFilter();
		}

		if(focusSearchField)
		{
			searchField.SetFocus();
			focusSearchField = false;
		}

		EditorGUI.BeginChangeCheck();
		fill = EditorGUI.Toggle(rectFillButton, fill, styles.gsToolbarButton);
		if(EditorGUI.EndChangeCheck())
		{
			styles.gsIconImage.font = fill ? fontRef.filled : fontRef.standard;
			if(selected != null)
				Select(selected, false);
		}
		EditorGUI.LabelField(rectFillButton, styles.gcLabelFill, styles.gsToolbarLabel);

		if(GUI.Button(rectSettingsButton, GUIContent.none, styles.gsToolbarButton))
		{
			GUIUtility.keyboardControl = 0;
			GenericMenu menu = new GenericMenu();
			menu.AddItem(styles.gcMenuSort0, sortId == 0, ChangeSort, 0);
			menu.AddItem(styles.gcMenuSort1, sortId == 1, ChangeSort, 1);
			menu.AddItem(styles.gcMenuSort2, sortId == 2, ChangeSort, 2);
			menu.AddSeparator(string.Empty);
			menu.AddItem(styles.gcMenuShowNames, showNames, ToggleShowNames);
			menu.AddItem(styles.gcMenuKeepView, allowKeepActiveInView, ToggleKeepView);
			menu.AddItem(styles.gcMenuFocusSearch, allowFocusSearchField, ToggleFocusSearch);
			menu.AddItem(styles.gcMenuEnableRegex, enableRegexSearch, ToggleRegexSearch);
			menu.AddSeparator(string.Empty);
			menu.AddItem(styles.gcMenuAbout, false, OpenAbout);
			menu.DropDown(rectSettingsButton);
		}

		rectSettingsButton.xMin += Styles.optionsMenuLeftOffset;
		rectSettingsButton.yMin += Styles.optionsMenuTopOffset;
		EditorGUI.LabelField(rectSettingsButton, styles.gcLabelOptions, styles.gsToolbarOptions);
	}

	private void OnBodyGUI()
	{
		Rect iconRect = new Rect(0f, 0f, Styles.iconSize + Styles.labelHeight, Styles.iconSize);
		Rect labelRect = new Rect(0f, 0f, iconRect.width, Styles.labelHeight);
		Rect buttonRect = new Rect(0f, 0f, iconRect.width + Styles.spacing, iconRect.height + labelRect.height + Styles.spacing);

		if(!showNames)
		{
			iconRect.width -= Styles.labelHeight;
			buttonRect.width -= Styles.labelHeight;
			buttonRect.height -= Styles.labelHeight;
			labelRect.height = 0f;
		}

		columns = Mathf.FloorToInt((base.position.width - Styles.verticalScrollbarFixedWidth) / (iconRect.width + Styles.spacing));
		rows = Mathf.CeilToInt(filteredCollection.Length / (float) columns);

		Rect groupRect = new Rect(0f, Styles.toolbarFixedHeight, base.position.width, base.position.height - Styles.toolbarFixedHeight);

		GUI.BeginGroup(groupRect);

		Rect scrollRect = new Rect(0f, 0f, groupRect.width, groupRect.height);
		Rect viewRect = new Rect(0f, 0f, scrollRect.width - Styles.verticalScrollbarFixedWidth, rows * (iconRect.height + labelRect.height + Styles.spacing));

		scrollPos = GUI.BeginScrollView(scrollRect, scrollPos, viewRect);

		CodepointData data;
		bool visible ,hover ,active, focus;

		focus = !searchField.HasFocus();

		for(int i = 0; i < filteredCollection.Length; i++)
		{
			data = filteredCollection[i];

			iconRect.x = ((i % columns) * (iconRect.width + Styles.spacing)) + (Styles.spacing * 0.5f);
			iconRect.y = ((i / columns) * (iconRect.height + labelRect.height + Styles.spacing)) + (Styles.spacing * 0.5f);

			labelRect.x = iconRect.x;
			labelRect.y = iconRect.y + iconRect.height;

			buttonRect.x = iconRect.x - (Styles.spacing * 0.5f);
			buttonRect.y = iconRect.y - (Styles.spacing * 0.5f);

			active = (data == selected);
			visible = (buttonRect.yMax > scrollPos.y) && (buttonRect.yMin < scrollPos.y + scrollRect.height);

			if(active && keepActiveInView)
			{
				if(buttonRect.yMax > scrollPos.y + scrollRect.height)
					scrollPos.y = buttonRect.yMax - scrollRect.height;
				else if(buttonRect.yMin < scrollPos.y)
					scrollPos.y = buttonRect.yMin;

				keepActiveInView = false;
				base.Repaint();
			}

			if(!visible)
				continue;

			hover = buttonRect.Contains(Event.current.mousePosition);

			if(Event.current.type == EventType.Repaint)
			{
				#if UNITY_2022_1_OR_NEWER
				styles.gsIconSelection.Draw(buttonRect, false, active, hover || active, active && focus);
				#else
				styles.gsIconSelection.Draw(buttonRect, false, active, active, active && focus);
				#endif

				styles.gsIconImage.Draw(iconRect, data.gcCode, false, false, active, active && focus);
				if(showNames)
					styles.gsIconLabel.Draw(labelRect, data.gcLabel, false, false, active, active && focus);
			}

			GUI.Label(showNames ? labelRect : iconRect, data.gcTooltip, GUIStyle.none);

			if(hover && (Event.current.type == EventType.MouseDown))
			{
				Select(data);
				if(Event.current.clickCount == 2)
				{
					EditorApplication.delayCall += base.Close;
					GUIUtility.ExitGUI();
				}
				Event.current.Use();
			}
		}

		GUI.EndScrollView();
		GUI.EndGroup();
	}

	private void ToggleShowNames()
	{
		EditorPrefs.SetBool(showNamesEPK, showNames = !showNames);
		keepActiveInView = allowKeepActiveInView;
	}

	private void ToggleFocusSearch()
	{
		EditorPrefs.SetBool(focusSearchFieldEPK, allowFocusSearchField = !allowFocusSearchField);
	}

	private void ToggleRegexSearch()
	{
		EditorPrefs.SetBool(enableRegexSearchEPK, enableRegexSearch = !enableRegexSearch);
		RunFilter();
	}

	private void ToggleKeepView()
	{
		EditorPrefs.SetBool(keepActiveInViewEPK, allowKeepActiveInView = !allowKeepActiveInView);
	}

	private void OpenAbout()
	{
		Application.OpenURL("https://github.com/convalise/unity-material-symbols");
	}

	private void ChangeSort(object i)
	{
		EditorPrefs.SetInt(sortIdEPK, sortId = (int) i);
		RunSorter();
	}

	private void RunSorter()
	{
		System.Array.Sort(codepointsCollection, (data, other) => {
			switch(sortId)
			{
				case 2: return data.hex.CompareTo(other.hex);
				case 1: return data.name.CompareTo(other.name);
				default: return data.index.CompareTo(other.index);
			}
		});

		RunFilter();
	}

	private void RunFilter()
	{
		if(string.IsNullOrEmpty(searchString))
		{
			filteredCollection = codepointsCollection;
		}
		else
		{
			filteredCollection = codepointsCollection.Where(DoesItemMatchFilter).ToArray();
		}

		keepActiveInView = allowKeepActiveInView;
		scrollPos.y = 0f;
		base.Repaint();
	}

	private bool DoesItemMatchFilter(CodepointData data)
	{
		if(enableRegexSearch)
		{
			try
			{
				return Regex.IsMatch(data.label, searchString, RegexOptions.IgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		return data.label.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private void SelectRelative(int delta)
	{
		if(searchField.HasFocus())
		{
			GUIUtility.keyboardControl = 0;
			delta = 0;
		}
		SelectAbsolute(System.Array.IndexOf(filteredCollection, selected) + delta);
	}

	private void SelectAbsolute(int index)
	{
		index = Mathf.Clamp(index, 0, filteredCollection.Length - 1);
		Select(filteredCollection[index]);
	}

	private void Select(CodepointData data, bool keep = true)
	{
		GUIUtility.keyboardControl = 0;
		selected = data;
		OnSelectionChanged.Invoke(data.code, fill);
		keepActiveInView = keep;
		base.Repaint();
	}

	[System.Serializable]
	public class CodepointData
	{
		public string name { get; private set; }
		public string hex { get; private set; }
		public int index { get; private set; }

		public string label { get; private set; }
		public char code { get; private set; }

		public GUIContent gcLabel { get; private set; }
		public GUIContent gcCode { get; private set; }
		public GUIContent gcTooltip { get; private set; }

		public CodepointData(string codepoint, int index)
		{
			string[] data = codepoint.Split(' ');
			Init(data[0], data[1], index);
		}

		public CodepointData(string name, string hex, int index)
		{
			Init(name, hex, index);
		}

		private void Init(string name, string hex, int index)
		{
			this.name = name;
			this.hex = hex;
			this.index = index;

			this.label = string.Format("{0} ({1})", this.name.ToLowerInvariant().Replace('_', ' '), this.hex);
			this.code = MaterialSymbol.ConvertHexToChar(this.hex);

			this.gcLabel = new GUIContent(this.label);
			this.gcCode = new GUIContent(this.code.ToString());
			this.gcTooltip = new GUIContent(string.Empty, this.label);
		}
	}

	private class Styles
	{
		public const int iconSize = 56;
		public const int labelHeight = 26;
		public const int spacing = 9;

		#if UNITY_2019_3_OR_NEWER
		public const int optionsMenuLeftOffset = 2;
		public const int optionsMenuTopOffset = 2;
		#else
		public const int optionsMenuLeftOffset = 1;
		public const int optionsMenuTopOffset = 5;
		#endif

		public static float verticalScrollbarFixedWidth { get { return GUI.skin.verticalScrollbar.fixedWidth; } }
		public static float toolbarFixedHeight { get { return EditorStyles.toolbar.fixedHeight; } }

		public GUIContent gcLabelFill { get; private set; }
		public GUIContent gcLabelOptions { get; private set; }

		public GUIContent gcMenuSort0 { get; private set; }
		public GUIContent gcMenuSort1 { get; private set; }
		public GUIContent gcMenuSort2 { get; private set; }
		public GUIContent gcMenuShowNames { get; private set; }
		public GUIContent gcMenuFocusSearch { get; private set; }
		public GUIContent gcMenuEnableRegex { get; private set; }
		public GUIContent gcMenuKeepView { get; private set; }
		public GUIContent gcMenuAbout { get; private set; }

		public GUIStyle gsToolbar { get; private set; }
		public GUIStyle gsToolbarButton { get; private set; }
		public GUIStyle gsToolbarSearchField { get; private set; }
		public GUIStyle gsToolbarLabel { get; private set; }
		public GUIStyle gsToolbarOptions { get; private set; }

		public GUIStyle gsIconSelection { get; private set; }
		public GUIStyle gsIconImage { get; private set; }
		public GUIStyle gsIconLabel { get; private set; }

		public Styles()
		{
			this.gcLabelFill = new GUIContent("Fill", "Switch between filled and standard styles.");
			this.gcLabelOptions = new GUIContent(string.Empty, "Options");

			this.gsToolbar = new GUIStyle("Toolbar");
			this.gsToolbarButton = new GUIStyle("ToolbarButton");

			this.gcMenuSort0 = new GUIContent("Sort by Font Index");
			this.gcMenuSort1 = new GUIContent("Sort by Name");
			this.gcMenuSort2 = new GUIContent("Sort by Code");
			this.gcMenuShowNames = new GUIContent("Show Labels");
			this.gcMenuFocusSearch = new GUIContent("Focus Search Field on Open");
			this.gcMenuEnableRegex = new GUIContent("Search Using Regular Expression");
			this.gcMenuKeepView = new GUIContent("Keep Selection in View");
			this.gcMenuAbout = new GUIContent("About...");

			this.gsToolbarSearchField = new GUIStyle("TextField");

			this.gsToolbarLabel = new GUIStyle("ControlLabel");
			this.gsToolbarLabel.alignment = TextAnchor.MiddleCenter;
			this.gsToolbarLabel.padding = new RectOffset(0, 0, 0, 0);
			this.gsToolbarLabel.margin = new RectOffset(0, 0, 0, 0);

			this.gsToolbarOptions = new GUIStyle("PaneOptions");

			this.gsIconSelection = new GUIStyle("PR Label");
			this.gsIconSelection.fixedHeight = 0;
			this.gsIconSelection.padding = new RectOffset(0, 0, 0, 0);
			this.gsIconSelection.margin = new RectOffset(0, 0, 0, 0);

			this.gsIconLabel = new GUIStyle("ControlLabel");
			this.gsIconLabel.fontSize = 10;
			this.gsIconLabel.alignment = TextAnchor.UpperCenter;
			this.gsIconLabel.wordWrap = true;
			this.gsIconLabel.padding = new RectOffset(0, 0, 0, 0);
			this.gsIconLabel.margin = new RectOffset(0, 0, 0, 0);

			this.gsIconImage = new GUIStyle("ControlLabel");
			this.gsIconImage.font = null;
			this.gsIconImage.fontSize = 38;
			this.gsIconImage.alignment = TextAnchor.MiddleCenter;
			this.gsIconImage.padding = new RectOffset(0, 0, 0, 0);
			this.gsIconImage.margin = new RectOffset(0, 0, 0, 0);

			#if !UNITY_2019_3_OR_NEWER
			this.gsIconLabel.onFocused.textColor = Color.white;
			this.gsIconImage.onFocused.textColor = Color.white;
			#endif
		}
	}
}

}
