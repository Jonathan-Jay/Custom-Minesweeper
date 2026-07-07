using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
	[NonSerialized] public Tile held;
	[NonSerialized] public short button = -1;
	public Action<Vector2Int> callbackL;
	public Action<Vector2Int> callbackR;
	public Action<Vector2Int> callbackM;
	public event Action liftMouse;

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && (button == 1 || button == 420))
		{
			//quick reveal number
			button = 420;
			held?.QuickBreak();
			return;
		}
		button = (short)eventData.button;
		held?.OnPointerEnter(eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if ((short)eventData.button == button)
		{
			liftMouse?.Invoke();
			button = -1;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		held = null;
	}

	void OnDisable()
	{
		liftMouse?.Invoke();
		held = null;
		button = -1;
	}
}
