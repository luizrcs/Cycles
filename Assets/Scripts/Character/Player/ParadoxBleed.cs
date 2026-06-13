using System.Collections.Generic;
using UnityEngine;

// In the last minute before the paradox, the PLAYER starts to glitch: brief
// GlitchShell flickers over your own hands and body, more frequent and longer
// as the timer runs out. You are becoming the next double — the game says so
// without a word of UI. Added at runtime by DetectPlayer onto the player.
public class ParadoxBleed : MonoBehaviour
{
    private PlayerTimer timer;
    private Material shellMaterial;
    private List<(SkinnedMeshRenderer shell, SkinnedMeshRenderer body)> shells;

    private float flickerUntil = -1f;
    private float nextFlicker;
    private bool shellsOn;

    private const float StartAt = 180f; // bleed begins 60 s before the paradox

    private static readonly int Intensity = Shader.PropertyToID("_Intensity");

    void Start()
    {
        timer = FindAnyObjectByType<PlayerTimer>();
        Shader shader = Shader.Find("Cycles/GlitchShell");
        if (timer == null || shader == null)
        {
            enabled = false;
            return;
        }

        shellMaterial = new Material(shader);
        shells = AntiPlayerGlitch.BuildShells(this, shellMaterial);
        SetShells(false);
    }

    void Update()
    {
        float ramp = Mathf.Clamp01((timer.Elapsed - StartAt) / (PlayerTimer.MaxTime - StartAt));
        if (ramp <= 0f)
        {
            if (shellsOn) SetShells(false);
            return;
        }

        // The paradox is a death too: the reel degrades with it.
        FilmDamage.ReportDanger(ramp * 0.9f);

        float t = Time.time;
        if (t < flickerUntil) return;

        if (shellsOn) SetShells(false);

        if (t > nextFlicker)
        {
            shellMaterial.SetFloat(Intensity, 0.2f + 0.6f * ramp);
            SetShells(true);
            flickerUntil = t + Random.Range(0.06f, 0.18f + 0.3f * ramp);
            nextFlicker = flickerUntil + Random.Range(2f, 9f) * (1.05f - ramp);
        }
    }

    private void SetShells(bool on)
    {
        shellsOn = on;
        foreach (var (shell, body) in shells)
            if (shell != null) shell.enabled = on && body != null && body.enabled;
    }
}
