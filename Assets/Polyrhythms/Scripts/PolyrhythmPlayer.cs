using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PolyrhythmPlayer : MonoBehaviour
{
	private KMAudio audio;
	private PolyrhythmAnimator animator;
	private Coroutine[] coroutines = new Coroutine[2];
	
	void Start ()
	{
		audio = GetComponent<KMAudio>();
		animator = GetComponent<PolyrhythmAnimator>();
	}

	IEnumerator KeepPlaying()
	{
		yield return null;

		var possibleNumbers = Enumerable.Range(2, 8);
		while (true)
		{
			var first = possibleNumbers.PickRandom();
			var second = possibleNumbers.Except(new[] {first}).PickRandom();
			PlayPolyrhythm(first, second, 4f);
			yield return new WaitForSeconds(6f);
		}
	}

	public void StopPlaying()
	{
		foreach (var c in coroutines)
		{
			if (c != null)
				StopCoroutine(c);
		}
	}
	
	public void PlayPolyrhythm(int first, int second, float time)
	{
		StopPlaying();

		coroutines = new[]
		{
			StartCoroutine(PlayRhythm(-first, time)),
			StartCoroutine(PlayRhythm(second, time))
		};
	}

	IEnumerator PlayRhythm(int number, float time)
	{
		var sound = number < 0 ? "lower" : "higher";
		number *= number < 0 ? -1 : 1;
		var targetTimes = Enumerable.Range(1, number).Select(n => time * n / number).ToList();
		
		var currentTime = 0f;
		
		// Always play the first sound at time == 0
		audio.PlaySoundAtTransform(sound, transform);
		animator.Pulse(.2f, (int) Symbols.PlayFilled);
		
		// Ignore the last time, to make it easier for defuser.
		while (targetTimes.Count() > 1)
		{
			currentTime += Time.deltaTime;
			if (currentTime > targetTimes.First())
			{
				audio.PlaySoundAtTransform(sound, transform);
				animator.Pulse(.2f, (int) Symbols.PlayFilled);
				targetTimes.RemoveAt(0);
			}
			yield return null;
		}
	}
}
