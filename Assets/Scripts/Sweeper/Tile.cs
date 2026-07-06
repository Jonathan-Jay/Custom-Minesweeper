using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IPointerEnterHandler
{
	public Vector2Int pos;
	public Action<Vector2Int> callbackL;
	public Action<Vector2Int> callbackR;
	public Action<Vector2Int> callbackM;
	public Hint hint;
	public MSVisuals visuals;

	[SerializeField] Image flag;
	[SerializeField] Image tile;
	[SerializeField] TMPro.TMP_Text text;
	
	public bool hasValue = false;
	short hoveredOver = -1;
	RectTransform rect;

	void Awake()
	{
		rect = GetComponent<RectTransform>();
	}
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		//if (eventData == null && visuals.noHold)	return;

		visuals.hover.held = this;

		if (visuals.hover.button == hoveredOver) return;

		switch (visuals.hover.button)
		{
			case (short)PointerEventData.InputButton.Left:
				callbackL?.Invoke(pos);
				break;
			case (short)PointerEventData.InputButton.Right:
				callbackR?.Invoke(pos);
				break;
			case (short)PointerEventData.InputButton.Middle:
				callbackM?.Invoke(pos);
				break;
		}
		//if (visuals.noHold && hoveredOver == -1)
		if (hoveredOver == -1)
			visuals.hover.liftMouse += UndoBinding;

		hoveredOver = visuals.hover.button;
	}

	public void UndoBinding()
	{
		hoveredOver = -1;
		visuals.hover.liftMouse -= UndoBinding;
	}

	public void Hide()
	{
		flag.gameObject.SetActive(false);
		tile.gameObject.SetActive(true);
		rect.sizeDelta = visuals.tileSize;
		if ((hasValue || (!visuals.NoHintsOverBombs && hint.flagValue != 0)) && visuals.useFlags)
			hint.valueChanged -= UpdateText;
		
		text.text = "";
		hasValue = false;
	}

	public void Reveal()
	{
		if (!hasValue)
		{
			tile.gameObject.SetActive(false);
			rect.sizeDelta = Vector2.zero;
		}

		if (hint.bomb != 0 || hint.flagValue != 0)	return;

		if (visuals.useFlags)
			hint.valueChanged += UpdateText;
		else
			hint.valueChanged -= UpdateText;

		SetText(visuals.useFlags ? hint.displayValue : hint.actualValue, hint.actualValue == 0);
		hasValue = true;
	}

	public void FlagText(bool remove = false)
	{
		if (visuals.NoHintsOverBombs)
		{
			if (visuals.useFlags)
				hint.valueChanged -= UpdateText;

			text.text = "";
			return;
		}
		
		if (visuals.useFlags)
			hint.valueChanged += UpdateText;
		else
			hint.valueChanged -= UpdateText;
		
		SetText(visuals.useFlags ? hint.displayValue : hint.actualValue, false);

	}

	void UpdateText()
	{
		SetText(hint.displayValue, hint.actualValue == 0 && hint.displayValue == 0);
	}

	void SetText(int val, bool empty)
	{
		if (empty)
		{
			text.text = "";
			return;
		}
		text.color = visuals.hintColours[Mathf.Min(Mathf.Abs(val), visuals.hintColours.Length - 1)];
		text.text = val.ToString();
	}

	public void SetFlag(Sprite flagImg)
	{
		if (flagImg)
		{
			if (!visuals.NoHintsOverBombs && !flag.gameObject.activeInHierarchy)
				FlagText();

			flag.gameObject.SetActive(true);
		}
		else
		{
			if (!visuals.NoHintsOverBombs)
			{
				if (visuals.useFlags)
					hint.valueChanged -= UpdateText;
				text.text = "";
			}
			flag.gameObject.SetActive(false);
		}
		
		flag.sprite = flagImg;
	}
}
