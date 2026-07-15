using UnityEngine;
using UnityEngine.UI;

public class BombListItem : MonoBehaviour
{
	public MSCustomizer customizer;
	[SerializeField] Image bombIcon;
	[SerializeField] TMPro.TMP_Text nameText;
	[SerializeField] TMPro.TMP_InputField countInput;
	[SerializeField] TMPro.TMP_InputField damageInput;
	[SerializeField] RawImage patternBG;
	[SerializeField] TMPro.TMP_Text patternText;
	[SerializeField] char zeroCharacter = '-';

	Minesweeper.BombPair bombPair;
	public void SetData(Minesweeper.BombPair newPair)
	{
		bombPair = newPair;

		bombIcon.sprite = bombPair.bomb.sprite;
		nameText.text = bombPair.bomb.name;
		customizer.SetFieldTextContextual(countInput, bombPair.realCount, 0, customizer.regularInputField, customizer.emptyInputField);
		customizer.SetFieldTextContextual(damageInput, bombPair.bomb.damage, 0, customizer.negativeInputField, customizer.positiveInputField);
		Vector2Int hintSize = bombPair.bomb.GetHintSize();
		patternBG.uvRect = new Rect(Vector2.zero, hintSize);

		patternText.text = "<mspace=1em>";
		Vector2Int hintPos = Vector2Int.zero;
		for (hintPos.y = hintSize.y - 1; hintPos.y >= 0; --hintPos.y)
		{
			for (hintPos.x = 0; hintPos.x < hintSize.x; ++hintPos.x)
			{
				string text = bombPair.bomb.GetHintVisual(hintPos);
				if (text.Length == 1 || text[0] == '<')
					patternText.text += text == "0" ? zeroCharacter : text;
				else
				{
					patternText.text += "<mspace=" + (1f / text.Length).ToString("0.0") + "em>" + text + "<mspace=1em>";
				}
			}
			if (hintPos.y > 0)
				patternText.text += "\n";
		}
	}

	public void SetCount(string value)
	{
		if (int.TryParse(value, out int res))
		{
			if (bombPair.realCount == res)	return;

			bombPair.realCount = Mathf.Max(0, res);

			customizer.ForceUpdate();
		}
		customizer.SetFieldTextContextual(countInput, bombPair.realCount, 0, customizer.regularInputField, customizer.emptyInputField);
	}

	public void SetDamage(string value)
	{
		if (int.TryParse(value, out int res))
			bombPair.bomb.damage = res;

		customizer.SetFieldTextContextual(damageInput, bombPair.bomb.damage, 0, customizer.negativeInputField, customizer.positiveInputField);
	}
}
