using Unity.VisualScripting;
using UnityEngine;

public class RandomBackground : MonoBehaviour
{
	public Camera camer;
	public float speed = 10f;

	Color last;
	Color next;
	float t = 0;

	void Start()
	{
		last = camer.backgroundColor;
		next = new Color(Random.Range(0.4f, 0.6f), Random.Range(0.4f, 0.6f), Random.Range(0.4f, 0.6f));
	}

	void Update()
	{
		t += Time.deltaTime * speed;
		if (t > 1)
		{
			t -= 1;
			last = next;
			next = new Color(Random.Range(0.4f, 0.6f), Random.Range(0.4f, 0.6f), Random.Range(0.4f, 0.6f));
		}
		camer.backgroundColor = Color.Lerp(last, next, t);
	}
}
