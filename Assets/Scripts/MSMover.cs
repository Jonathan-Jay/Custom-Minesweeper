using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MSMover : MonoBehaviour
{
	[SerializeField] public RectTransform zoomRect;
	[SerializeField] float zoomSpeed = 1f;
	[SerializeField] Vector2 zoomBounds = new Vector2(0.5f, 5f);
	[SerializeField] RectTransform moveRect;
	[SerializeField] float moveSpeed = 150f;
	public Vector2 movementBounds = Vector2.zero;

	public bool _deactivated;
	public bool deactivated {get => _deactivated; set
		{
			_deactivated = value;
			enabled = false;
		}
	}
	public float zoom {get; private set;}
	Vector2 direction;
	//RectTransform rect;

	void Awake()
	{
		//rect = GetComponent<RectTransform>();
		deactivated = false;
		ResetCamera();
	}

	public void Move(InputAction.CallbackContext context)
	{
		if (deactivated)
			return;
		direction = -context.ReadValue<Vector2>();
		enabled = direction != Vector2.zero;
	}

	public void Zoom(InputAction.CallbackContext context)
	{
		if (deactivated)
			return;
		zoom = Mathf.Clamp(zoom + 0.1f * context.ReadValue<float>() * zoomSpeed, 0f, 1f);
		zoomRect.localScale = Vector3.one * Mathf.Lerp(zoomBounds.x, zoomBounds.y, zoom * zoom);
	}

	public void ResetCamera(InputAction.CallbackContext context)
	{
		if (deactivated || context.phase != InputActionPhase.Started)
			return;
		ResetCamera();
	}
	
	public void ResetCamera()
	{
		zoom = Mathf.Sqrt(Mathf.InverseLerp(zoomBounds.x, zoomBounds.y, 1f));
		zoomRect.localScale = Vector2.one;
		moveRect.anchoredPosition = Vector2.zero;
	}

	public void Move(Vector2 amount)
	{
		moveRect.anchoredPosition = new Vector2(
			Mathf.Clamp(moveRect.anchoredPosition.x + amount.x, -movementBounds.x, movementBounds.x),
			Mathf.Clamp(moveRect.anchoredPosition.y + amount.y, -movementBounds.y, movementBounds.y)
		);
	}

	public float GetZoomFactor()
	{
		return zoomRect.localScale.x;
	}

	void Update()
	{
		Move(direction * (moveSpeed / (zoom * 0.9f + 0.1f) * Time.deltaTime));
	}
}
