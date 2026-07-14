using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MSCustomizer : MonoBehaviour
{
	public MSVisuals visuals;
	public MSMover mover;

	[SerializeField] TMPro.TMP_InputField initialRadiusField;
	[SerializeField] TMPro.TMP_InputField maxHealthField;
	[SerializeField] TMPro.TMP_InputField seedText;

	Vector2Int tempSize = Vector2Int.zero;

	void Awake()
	{
		visuals.seedUpdated += (int val) => seedText.text = val.ToString();
	}

	void Start()
	{
		// Lots of stupid code just to not require changing script execution order
		Image img = GetComponent<Image>();
		img.enabled = false;

		for (int child = 0; child < transform.childCount; ++child)
			transform.GetChild(child).gameObject.SetActive(false);

		IEnumerator DelayedStart() {
			yield return null;
			mover.movementBounds = visuals.boardParent.sizeDelta * 0.5f - Vector2.one * 50f;
			
			img.enabled = true;
			gameObject.SetActive(false);

			for (int child = 0; child < transform.childCount; ++child)
				transform.GetChild(child).gameObject.SetActive(true);

			// Default values
			initialRadiusField.text = visuals.game.initialIslandRadius.ToString();
			maxHealthField.text = visuals.maxMistakes.ToString();
		}

		StartCoroutine(DelayedStart());
	}

	public void Toggle()
	{
		if (!gameObject.activeInHierarchy)
		{
			gameObject.SetActive(true);
			mover.deactivated = true;
			visuals.hover.enabled = false;
			tempSize = visuals.game.size;
			return;
		}
		
		visuals.ValidateChanges(tempSize);

		gameObject.SetActive(false);
		mover.deactivated = false;
		visuals.hover.enabled = true;
	}

	public void SetInitialRadius(string value)
	{
		if (int.TryParse(value, out int res))
			visuals.game.SetRadius(res);
		
		initialRadiusField.text = visuals.game.initialIslandRadius.ToString();
	}

	public void SetMaxHealth(string value)
	{
		if (int.TryParse(value, out int res))
			visuals.SetMaxMistakes(res);
		
		maxHealthField.text = visuals.maxMistakes.ToString();
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
}
