using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

   
    [SerializeField] private AudioSource audioSource;

    
    [SerializeField] private AudioClip[] stage1Dialogues;
    [SerializeField] private AudioClip[] stage2Dialogues;
    [SerializeField] private AudioClip[] stage3Dialogues;

    
    [SerializeField] private float delayBetweenClips = 1f; 

    private Coroutine currentDialogue;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayDialoguesForStage(int stage)
    {
        
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