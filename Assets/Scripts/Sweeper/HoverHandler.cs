using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
	public static Tile held;
	public static short button = -1;
	public static event Action liftMouse;

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
