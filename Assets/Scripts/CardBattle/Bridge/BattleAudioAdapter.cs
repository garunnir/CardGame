using UnityEngine;
using UnityEngine.Audio;

namespace CardGame.CardBattle.Bridge
{
    /// <summary>메인 AudioMixerGroup 연동 스텁.</summary>
    public sealed class BattleAudioAdapter : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioSource sfxSource;

        public void Configure(AudioMixerGroup group, AudioSource source)
        {
            sfxGroup = group;
            sfxSource = source;
            if (sfxSource != null && sfxGroup != null)
            {
                sfxSource.outputAudioMixerGroup = sfxGroup;
            }
        }

        public void PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }
    }
}
