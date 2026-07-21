using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MSCustomizer : MonoBehaviour
{
	public MSVisuals visuals;

	public Color regularInputField = Color.white;
	public Color emptyInputField = Color.grey;
	public Color positiveInputField = Color.green;
	public Color negativeInputField = Color.red;
	
	[SerializeField] RectTransform bombListContent;
	[SerializeField] BombCategoryItem groupParent;
	[SerializeField] BombListItem baseItem;
	[SerializeField] TMPro.TMP_InputField deathAnimDurationField;
	[SerializeField] TMPro.TMP_InputField initialRadiusField;
	[SerializeField] TMPro.TMP_InputField maxHealthField;
	[SerializeField] TMPro.TMP_InputField seedText;
	[SerializeField] TMPro.TMP_InputField sizexText;
	[SerializeField] TMPro.TMP_InputField sizeyText;

	[NonSerialized] public Vector2Int tempSize = Vector2Int.zero;
	bool forceUpdate = false;

	public void ForceUpdate()
	{
		forceUpdate = true;
		foreach (BombCategory bombCategory in visuals.game.bombCategories)
			foreach (BombPair bombPair in bombCategory.bombOptions)
				bombPair.count = bombPair.realCount / bombPair.bomb.GetBombCount() * bombPair.bomb.GetBombCount();
	}

	void Awake()
	{
		visuals.seedUpdated += (int val) => seedText.text = val.ToString();
	}

	void Start()
	{
		// Lots of stupid code just to not require changing script execution order
		Image img = GetComponent<Image>();
		img.enabled = false;

		UpdateBombList();

		for (int child = 0; child < transform.childCount; ++child)
			transform.GetChild(child).gameObject.SetActive(false);

		IEnumerator DelayedStart() {
			yield return null;
			
			img.enabled = true;
			gameObject.SetActive(false);

			for (int child = 0; child < transform.childCount; ++child)
				transform.GetChild(child).gameObject.SetActive(true);

			// Default values
			deathAnimDurationField.text = visuals.game.deathAnimDuration.ToString("0.00") + "s";
			deathAnimDurationField.GetComponent<Image>().color = visuals.game.deathAnimDuration > 0f ? regularInputField : emptyInputField;
			SetFieldTextContextual(initialRadiusField, visuals.game.initialIslandRadius, -1, regularInputField, emptyInputField);
			SetFieldTextContextual(maxHealthField, visuals.maxMistakes, 1, regularInputField, emptyInputField);
			sizexText.text = visuals.game.size.x.ToString();
			sizeyText.text = visuals.game.size.y.ToString();
		}

		StartCoroutine(DelayedStart());
	}

	public void Toggle()
	{
		if (visuals.game.animating)	return;

		if (!gameObject.activeInHierarchy)
		{
			gameObject.SetActive(true);
			visuals.hover.mover.deactivated = true;
			visuals.hover.enabled = false;
			tempSize = visuals.game.size;
			forceUpdate = false;
			return;
		}

		visuals.ValidateChanges(tempSize, forceUpdate);

		gameObject.SetActive(false);
		visuals.hover.mover.deactivated = false;
		visuals.hover.enabled = true;
	}

	public void UpdateBombList()
	{
		// child 0 is the preset base
		while (bombListContent.childCount > 1)
		{
			Transform child = bombListContent.GetChild(1);
			child.SetParent(null);
			Destroy(child.gameObject);
		}
		
		float offset = -baseItem.GetComponent<RectTransform>().anchoredPosition.y;
		float height = baseItem.GetComponent<RectTransform>().sizeDelta.y;
		float combinedHeight = bombListContent.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
		foreach (BombCategory bombCategory in visuals.game.bombCategories)
		{
			BombCategoryItem curCategory = Instantiate(groupParent, bombListContent);
			curCategory.customizer = this;
			curCategory.nameText.text = bombCategory.name;
			curCategory.scrollContent = bombListContent;
			curCategory.contents = new BombListItem[bombCategory.Count];

			RectTransform curParent = curCategory.GetComponent<RectTransform>();
			curParent.anchoredPosition = Vector2.down * (combinedHeight + curParent.sizeDelta.y * 0.5f);

			int index = 0;
			foreach (BombPair bombPair in bombCategory.bombOptions)
			{
				BombListItem item = Instantiate(baseItem, curParent);
				item.categoryItem = curCategory;
				item.GetComponent<RectTransform>().anchoredPosition = Vector2.down * (offset * (index + 1) + height * index * 0.5f);
				item.SetData(bombPair);
				curCategory.contents[index] = item;
				++index;
			}
			
			curCategory.UpdateCount();
			if (curCategory.activeText.text == "")
				curCategory.GetComponent<Toggle>().isOn = false;
			
			curCategory.height = offset * (index + 1) + height * (index - 1) * 0.5f;
			
			combinedHeight += curParent.sizeDelta.y;
			if (curCategory.activeText.text != "")
			 	combinedHeight += curCategory.height;

		}
		bombListContent.sizeDelta = Vector2.up * combinedHeight;
	}

	public void SetXSize(string value)
	{
		if (int.TryParse(value, out int res))
			tempSize.x = Mathf.Clamp(res, 1, 200);
		
		sizexText.text = tempSize.x.ToString();
	}

	public void SetYSize(string value)
	{
		if (int.TryParse(value, out int res))
			tempSize.y = Mathf.Clamp(res, 1, 200);
		
		sizeyText.text = tempSize.y.ToString();
	}

	public void SetDeathAnimDuration(string value)
	{
		if (float.TryParse(value, out float res))
			visuals.game.deathAnimDuration = Mathf.Clamp(res, 0f, 10f);

		deathAnimDurationField.text = visuals.game.deathAnimDuration.ToString("0.00") + "s";
		deathAnimDurationField.GetComponent<Image>().color = visuals.game.deathAnimDuration > 0f ? regularInputField : emptyInputField;
	}

	public void SetInitialRadius(string value)
	{
		if (int.TryParse(value, out int res))
			visuals.game.SetRadius(res);
		
		SetFieldTextContextual(initialRadiusField, visuals.game.initialIslandRadius, -1, regularInputField, emptyInputField);
	}

	public void SetMaxHealth(string value)
	{
		if (int.TryParse(value, out int res))
			visuals.SetMaxMistakes(res);

		SetFieldTextContextual(maxHealthField, visuals.maxMistakes, 1, regularInputField, emptyInputField);
	}

	public void SetSeed(string value)
	{
		if (int.TryParse(value, out int res))
		{
			visuals.game.seed = res;
			seedText.text += " Reset Same Seed";
			return;
		}
		seedText.text = visuals.game.seed.ToString();
	}

	public void SetFieldTextContextual(TMPro.TMP_InputField field, int text, int minimum, Color regular, Color below)
	{
		field.text = text.ToString();
		field.GetComponent<Image>().color = text > minimum ? regular : below;
	}
}
