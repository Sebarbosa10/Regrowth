using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Diálogos por Stage")]
    [SerializeField] private AudioClip[] stage1Dialogues;
    [SerializeField] private AudioClip[] stage2Dialogues;
    [SerializeField] private AudioClip[] stage3Dialogues;

    [Header("Settings")]
    [SerializeField] private float delayBetweenClips = 1f; // pausa entre audios

    private Coroutine currentDialogue;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayDialoguesForStage(int stage)
    {
        // Si hay algo reproduciéndose, lo para
        if (currentDialogue != null)
        {
            StopCoroutine(currentDialogue);
            audioSource.Stop();
        }

        AudioClip[] clips = null;

        switch (stage)
        {
            case 0: clips = stage1Dialogues; break;
            case 1: clips = stage2Dialogues; break;
            case 2: clips = stage3Dialogues; break;
        }

        if (clips != null && clips.Length > 0)
        {
            currentDialogue = StartCoroutine(PlayClipsInOrder(clips));
        }
    }

    private IEnumerator PlayClipsInOrder(AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            audioSource.clip = clip;
            audioSource.Play();

            // Espera que termine el clip
            yield return new WaitForSeconds(clip.length + delayBetweenClips);
        }

        currentDialogue = null;
    }

    public void StopDialogue()
    {
        if (currentDialogue != null)
        {
            StopCoroutine(currentDialogue);
            currentDialogue = null;
        }
        audioSource.Stop();
    }
}