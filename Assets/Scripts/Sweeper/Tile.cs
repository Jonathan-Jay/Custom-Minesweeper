using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IPointerEnterHandler
{
	public Vector2Int pos;
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
				visuals.hover.callbackL?.Invoke(pos);
				break;
			case (short)PointerEventData.InputButton.Right:
				visuals.hover.callbackR?.Invoke(pos);
				break;
			case (short)PointerEventData.InputButton.Middle:
				visuals.hover.callbackM?.Invoke(pos);
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
		if (hasValue || (!visuals.NoHintsOverBombs && hint.flagValue != 0))
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

		if (!hasValue)
			hint.valueChanged += UpdateText;

		UpdateText();
		hasValue = true;
	}

	public void FlagText(bool toggle = false)
	{
		if (visuals.NoHintsOverBombs)
		{
			hint.valueChanged -= UpdateText;

			text.text = "";
			return;
		}
		
		if (toggle)
			hint.valueChanged += UpdateText;
		
		SetText(visuals.useFlags ? hint.displayValue : hint.actualValue);

	}

	void UpdateText()
	{
		SetText(visuals.useFlags ? hint.displayValue : hint.actualValue);
	}

	void SetText(int val)
	{
		if (hint.status == Hint.TileStatus.Mystery || hint.status == Hint.TileStatus.Closed)
		{
			text.color = visuals.mysteryColour;
			text.text = "?";
			return;
		}
		
		if (hint.actualValue == 0 && !(visuals.useFlags && hint.displayValue != 0) && hint.flagValue == 0)
		{
			text.text = "";
			return;
		}
		string newText = val.ToString();
		if (text.text == newText)
			return;
		text.color = visuals.hintColours[Mathf.Min(Mathf.Abs(val), visuals.hintColours.Length - 1)];
		text.text = newText;
	}

	public void SetFlag(Sprite flagImg)
	{
		if (flagImg)
		{
			if (!visuals.NoHintsOverBombs && !flag.gameObject.activeInHierarchy)
				FlagText(true);

			flag.gameObject.SetActive(true);
		}
		else
		{
			if (!visuals.NoHintsOverBombs)
			{
				hint.valueChanged -= UpdateText;
				text.text = "";
			}
			flag.gameObject.SetActive(false);
		}
		
		flag.sprite = flagImg;
	}
}
