using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MSCustomizer : MonoBehaviour
{
	public MSVisuals visuals;
	public MSMover mover;

	void Start()
	{
		// Lots of stupid code just to not require changing script execution order
		Image img = GetComponent<Image>();
		img.enabled = false;

		for (int child = 0; child < transform.childCount; ++child)
			transform.GetChild(0).gameObject.SetActive(false);

		IEnumerator DelayedStart() {
			yield return null;
			mover.movementBounds = visuals.boardParent.sizeDelta * 0.5f - Vector2.one * 50f;
			
			img.enabled = true;
			gameObject.SetActive(false);

			img.enabled = true;

			for (int child = 0; child < transform.childCount; ++child)
				transform.GetChild(0).gameObject.SetActive(true);
		}

		StartCoroutine(DelayedStart());
	}

	public void Toggle()
	{
		if (!gameObject.activeInHierarchy)
		{
			gameObject.SetActive(true);
			mover.deactivated = true;
			return;
		}
		//ConfirmChanges();

		gameObject.SetActive(false);
		mover.deactivated = false;
	}
}
