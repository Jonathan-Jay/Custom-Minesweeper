using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IPointerClickHandler
{
	public Vector2Int pos;
	public event Action<Vector2Int> callbackL;
	public event Action<Vector2Int> callbackR;
	[SerializeField] Sprite tileImg;
	[SerializeField] Image sprite;
	[SerializeField] TMPro.TMP_Text text;
	public Hint hint;

	public bool hasValue = false;

	public void Hide(bool useFlags)
	{
		sprite.gameObject.SetActive(true);
		sprite.sprite = tileImg;

		if (hasValue && useFlags)
			hint.valueChanged -= UpdateText;
		
		text.text = "";
		hasValue = false;
	}

	public void Reveal(bool useFlags)
	{
		sprite.gameObject.SetActive(false);

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

		string hintText = hint.actualValue < 0 ? "X" : hintVal.ToString();

		if (useFlags)
		{
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
		if (hasValue)
		{
			if (flagImg)
				sprite.gameObject.SetActive(true);
			else
				sprite.gameObject.SetActive(false);
		}
			
		sprite.sprite = flagImg ? flagImg : tileImg;
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
