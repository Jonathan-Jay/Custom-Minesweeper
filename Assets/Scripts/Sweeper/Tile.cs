using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
		HoverHandler.held = this;

		if (HoverHandler.button == hoveredOver) return;

		switch (HoverHandler.button)
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
		if (hoveredOver == -1)
			HoverHandler.liftMouse += UndoBinding;

		hoveredOver = HoverHandler.button;
	}

	public void UndoBinding()
	{
		hoveredOver = -1;
		HoverHandler.liftMouse -= UndoBinding;
	}

	public void Hide()
	{
		flag.gameObject.SetActive(false);
		tile.gameObject.SetActive(true);
		rect.sizeDelta = visuals.tileSize;

		if (hasValue && visuals.useFlags)
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

		if (hint.actualValue < 0)	return;

		if (visuals.useFlags)
			hint.valueChanged += UpdateText;
		else
			hint.valueChanged -= UpdateText;

		int hintVal =  visuals.useFlags ? hint.displayValue : hint.actualValue;
		text.text = (hintVal != 0 || (visuals.useFlags && hint.actualValue != 0)) ? hintVal.ToString() : "";
		hasValue = true;
	}

	void UpdateText()
	{
		text.text = (hint.actualValue == 0 && hint.displayValue == 0) ? "" : hint.displayValue.ToString();
	}

	public void SetFlag(Sprite flagImg)
	{
		if (flagImg)
			flag.gameObject.SetActive(true);
		else
			flag.gameObject.SetActive(false);
			
		flag.sprite = flagImg;
	}
}
