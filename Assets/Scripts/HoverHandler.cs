using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IDragHandler
{
	[SerializeField] CanvasScaler scaler;
	[SerializeField] MSMover mover;
	[NonSerialized] public Tile held;
	[NonSerialized] public short button = -1;
	[NonSerialized] public Tile lastHeld;
	public Action<Vector2Int> callbackL;
	public Action<Vector2Int> callbackR;
	public Action<Vector2Int> callbackM;

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
		else if (button == 420)
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
		if (button != 1)	return;

		mover.Move(eventData.delta * (scaler.scaleFactor / mover.GetZoomFactor()));
	}
}
