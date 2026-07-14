using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverHandler : MonoBehaviour,
		IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
		IDragHandler, IBeginDragHandler, IEndDragHandler
{
	[SerializeField] CanvasScaler scaler;
	[SerializeField] MSMover mover;
	[NonSerialized] public Tile held;
	public short button {get; private set;} = -1;
	public Tile lastHeld {get; private set;}
	public Action<Vector2Int> callbackL;
	public Action<Vector2Int> callbackR;
	public Action<Vector2Int> callbackM;

	Vector2 startPos = Vector2.negativeInfinity;

	public void OnPointerDown(PointerEventData eventData)
	{
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
		if ((short)eventData.button == button)
		{
			if (lastHeld == held)
				held?.ProcessInput();
			button = -1;
		}
		else if (eventData.button == PointerEventData.InputButton.Right && button == 420)
			button = -1;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		held = null;
	}

	void OnDisable()
	{
		held = null;
		button = -1;
	}

	public void OnDrag(PointerEventData eventData)
	{
		switch (button)
		{
			case (short)PointerEventData.InputButton.Right:
				mover.Move(eventData.delta * (scaler.scaleFactor / mover.GetZoomFactor()));
				break;
			case (short)PointerEventData.InputButton.Middle:
				break;
		}

	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Middle)
		{
			startPos = eventData.position;
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Middle && startPos != eventData.position)
		{
			//Select the thing
			Debug.Log(GetIndex(eventData.position));

			startPos = Vector2.negativeInfinity;
		}
	}

	float GetIndex(Vector2 pos)
	{
		if (startPos == Vector2.negativeInfinity)	return -1f;

		return 180f - Vector2.SignedAngle(Vector2.down, pos - startPos);
	}
}
