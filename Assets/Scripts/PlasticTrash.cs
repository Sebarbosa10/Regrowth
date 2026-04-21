using UnityEngine;

public class PlasticTrash : TrashItem
{
    [SerializeField] private AudioClip firstPickupClip;
    [SerializeField][Range(0f, 1f)] private float volume = 1f;

    private static bool hasBeenPickedUp = false;

    public override void OnVacuumed()
    {
        if (hasBeenPickedUp || firstPickupClip == null) return;

        hasBeenPickedUp = true;

        if (TrashAudioPlayer.Instance != null)
            TrashAudioPlayer.Instance.PlayOneShot(firstPickupClip, volume);
    }

    public static void ResetPickupState() => hasBeenPickedUp = false;
}