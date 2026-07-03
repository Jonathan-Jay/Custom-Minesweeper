using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IPointerClickHandler
{
	public Vector2Int pos;
	public event Action<Vector2Int> callbackL;
	public event Action<Vector2Int> callbackR;
	[SerializeField] Image flag;
	[SerializeField] Image tile;
	[SerializeField] TMPro.TMP_Text text;
	public Hint hint;

	public bool hasValue = false;

	public void Hide(bool useFlags)
	{
		flag.gameObject.SetActive(false);
		tile.gameObject.SetActive(true);
		tile.color = Color.white;

		if (hasValue && useFlags)
			hint.valueChanged -= UpdateText;
		
		text.text = "";
		hasValue = false;
	}

	public void Reveal(bool useFlags)
	{
		tile.gameObject.SetActive(false);

		if (hint.flagValue < 0)	return;

		int hintVal = useFlags ? hint.displayValue : hint.actualValue;

		// if already revealed, check what we need to update
		if (hasValue)
		{
			if (useFlags)
				hint.valueChanged += UpdateText;
			else
				hint.valueChanged -= UpdateText;
			
			if ((hintVal == 0 && text.text == "") || hintVal.ToString() == text.text)	return;
		}

		string hintText = hint.actualValue < 0 ? "" : hintVal.ToString();

		if (useFlags)
		{
			if (!hasValue)
				hint.valueChanged += UpdateText;
			
			if (hint.actualValue == 0)
				hintText = "";
		}
		else
		{
			if (hintVal == 0)
				hintText = "";
		}

		text.text = hintText;
		hasValue = true;
	}

	void UpdateText()
	{
		string hintText = hint.actualValue < 0 ? "X" : hint.displayValue.ToString();
		if (hint.actualValue == 0 && hint.displayValue == 0)
			hintText = "";
		text.text = hintText;
	}

	public void SetFlag(Sprite flagImg)
	{
		if (flagImg)
			flag.gameObject.SetActive(true);
		else
			flag.gameObject.SetActive(false);
			
		flag.sprite = flagImg;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		switch (eventData.button)
		{
			case PointerEventData.InputButton.Left:
				callbackL?.Invoke(pos);
				break;
			case PointerEventData.InputButton.Right:
				callbackR?.Invoke(pos);
				break;
		}
	}
}
