using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Animator animator;

    [Header("Kill Sound")]
    [SerializeField] private AudioSource killAudioSource;
    [SerializeField] private AudioClip killSound;

    [SerializeField] private Graphic hitMarkerGraphic;
    [SerializeField] private Graphic killMarkerGraphic;

    public void ShowHit()
    {
        if (hitMarkerGraphic != null)
            hitMarkerGraphic.color = Color.white;

        if (animator != null)
            animator.SetTrigger("Hit");
    }

    public void ShowKill()
    {
        if (killMarkerGraphic != null)
            killMarkerGraphic.color = new Color(1f, 0.85f, 0.25f, 1f);

        if (animator != null)
            animator.SetTrigger("Kill");

        if (killAudioSource != null && killSound != null)
        {
            killAudioSource.PlayOneShot(killSound);
        }
    }
}
