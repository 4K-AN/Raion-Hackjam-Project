using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    public enum StepType { Video, Image }

    [System.Serializable]
    public class CutsceneStep
    {
        public StepType type;
        public VideoClip videoClip;
        public Sprite imageSprite;
        public float imageDuration = 3f; // durasi tampilan image
    }

    [Header("Sequence steps")]
    public CutsceneStep[] steps;

    [Header("References")]
    public VideoPlayer videoPlayer;
    public RawImage imageDisplay; // Tetap RawImage

    [Header("Optional skip controls (for testing)")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;
    public bool skipOnMouseClick = true;

    private int stepIndex = 0;
    private Coroutine imageCoroutine = null;
    private bool isFinished = false;

    void Start()
    {
        // safety checks
        if (videoPlayer == null) Debug.LogWarning("[CutsceneManager] VideoPlayer not assigned.");
        if (imageDisplay == null) Debug.LogWarning("[CutsceneManager] ImageDisplay not assigned.");
        PlayStep();
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

    void PlayStep()
    {
        // Matikan semua dulu
        if (videoPlayer != null) videoPlayer.gameObject.SetActive(false);
        if (imageDisplay != null) imageDisplay.gameObject.SetActive(false);

        if (stepIndex >= steps.Length)
        {
            Debug.Log("[CutsceneManager] Cutscene selesai semua step!");
            isFinished = true;
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
            // Wait until prepared then play (use coroutine)
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

            Debug.Log("[CutsceneManager] Menampilkan IMAGE: " + current.imageSprite.name + " (step " + stepIndex + "), durasi: " + current.imageDuration);
            imageDisplay.gameObject.SetActive(true);
            
            // Convert Sprite to Texture and assign to RawImage
            imageDisplay.texture = current.imageSprite.texture;
            
            // Optional: Adjust UV rect untuk menampilkan bagian sprite yang benar
            Rect spriteRect = current.imageSprite.rect;
            Rect uvRect = new Rect(
                spriteRect.x / current.imageSprite.texture.width,
                spriteRect.y / current.imageSprite.texture.height,
                spriteRect.width / current.imageSprite.texture.width,
                spriteRect.height / current.imageSprite.texture.height
            );
            imageDisplay.uvRect = uvRect;

            // pastikan coroutine image sebelumnya dihentikan
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            imageCoroutine = StartCoroutine(WaitAndNext(current.imageDuration));
        }
        else
        {
            Debug.LogWarning("[CutsceneManager] Step " + stepIndex + " tidak valid atau data kosong! Lanjut ke step berikutnya.");
            NextStep();
        }
    }

    public static class RawImageUtil
    {
        // raw: target RawImage, tex: texture yang sudah diassign ke raw.texture
        public static void FillAndCropRawImage(RawImage raw, Texture tex)
        {
            if (raw == null || tex == null) return;

            // Pastikan layout UI sudah ter-update sehingga parent rect valid
            Canvas.ForceUpdateCanvases();

            // ambil ukuran parent (biasanya Canvas area)
            RectTransform parentRt = raw.rectTransform.parent as RectTransform;
            float parentW = parentRt != null ? parentRt.rect.width : raw.rectTransform.rect.width;
            float parentH = parentRt != null ? parentRt.rect.height : raw.rectTransform.rect.height;
            if (parentW <= 0f || parentH <= 0f)
            {
                // fallback: pakai raw rect
                parentW = Mathf.Max(1f, raw.rectTransform.rect.width);
                parentH = Mathf.Max(1f, raw.rectTransform.rect.height);
            }

            float texW = tex.width;
            float texH = tex.height;
            if (texW <= 0f || texH <= 0f) return;

            // scale untuk "cover" (fill & crop)
            float sX = parentW / texW;
            float sY = parentH / texH;
            float s = Mathf.Max(sX, sY);

            // visible portion in UV space (0..1)
            float uvW = (parentW / (texW * s)); // equivalent to sX / s
            float uvH = (parentH / (texH * s)); // equivalent to sY / s

            // center the uvRect
            float uvX = Mathf.Clamp01((1f - uvW) * 0.5f);
            float uvY = Mathf.Clamp01((1f - uvH) * 0.5f);

            raw.uvRect = new Rect(uvX, uvY, Mathf.Clamp01(uvW), Mathf.Clamp01(uvH));

            // ensure anchors stretch full screen (optional safety)
            RectTransform rt = raw.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            Debug.Log($"[RawImageUtil] FillCrop parent({parentW}x{parentH}) tex({texW}x{texH}) uvRect={raw.uvRect}");
        }
    }

    private IEnumerator PrepareAndPlayVideo(VideoPlayer vp)
    {
        // tunggu sampai prepared (atau timeout 10s)
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

        // assign texture ke target (jika menggunakan RawImage set up berbeda; di sini kita pake VideoPlayer render ke camera atau RenderTexture)
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
            // jika user skip, coroutine akan dihentikan oleh SkipCurrentStep()
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
                // unsubscribe agar OnVideoEnd tidak terpanggil lagi
                videoPlayer.loopPointReached -= OnVideoEnd;
                if (videoPlayer.isPlaying) videoPlayer.Stop();
            }
            NextStep();
            return;
        }

        // default
        NextStep();
    }
}