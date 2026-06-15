using System.Collections;
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

    private void Awake() { Instance = this; }

    public void PlayDialoguesForStage(int stage)
    {
        if (currentDialogue != null)
        {
            StopCoroutine(currentDialogue);
            audioSource.Stop();
        }

        AudioClip[] clips = stage switch
        {
            0 => stage1Dialogues,
            1 => stage2Dialogues,
            2 => stage3Dialogues,
            _ => null
        };

        if (clips != null && clips.Length > 0)
            currentDialogue = StartCoroutine(PlayClipsInOrder(clips));
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
