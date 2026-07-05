using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
	[NonSerialized] public Tile held;
	[NonSerialized] public short button = -1;
	public event Action liftMouse;

	public void OnPointerDown(PointerEventData eventData)
	{
		button = (short)eventData.button;
		if (held)
			held.OnPointerEnter(eventData);
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
}
