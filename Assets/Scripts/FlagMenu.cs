using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlagMenu : MonoBehaviour
{
	[SerializeField] MSVisuals visuals;
	[SerializeField] List<Image> icons;
	[SerializeField] float scaledRadius = 20f;
	[SerializeField] float minimumRadius = 75f;
	[SerializeField] float maximumRadius = 300f;
	[SerializeField] Color faded = new Color(1f, 1f, 1f, 0.5f);

	RectTransform rect;

	void Awake()
	{
		rect = GetComponent<RectTransform>();
		visuals.bombListUpdated += UpdateBombList;
	}

	public void UpdateBombList()
	{
		Image first = icons[0];
		foreach (Image icon in icons)
		{
			if (icon == first)	continue;
			Destroy(icon.gameObject);
		}
		icons.Clear();
		int count = 0;
		foreach (BombCategory bombCategory in visuals.game.bombCategories)
			count += bombCategory.bombOptions.Count;
		icons.Capacity = count;

		first.color = faded;
		icons.Add(first);

		foreach (BombCategory bombCategory in visuals.game.bombCategories)
		{
			foreach (BombPair bombPair in bombCategory.bombOptions)
			{
				if (bombPair.count == 0)
					continue;
				Image icon = Instantiate(first, transform);
				icon.name = bombPair.bomb.name;
				icon.sprite = bombPair.bomb.sprite;
				icon.color = faded;
				icons.Add(icon);
			}
		}

		Quaternion angle = Quaternion.Euler(0f, 0f, -360f / icons.Count);
		Quaternion rot = Quaternion.identity;
		foreach (Image icon in icons)
		{
			icon.GetComponent<RectTransform>().anchoredPosition = rot * (Vector2.up * Mathf.Clamp(
					scaledRadius * (icons.Count - 1), minimumRadius, maximumRadius));
			rot *= angle;
		}
	}

	public void SetPos(Vector2 pos)
	{
		rect.anchoredPosition = pos;
		UpdateAngle(0f);
	}

	public void UpdateAngle(float angle)
	{
		int selected = GetSelected(angle);
		for (int i = 0; i < icons.Count; ++i)
		{
			icons[i].color = i == selected ? Color.white : faded;
		}
	}

	public void ConfirmAngle(float angle)
	{
		visuals.SetDefaultFlag(GetSelected(angle));
	}

	public int GetSelected(float angle)
	{
		return Mathf.FloorToInt(Mathf.Repeat(angle * icons.Count + 0.5f, icons.Count));
	}
}
