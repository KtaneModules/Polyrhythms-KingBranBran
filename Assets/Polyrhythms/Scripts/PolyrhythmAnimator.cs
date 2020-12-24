using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyrhythmAnimator : MonoBehaviour
{
	public Sprite[] sprites;
	public GameObject parent;

	private Coroutine pulseCoroutine;
	private GameObject pulseObj;

	public void Pulse(float time, int sprite, Color? color = null)
	{
		if (pulseCoroutine != null)
		{
			StopCoroutine(pulseCoroutine);
			Destroy(pulseObj);
		}

		pulseObj = Instantiate(transform.Find("Button/Symbol").gameObject, parent.transform);
		color = color ?? Color.white;
		
		Vector3 original = pulseObj.transform.localPosition;
		original.y += .001f;
		pulseObj.transform.localPosition = original;

		pulseObj.GetComponent<SpriteRenderer>().sprite = sprites[sprite];

		pulseCoroutine = StartCoroutine(DoPulse(time, color.Value));
	}

	IEnumerator DoPulse(float time, Color color)
	{
		var originalTime = time;
		var spriteRenderer = pulseObj.GetComponent<SpriteRenderer>();
		
		while (time > 0f)
		{
			time -= Time.deltaTime;
			color.a = time / originalTime;
			spriteRenderer.color = color;
			yield return null;
		}
		
		// Destroy(pulseObj);
	}
}
