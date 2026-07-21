using UnityEngine;

public class FullScreenShortcut : MonoBehaviour
{
	public void Fullscreen()
	{
		Screen.fullScreen = !Screen.fullScreen;
	}
}
