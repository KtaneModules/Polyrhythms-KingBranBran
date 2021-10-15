using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

public class Polyrhythms : MonoBehaviour
{
	public Sprite[] sprites;

	private KMBombModule module;
	private KMBombInfo edgework;
	private KMAudio audio;
	private KMSelectable button;
	private PolyrhythmPlayer player;
	private PolyrhythmAnimator animator;
	private SpriteRenderer spriteRenderer;
	
	private static int _moduleIdCounter = 1;
	private int _moduleId;

	private const int STAGES = 3;
	private int amountCorrect = 0;
	private int[] currentSolution;
	private int[] input;
	private bool submitting;
	private bool playing;
	private bool solved;
	private bool realSolve;

	private Coroutine submitModeTimerCoroutine;
	
	void Start ()
	{
		_moduleId = _moduleIdCounter++;
		module = GetComponent<KMBombModule>();
		edgework = GetComponent<KMBombInfo>();
		audio = GetComponent<KMAudio>();
		button = transform.Find("Button").GetComponent<KMSelectable>();
		player = GetComponent<PolyrhythmPlayer>();
		animator = GetComponent<PolyrhythmAnimator>();
		spriteRenderer = transform.Find("Button/Symbol").GetComponent<SpriteRenderer>();

		button.OnInteract += () =>
		{
			ButtonPressed();
			return false;
		};
		button.OnInteractEnded += ButtonReleased;
	}

	void ButtonPressed()
	{
		button.AddInteractionPunch();
		if (solved || playing)
			return;
		
		if (!submitting)
		{
			input = null;
			
			var possibleNumbers = Enumerable.Range(2, 10).ToArray();
			var first = possibleNumbers.PickRandom();
			var second = possibleNumbers.Except(new[] {first}).PickRandom();
			var time = Math.Max(first, second) * Random.Range(0.25f, .5f);
		
			DebugLog("Playing polyrhythm: {0} | {1}", first, second);
			currentSolution = new[] {first % 10, second % 10};
			player.PlayPolyrhythm(first, second, time);
			playing = true;
			DebugLog("Solution: {0} | {1}", first % 10, second % 10);
			StartCoroutine(WaitTimeThenSubmitMode(time));
		}
		else
		{
			if (submitModeTimerCoroutine != null)
				StopCoroutine(submitModeTimerCoroutine);

			input = new[] {(int) edgework.GetTime() % 10, -1};
			spriteRenderer.sprite = sprites[(int) Symbols.CircleFilled];
			audio.PlaySoundAtTransform("lower", transform);
		}
	}

	void ButtonReleased()
	{
		button.AddInteractionPunch(.5f);
		
		if (solved || playing || !submitting || input == null)
			return;

		input[1] = (int) edgework.GetTime() % 10;

		if (input.SequenceEqual(currentSolution))
		{
			amountCorrect++;
			DebugLog("You submitted {0}, that was correct! {1} stages to go.", input.Join(" | "), STAGES - amountCorrect);
			
			if (amountCorrect != STAGES)
				audio.PlaySoundAtTransform("good", transform);
		}
		else
		{
			DebugLog("You submitted {0}, that was incorrect...", input.Join(" | "), STAGES);
			module.HandleStrike();
			audio.PlaySoundAtTransform("strike", transform);
		}

		if (amountCorrect == STAGES)
		{
			DebugLog("Module solved!");
			audio.PlaySoundAtTransform("solve", transform);
			solved = true;
			StartCoroutine(SolveAnimation());
		}
		else
		{
			PulseAnimation(input.SequenceEqual(currentSolution) ? Color.green : Color.red, .5f);
		}
	}
	
	private void DebugLog(string log, params object[] args)
	{
		var logData = string.Format(log, args);
		Debug.LogFormat("[Polyrhythms #{0}] {1}", _moduleId, logData);
	}
	
	void PulseAnimation(Color color, float time)
	{
		spriteRenderer.sprite = sprites[(int) Symbols.Play];
		animator.Pulse(time, (int) Symbols.CircleFilled, color);
		submitting = false;
	}

	bool LastDigitOfTimerIsSame(string number)
	{
		return ((int) edgework.GetTime() % 10).ToString().Equals(number);
	}
		

	IEnumerator WaitTimeThenSubmitMode(float time)
	{
		yield return new WaitForSeconds(time);
		submitting = true;
		playing = false;
		spriteRenderer.sprite = sprites[(int) Symbols.Circle];
		submitModeTimerCoroutine = StartCoroutine(SubmitModeTimer());
	}

	IEnumerator SubmitModeTimer()
	{
		const float SUBMIT_MODE_TIME = 12f;
		if (ZenModeActive)
        {
			var targetTime = edgework.GetTime() + SUBMIT_MODE_TIME;
			if (TwitchPlaysActive)
				targetTime += 10f;
			yield return new WaitUntil(() => edgework.GetTime() > targetTime);
		}
        else
        {
			var targetTime = edgework.GetTime() - SUBMIT_MODE_TIME;
			if (TwitchPlaysActive)
				targetTime -= 10f;
			yield return new WaitUntil(() => edgework.GetTime() < targetTime);
		}
		submitting = false;
		if (amountCorrect > 0)
			amountCorrect--;
		
		DebugLog("You ran out of time! {0} stages to go.", STAGES - amountCorrect);
		PulseAnimation(Color.blue, .5f);
		spriteRenderer.sprite = sprites[(int) Symbols.Play];
		audio.PlaySoundAtTransform("cancel", transform);
	}
	
	IEnumerator SolveAnimation()
	{
		// The solve sound is 100 BPM
		var measure = 60f / 100f * 2f;
		spriteRenderer.color = Color.clear;
		animator.Pulse(measure, (int) Symbols.PlayFilled, Color.yellow);
		
		yield return new WaitForSeconds(measure);
		
		animator.Pulse(measure, (int) Symbols.CircleFilled, Color.yellow);
		
		yield return new WaitForSeconds(measure);

		realSolve = true;
		module.HandlePass();
		animator.Pulse(measure * 4, (int) Symbols.Star, Color.yellow);
	}
	
	string TwitchHelpMessage = "'!{0} play/p' -> Play the polyrhythm. '!{0} submit/s [number] [number]' -> Submit the polyrhythm. '!{0} hold/h [number]' -> Holds the button on that number. '{0} release/r [number]' -> Releases the button on that number. The time to submit an answer is increased by 10 seconds.";
	bool TwitchPlaysActive;
	bool ZenModeActive;
	
	IEnumerator ProcessTwitchCommand(string command)
	{
		if (Regex.IsMatch(command.Trim(), "^(?:(?:p)(?:lay)?|(?:s)(?:ubmit)?(?: [0-9]){2}|(?:h|r|hold|release) [0-9])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;

			var parts = command.ToLowerInvariant().Trim().Split(' ');

			switch (parts[0])
			{
				case "play":
				case "p":
					if (!submitting)
						ButtonPressed();
					else
						yield return "sendtochaterror You cannot play a polyrhythm at this time!";
					break;
				case "submit":
				case "s":
					if (submitting)
					{
						while (submitting && !LastDigitOfTimerIsSame(parts[1]))
							yield return "trycancel";

						if (!submitting)
						{
							yield return "sendtochaterror You ran out of time!";
							yield break;
						}
							
						ButtonPressed();
						
						while (!LastDigitOfTimerIsSame(parts[2]))
							yield return "trycancel";
						
						ButtonReleased();
					}
					else
						yield return "sendtochaterror You cannot submit a polyrhythm at this time!";
					break;
				case "hold":
				case "h":
					if (!submitting)
					{
						yield return "sendtochaterror You cannot hold the button at this time!";
						yield break;
					}
					
					while (submitting && !LastDigitOfTimerIsSame(parts[1]))
						yield return "trycancel";

					if (!submitting)
					{
						yield return "sendtochaterror You ran out of time!";
						yield break;
					}
					
					ButtonPressed();
					break;
				case "release":
				case "r":
					if (!submitting || input == null)
					{
						yield return "sendtochaterror You cannot release the button at this time!";
						yield break;
					}
					
					while (!LastDigitOfTimerIsSame(parts[1]))
						yield return "trycancel";
					
					ButtonReleased();
					break;
				default:
					yield return "sendtochaterror Invalid command: " + parts[0];
					break;
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
    {
		if (submitting && input != null)
        {
			if (input[0] != currentSolution[0])
            {
				StopAllCoroutines();
				DebugLog("Module solved!");
				audio.PlaySoundAtTransform("solve", transform);
				solved = true;
				StartCoroutine(SolveAnimation());
				while (!realSolve) yield return true;
				yield break;
            }
			while (!LastDigitOfTimerIsSame(currentSolution[1].ToString())) { if (!submitting) break; yield return null; }
			if (submitting)
            {
				button.OnInteractEnded();
				yield return new WaitForSeconds(0.05f);
			}
        }
		else if (submitting && input == null)
		{
			while (!LastDigitOfTimerIsSame(currentSolution[0].ToString())) { if (!submitting) break; yield return null; }
			if (submitting)
				button.OnInteract();
			while (!LastDigitOfTimerIsSame(currentSolution[1].ToString())) { if (!submitting) break; yield return null; }
			if (submitting)
            {
				button.OnInteractEnded();
				yield return new WaitForSeconds(0.05f);
			}
		}
		int start = amountCorrect;
		for (int i = start; i < 3; i++)
        {
			if (!playing)
				button.OnInteract();
			while (playing) yield return null;
			while (!LastDigitOfTimerIsSame(currentSolution[0].ToString())) { yield return null; }
			button.OnInteract();
			while (!LastDigitOfTimerIsSame(currentSolution[1].ToString())) { yield return null; }
			button.OnInteractEnded();
			yield return new WaitForSeconds(0.05f);
		}
		while (!realSolve) yield return true;
	}
}