using UnityEngine;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.5f;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // Ensure known, non-blocking start state
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false; ;
    }

    public IEnumerator FadeOutRoutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            //Debug.Log($"Fading out: {canvasGroup.alpha}");
            yield return null;
        }
    }

    public IEnumerator FadeInRoutine()
    {
        yield return new WaitForSeconds(0.25f); // wait a frame to avoid visual hitch
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            //Debug.Log($"Fading in: {canvasGroup.alpha}");
            yield return null;
        }
    }
}
