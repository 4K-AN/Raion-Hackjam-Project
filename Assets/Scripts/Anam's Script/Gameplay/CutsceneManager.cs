using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    public enum StepType { Video, Image }
    public enum DurationType { Fixed, Random }

    [Serializable]
    public class CutsceneStep
    {
        public StepType type;
        public VideoClip videoClip;
        public Sprite imageSprite;
        
        [Header("Image Duration Settings")]
        public DurationType durationType = DurationType.Fixed;
        
        [Header("Fixed Duration")]
        public float imageDuration = 3f; // durasi tampilan image (untuk fixed)
        
        [Header("Random Duration")]
        public float minDuration = 1f;   // durasi minimum (untuk random)
        public float maxDuration = 5f;   // durasi maksimum (untuk random)
        
        // Method untuk mendapatkan duration berdasarkan tipe
        public float GetDuration()
        {
            if (durationType == DurationType.Random)
            {
                return UnityEngine.Random.Range(minDuration, maxDuration);
            }
            return imageDuration;
        }
    }

    [Header("Sequence steps")]
    public CutsceneStep[] steps;

    [Header("References")]
    public VideoPlayer videoPlayer;
    public RawImage imageDisplay; // gunakan RawImage agar bisa assign Texture

    [Header("Behaviour")]
    public bool autoPlayOnStart = false; // jika true akan PlayStep() di Start()
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;
    public bool skipOnMouseClick = true;

    // public event supaya GameManager bisa menunggu / subscribe
    public Action OnCutsceneFinished;

    // runtime
    private int stepIndex = 0;
    private Coroutine imageCoroutine = null;
    private bool isFinished = false;

    // temporary sequence support
    private bool isTempSequence = false;
    private CutsceneStep[] backupSteps = null;

    void Start()
    {
        if (videoPlayer == null) Debug.LogWarning("[CutsceneManager] VideoPlayer not assigned.");
        if (imageDisplay == null) Debug.LogWarning("[CutsceneManager] ImageDisplay (RawImage) not assigned.");

        if (autoPlayOnStart)
        {
            PlaySequence(); // play steps assigned in inspector
        }
    }

    void Update()
    {
        if (isFinished || !allowSkip) return;

        if (Input.GetKeyDown(skipKey) || (skipOnMouseClick && Input.GetMouseButtonDown(0)))
        {
            Debug.Log("[CutsceneManager] Skip requested by user.");
            SkipCurrentStep();
        }
    }

    public void StopCurrentCutscene()
{
    // Stop any ongoing coroutines
    StopAllCoroutines();
    
    // Hide video player if active
    if (videoPlayer != null && videoPlayer.isPlaying)
    {
        videoPlayer.Stop();
        videoPlayer.gameObject.SetActive(false);
    }
    
    // Hide image display if active  
    if (imageDisplay != null)
    {
        imageDisplay.gameObject.SetActive(false);
    }
    
    Debug.Log("[CutsceneManager] Current cutscene stopped");
}

    // ---------------------
    // Public API
    // ---------------------
    public bool IsPlaying => !isFinished && (imageCoroutine != null || (videoPlayer != null && videoPlayer.isPlaying));

    // play the sequence defined in 'steps' from inspector
    public void PlaySequence()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[CutsceneManager] PlaySequence called but no steps defined.");
            SequenceFinished();
            return;
        }

        stepIndex = 0;
        isFinished = false;
        PlayStep();
    }

    // play a custom sequence (temporary). After finished original steps restored.
    public void PlaySequence(CutsceneStep[] newSteps)
    {
        if (newSteps == null || newSteps.Length == 0)
        {
            Debug.LogWarning("[CutsceneManager] PlaySequence(newSteps) called with null/empty.");
            SequenceFinished();
            return;
        }

        backupSteps = steps;
        steps = newSteps;
        isTempSequence = true;
        stepIndex = 0;
        isFinished = false;
        PlayStep();
    }

    // play a single VideoClip (temporary) — convenient for GameManager usage
    public void PlaySingleClip(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CutsceneManager] PlaySingleClip called with null clip.");
            // still invoke finished to avoid callers waiting forever
            OnCutsceneFinished?.Invoke();
            return;
        }

        CutsceneStep[] tmp = new CutsceneStep[1];
        tmp[0] = new CutsceneStep { type = StepType.Video, videoClip = clip, imageDuration = 0f };
        PlaySequence(tmp);
    }

    // Method tambahan untuk membuat image step dengan duration random
    public void PlaySingleImage(Sprite sprite, float minDuration = 1f, float maxDuration = 5f)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[CutsceneManager] PlaySingleImage called with null sprite.");
            OnCutsceneFinished?.Invoke();
            return;
        }

        CutsceneStep[] tmp = new CutsceneStep[1];
        tmp[0] = new CutsceneStep 
        { 
            type = StepType.Image, 
            imageSprite = sprite,
            durationType = DurationType.Random,
            minDuration = minDuration,
            maxDuration = maxDuration
        };
        PlaySequence(tmp);
    }

    // Method untuk membuat image step dengan duration fixed
    public void PlaySingleImage(Sprite sprite, float fixedDuration)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[CutsceneManager] PlaySingleImage called with null sprite.");
            OnCutsceneFinished?.Invoke();
            return;
        }

        CutsceneStep[] tmp = new CutsceneStep[1];
        tmp[0] = new CutsceneStep 
        { 
            type = StepType.Image, 
            imageSprite = sprite,
            durationType = DurationType.Fixed,
            imageDuration = fixedDuration
        };
        PlaySequence(tmp);
    }

    // ---------------------
    // Core sequence runner
    // ---------------------
    void PlayStep()
    {
        // Matikan semua dulu
        if (videoPlayer != null) videoPlayer.gameObject.SetActive(false);
        if (imageDisplay != null) imageDisplay.gameObject.SetActive(false);

        if (steps == null || stepIndex >= steps.Length)
        {
            Debug.Log("[CutsceneManager] Cutscene selesai semua step!");
            SequenceFinished();
            return;
        }

        CutsceneStep current = steps[stepIndex];

        if (current.type == StepType.Video && current.videoClip != null)
        {
            if (videoPlayer == null)
            {
                Debug.LogError("[CutsceneManager] Video step requested but VideoPlayer is null.");
                NextStep();
                return;
            }

            Debug.Log("[CutsceneManager] Memainkan VIDEO: " + current.videoClip.name + " (step " + stepIndex + ")");
            videoPlayer.gameObject.SetActive(true);

            // unsubscribe dulu untuk menghindari double-subscribe
            videoPlayer.loopPointReached -= OnVideoEnd;

            videoPlayer.clip = current.videoClip;
            videoPlayer.Prepare();
            StartCoroutine(PrepareAndPlayVideo(videoPlayer));
        }
        else if (current.type == StepType.Image && current.imageSprite != null)
        {
            if (imageDisplay == null)
            {
                Debug.LogError("[CutsceneManager] Image step requested but imageDisplay is null.");
                NextStep();
                return;
            }

            // Dapatkan duration berdasarkan tipe (fixed atau random)
            float actualDuration = current.GetDuration();
            string durationInfo = current.durationType == DurationType.Random ? 
                $"random({current.minDuration:F1}-{current.maxDuration:F1})" : 
                "fixed";

            Debug.Log($"[CutsceneManager] Menampilkan IMAGE: {current.imageSprite.name} (step {stepIndex}), durasi: {actualDuration:F2}s [{durationInfo}]");
            imageDisplay.gameObject.SetActive(true);

            // assign texture and auto-fit / crop correctly (supports sprite in atlas)
            RawImageUtil.FillAndCropRawImageForSprite(imageDisplay, current.imageSprite);

            // start wait coroutine with actual duration
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            imageCoroutine = StartCoroutine(WaitAndNext(actualDuration));
        }
        else
        {
            Debug.LogWarning("[CutsceneManager] Step " + stepIndex + " tidak valid atau data kosong! Lanjut ke step berikutnya.");
            NextStep();
        }
    }

    // ---------------------
    // Video prepare & play
    // ---------------------
    private IEnumerator PrepareAndPlayVideo(VideoPlayer vp)
    {
        float timeout = 10f;
        float timer = 0f;
        while (!vp.isPrepared && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!vp.isPrepared)
        {
            Debug.LogError("[CutsceneManager] Video gagal dipersiapkan dalam timeout. Clip: " + (vp.clip ? vp.clip.name : "null"));
            // langsung next agar cutscene tidak hang
            OnVideoEnd(vp);
            yield break;
        }

        // Assign video texture to RawImage when possible so imageDisplay can control aspect/cropping
        // If VideoPlayer uses TargetTexture (RenderTexture), use that. Otherwise use vp.texture (API).
        if (imageDisplay != null)
        {
            if (vp.targetTexture != null)
            {
                imageDisplay.texture = vp.targetTexture;
                RawImageUtil.FillAndCropRawImage(imageDisplay, vp.targetTexture);
                imageDisplay.gameObject.SetActive(true);
            }
            else if (vp.texture != null)
            {
                imageDisplay.texture = vp.texture;
                RawImageUtil.FillAndCropRawImage(imageDisplay, vp.texture);
                imageDisplay.gameObject.SetActive(true);
            }
        }

        vp.Play();
        Debug.Log("[CutsceneManager] Video started: " + (vp.clip ? vp.clip.name : "null"));

        // subscribe untuk event selesai
        vp.loopPointReached -= OnVideoEnd;
        vp.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        // unsub dulu supaya tidak terpanggil ganda
        vp.loopPointReached -= OnVideoEnd;
        Debug.Log("[CutsceneManager] Video finished: " + (vp.clip ? vp.clip.name : "null"));
        NextStep();
    }

    System.Collections.IEnumerator WaitAndNext(float duration)
    {
        // tunggu satu frame agar image benar-benar dirender sebelum menghitung durasi
        yield return null;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        imageCoroutine = null;
        Debug.Log("[CutsceneManager] Image duration ended.");
        NextStep();
    }

    void NextStep()
    {
        // hentikan coroutine image jika masih berjalan
        if (imageCoroutine != null)
        {
            StopCoroutine(imageCoroutine);
            imageCoroutine = null;
        }

        // hentikan video jika sedang play (untuk menjaga konsistensi)
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        stepIndex++;
        PlayStep();
    }

    void SkipCurrentStep()
    {
        if (stepIndex >= steps.Length)
            return;

        CutsceneStep current = steps[stepIndex];

        Debug.Log("[CutsceneManager] Skipping step " + stepIndex + " (type: " + current.type + ")");

        // jika gambar sedang tampil, hentikan coroutine dan lanjut
        if (current.type == StepType.Image)
        {
            if (imageCoroutine != null)
            {
                StopCoroutine(imageCoroutine);
                imageCoroutine = null;
            }
            NextStep();
            return;
        }

        // jika video sedang dipersiapkan atau dimainkan, hentikan dan langsung lanjut
        if (current.type == StepType.Video)
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoEnd;
                if (videoPlayer.isPlaying) videoPlayer.Stop();
            }
            NextStep();
            return;
        }

        // default
        NextStep();
    }

    // ---------------------
    // When sequence finishes (end of steps)
    // ---------------------
    private void SequenceFinished()
    {
        isFinished = true;

        // restore steps if temporary
        if (isTempSequence)
        {
            steps = backupSteps;
            backupSteps = null;
            isTempSequence = false;
        }

        Debug.Log("[CutsceneManager] Sequence finished. Firing OnCutsceneFinished.");
        OnCutsceneFinished?.Invoke();
    }

    // ---------------------
    // RawImage helper util
    // ---------------------
    public static class RawImageUtil
    {
        // full-texture version: assign raw.texture and crop/cover to parent area
        public static void FillAndCropRawImage(RawImage raw, Texture tex)
        {
            if (raw == null || tex == null) return;

            Canvas.ForceUpdateCanvases();

            RectTransform parentRt = raw.rectTransform.parent as RectTransform;
            float parentW = parentRt != null ? parentRt.rect.width : raw.rectTransform.rect.width;
            float parentH = parentRt != null ? parentRt.rect.height : raw.rectTransform.rect.height;
            if (parentW <= 0f || parentH <= 0f)
            {
                parentW = Mathf.Max(1f, raw.rectTransform.rect.width);
                parentH = Mathf.Max(1f, raw.rectTransform.rect.height);
            }

            float texW = tex.width;
            float texH = tex.height;
            if (texW <= 0f || texH <= 0f) return;

            float sX = parentW / texW;
            float sY = parentH / texH;
            float s = Mathf.Max(sX, sY);

            float uvW = (parentW / (texW * s));
            float uvH = (parentH / (texH * s));

            float uvX = Mathf.Clamp01((1f - uvW) * 0.5f);
            float uvY = Mathf.Clamp01((1f - uvH) * 0.5f);

            raw.uvRect = new Rect(uvX, uvY, Mathf.Clamp01(uvW), Mathf.Clamp01(uvH));

            RectTransform rt = raw.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            Debug.Log($"[RawImageUtil] FillCrop parent({parentW}x{parentH}) tex({texW}x{texH}) uvRect={raw.uvRect}");
        }

        // Sprite-aware version: supports sprites that are subrects (atlas)
        public static void FillAndCropRawImageForSprite(RawImage raw, Sprite sprite)
        {
            if (raw == null || sprite == null || sprite.texture == null) return;

            Canvas.ForceUpdateCanvases();

            RectTransform parentRt = raw.rectTransform.parent as RectTransform;
            float parentW = parentRt != null ? parentRt.rect.width : raw.rectTransform.rect.width;
            float parentH = parentRt != null ? parentRt.rect.height : raw.rectTransform.rect.height;
            if (parentW <= 0f || parentH <= 0f)
            {
                parentW = Mathf.Max(1f, raw.rectTransform.rect.width);
                parentH = Mathf.Max(1f, raw.rectTransform.rect.height);
            }

            Texture tex = sprite.texture;
            Rect spriteRect = sprite.rect; // subrect on texture
            float texW = tex.width;
            float texH = tex.height;

            float sX = parentW / spriteRect.width;
            float sY = parentH / spriteRect.height;
            float s = Mathf.Max(sX, sY);

            // uv width/height relative to full texture
            float uvW = parentW / (texW * s);
            float uvH = parentH / (texH * s);

            // normalized sprite region
            float spriteNormX = spriteRect.x / texW;
            float spriteNormY = spriteRect.y / texH;
            float spriteNormW = spriteRect.width / texW;
            float spriteNormH = spriteRect.height / texH;

            float finalW = Mathf.Clamp01(uvW);
            float finalH = Mathf.Clamp01(uvH);

            float uvX = spriteNormX + Mathf.Clamp01((spriteNormW - finalW) * 0.5f);
            float uvY = spriteNormY + Mathf.Clamp01((spriteNormH - finalH) * 0.5f);

            raw.texture = tex;
            raw.uvRect = new Rect(uvX, uvY, Mathf.Clamp01(finalW * spriteNormW), Mathf.Clamp01(finalH * spriteNormH));

            RectTransform rt = raw.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            Debug.Log($"[RawImageUtil] FillCropSprite parent({parentW}x{parentH}) spriteRect({spriteRect.width}x{spriteRect.height}) uvRect={raw.uvRect}");
        }
    }
}