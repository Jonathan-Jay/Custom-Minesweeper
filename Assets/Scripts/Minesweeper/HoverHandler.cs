using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverHandler : MonoBehaviour,
		IPointerDownHandler, IPointerUpHandler,
		IBeginDragHandler, IDragHandler
{
	public FlagMenu flagMenu;
	public MSMover mover;
	[SerializeField] CanvasScaler scaler;
	public Action<Vector2Int> callbackL;
	public Action<Vector2Int> callbackR;
	public Action<Vector2Int> callbackM;

	[NonSerialized] public Tile held;
	public short button {get; private set;} = -1;
	public Tile lastHeld {get; private set;}
	public bool noClicks {get; private set;} = false;
	public void SetNoClicks(bool value)	{	noClicks = value;	ClearButtons();	}
	bool holdLMBAllowed = false;
	public void SetHoldAllowed(bool val) => holdLMBAllowed = val;
	Vector2 startPos = Vector2.negativeInfinity;
	Coroutine tryingHold;

	void Start()
	{
		flagMenu.gameObject.SetActive(false);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (noClicks || (button == 101 && eventData.button != PointerEventData.InputButton.Left) || startPos.x > -1000f || tryingHold != null)	return;

		lastHeld = held;

		if (eventData.button == PointerEventData.InputButton.Left && (button == 1 || button == 101 || button == 420))
		{
			//quick reveal number
			button = 420;
			held?.QuickBreak();
			return;
		}
		button = (short)eventData.button;

		if (eventData.button == PointerEventData.InputButton.Middle)
			tryingHold = StartCoroutine(FlagMenuHold(eventData.position));
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (noClicks)	return;

		if (tryingHold != null || startPos.x > -1000f)
		{
			if (eventData.button == PointerEventData.InputButton.Middle)
			{
				if (startPos.x > -1000f)
				{
					if (startPos != eventData.position)
					{
						flagMenu.ConfirmAngle(GetIndex(eventData.position));
						CloseFlagMenu();
						button = -1;
						return;
					}
					CloseFlagMenu();
				}
				ClearTrying();
			}
			else
				return;
		}
		
		if ((short)eventData.button == button)
		{
			if (lastHeld == held)
				held?.ProcessInput();
			button = -1;
		}
		else if (button < 100 || eventData.button == PointerEventData.InputButton.Right)
			button = -1;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (noClicks || button > 100 || startPos.x > -1000f) return;

		switch (eventData.button)
		{
			case PointerEventData.InputButton.Left:
				lastHeld = null;
				break;
			case PointerEventData.InputButton.Middle:
				ClearTrying();
				OpenFlagMenu(eventData.position);
				break;
			case PointerEventData.InputButton.Right:
				button = 101;
				break;
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (noClicks)
		{
			if (!mover.deactivated && eventData.button == PointerEventData.InputButton.Right)
				mover.Move(GetScaledPosition(eventData.delta));
			return;
		}

		switch (button)
		{
			case 420:
				if (holdLMBAllowed && eventData.button == PointerEventData.InputButton.Left)
					held?.QuickBreak();
				break;
			case 101:
				mover.Move(GetScaledPosition(eventData.delta));
				break;
			case (short)PointerEventData.InputButton.Left:
				if (holdLMBAllowed)
				{
					if (lastHeld != held)
						held?.ProcessInput();
					lastHeld = held;
				}
				break;
			case (short)PointerEventData.InputButton.Middle:
				flagMenu.UpdateAngle(GetIndex(eventData.position));
				break;
		}

	}

	public void ClearButtons()
	{
		lastHeld = null;
		held = null;
		button = -1;
		ClearTrying();
		CloseFlagMenu();
	}

	IEnumerator FlagMenuHold(Vector2 position)
	{
		yield return new WaitForSecondsRealtime(0.5f);
		if (startPos.x < -1000f)
			OpenFlagMenu(position);
		
		tryingHold = null;
	}

	void ClearTrying()
	{
		if (tryingHold != null)
			StopCoroutine(tryingHold);
		tryingHold = null;
	}

	void OpenFlagMenu(Vector2 position)
	{
		startPos = position;
		flagMenu.gameObject.SetActive(true);
		flagMenu.SetPos(startPos * (scaler.referenceResolution.y / Screen.height));
	}

	void CloseFlagMenu()
	{
		flagMenu.gameObject.SetActive(false);
		startPos = Vector2.negativeInfinity;
	}

	float GetIndex(Vector2 pos)
	{
		if (startPos == Vector2.negativeInfinity)	return 0f;

		return 0.5f - Vector2.SignedAngle(Vector2.down, pos - startPos) / 360f;
	}

	Vector2 GetScaledPosition(Vector2 pos)
	{
		return pos * (scaler.referenceResolution.y / Screen.height / mover.GetZoomFactor());
	}
}
