using System;
using UnityEngine;

public class BombCategoryItem : MonoBehaviour
{

	public MSCustomizer customizer;
	public TMPro.TMP_Text nameText;
	public TMPro.TMP_Text activeText;
	[NonSerialized]	public BombListItem[] contents;
	[NonSerialized]	public float height = 0f;
	[NonSerialized]	public RectTransform scrollContent;

	public void Toggle(bool active)
	{
		Vector2 offset = active ? Vector2.down * height : Vector2.up * height;
		foreach (BombListItem item in contents)
			item.gameObject.SetActive(active);

		for (int i = transform.GetSiblingIndex() + 1; i < transform.parent.childCount; ++i)
			transform.parent.GetChild(i).GetComponent<RectTransform>().anchoredPosition += offset;
		
		scrollContent.sizeDelta -= offset;
	}

	public void UpdateCount()
	{
		int count = 0;
		foreach (BombListItem item in contents)
			count += item.GetCount();

		activeText.text = count == 0 ? "" : count.ToString();
	}
}
