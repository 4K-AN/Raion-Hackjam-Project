using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class HealthVideoSwitcherCrossfade : MonoBehaviour
{
    [Header("Mapping clips by HP (index: HP value)")]
    // Option A: index 1 => 1HP, index 2 => 2HP, index 3 => 3HP (index 0 unused)
    public VideoClip[] clipsIndexedByHP;

    [Header("UI Targets (two RawImages for crossfade)")]
    public RawImage rawA;
    public RawImage rawB;
    public CanvasGroup canvasA; // CanvasGroup of rawA parent (for alpha)
    public CanvasGroup canvasB; // CanvasGroup of rawB parent
    public float crossfadeDuration = 0.25f;

    [Header("VideoPlayers (attached to same GameObject or separate)")]
    public VideoPlayer vpA;
    public VideoPlayer vpB;

    // internal
    private bool usingA = true;
    private Coroutine prepareCoroutine;

    private void Awake()
    {
        // Quick sanity: ensure VideoPlayers configured for APIOnly
        SetupVideoPlayer(vpA);
        SetupVideoPlayer(vpB);

        // start with A visible
        if (canvasA != null) canvasA.alpha = 1f;
        if (canvasB != null) canvasB.alpha = 0f;
    }

    private void SetupVideoPlayer(VideoPlayer vp)
    {
        if (vp == null) return;
        vp.playOnAwake = false;
        vp.renderMode = VideoRenderMode.APIOnly;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        // if no audio source, VideoPlayer will still play video
    }

    // Public hook — panggil saat health berubah
    public void OnHealthChanged(int hp)
    {
        PlayClipForHP(hp);
    }

    // map hp -> clip index and play
    public void PlayClipForHP(int hp)
    {
        // safe bounds: clipsIndexedByHP[hp] should exist
        if (clipsIndexedByHP == null || clipsIndexedByHP.Length <= hp || clipsIndexedByHP[hp] == null)
        {
            Debug.LogWarning($"[HealthVideoSwitcher] No clip for HP={hp} (clips length:{(clipsIndexedByHP==null?0:clipsIndexedByHP.Length)})");
            return;
        }

        VideoClip clip = clipsIndexedByHP[hp];
        if (prepareCoroutine != null) StopCoroutine(prepareCoroutine);
        prepareCoroutine = StartCoroutine(PrepareAndCrossfade(clip));
    }

    private IEnumerator PrepareAndCrossfade(VideoClip newClip)
    {
        // choose target player / raw
        VideoPlayer targetVP = usingA ? vpB : vpA;
        RawImage targetRaw = usingA ? rawB : rawA;
        CanvasGroup targetCanvas = usingA ? canvasB : canvasA;

        // prepare target vp
        targetVP.Stop();
        targetVP.clip = newClip;
        targetVP.Prepare();

        // wait until prepared (don't block main thread)
        while (!targetVP.isPrepared)
            yield return null;

        // assign texture
        if (targetRaw != null)
            targetRaw.texture = targetVP.texture;

        // start playing both audio and video on target
        targetVP.Play();
        // optional: stop current after crossfade ends

        // crossfade alpha
        yield return StartCoroutine(Crossfade(targetCanvas, usingA ? canvasA : canvasB, crossfadeDuration));

        // stop the previous player to free resources
        VideoPlayer prevVP = usingA ? vpA : vpB;
        prevVP.Stop();

        // flip active flag
        usingA = !usingA;
        prepareCoroutine = null;
    }

    private IEnumerator Crossfade(CanvasGroup fadeIn, CanvasGroup fadeOut, float dur)
    {
        if (fadeIn == null || fadeOut == null)
            yield break;

        float t = 0f;
        fadeIn.alpha = 0f;
        fadeOut.alpha = 1f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float v = t / dur;
            fadeIn.alpha = Mathf.Lerp(0f, 1f, v);
            fadeOut.alpha = Mathf.Lerp(1f, 0f, v);
            yield return null;
        }
        fadeIn.alpha = 1f;
        fadeOut.alpha = 0f;
    }

    // Debug helpers to test from Inspector or Button
    [ContextMenu("Test Play 3HP (index 3)")]
    public void Test3HP() => PlayClipForHP(Mathf.Min(3, clipsIndexedByHP.Length - 1));
}
