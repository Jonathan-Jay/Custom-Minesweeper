using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverHandler : MonoBehaviour,
		IPointerDownHandler, IPointerUpHandler,
		IDragHandler, IBeginDragHandler, IEndDragHandler
{
	public FlagMenu flagMenu;
	[SerializeField] CanvasScaler scaler;
	[SerializeField] MSMover mover;
	[NonSerialized] public Tile held;
	public short button {get; private set;} = -1;
	public Tile lastHeld {get; private set;}
	public Action<Vector2Int> callbackL;
	public Action<Vector2Int> callbackR;
	public Action<Vector2Int> callbackM;

	Vector2 startPos = Vector2.negativeInfinity;

	void Start()
	{
		flagMenu.gameObject.SetActive(false);
	}

	void OnDisable()
	{
		held = null;
		button = -1;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (startPos.x > -1000f || button == 101)	return;

		lastHeld = held;

		if (eventData.button == PointerEventData.InputButton.Left && (button == 1 || button == 420))
		{
			//quick reveal number
			button = 420;
			held?.QuickBreak();
			return;
		}
		button = (short)eventData.button;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (startPos.x > -1000f) return;
		
		if ((short)eventData.button == button)
		{
			if (lastHeld == held)
				held?.ProcessInput();
			button = -1;
		}
		else if (button < 100 || eventData.button == PointerEventData.InputButton.Right)
			button = -1;
	}

	public void OnDrag(PointerEventData eventData)
	{
		switch (button)
		{
			case 101:
				mover.Move(GetScaledPosition(eventData.delta));
				break;
			case (short)PointerEventData.InputButton.Middle:
				flagMenu.UpdateAngle(GetIndex(eventData.position));
				break;
		}

	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (button > 100 || startPos.x > -1000f)	return;

		if (eventData.button == PointerEventData.InputButton.Middle)
		{
			startPos = eventData.position;

			flagMenu.gameObject.SetActive(true);
			flagMenu.SetPos(startPos * (scaler.referenceResolution.y / Screen.height));
		}
		else if (eventData.button == PointerEventData.InputButton.Right)
			button = 101;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Middle && startPos != eventData.position)
		{
			flagMenu.ConfirmAngle(GetIndex(eventData.position));

			flagMenu.gameObject.SetActive(false);
			startPos = Vector2.negativeInfinity;
		}
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
