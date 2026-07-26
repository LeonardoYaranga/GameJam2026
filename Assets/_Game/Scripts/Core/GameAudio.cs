using System.Collections.Generic;
using UnityEngine;

namespace NidoCero
{
    public enum GameSfx
    {
        MenuClick,
        Jump,
        PlayerCry,
        PlayerLaser,
        Rockfall,
        CrabClaw,
        TankShot,
        TankStomp,
        RobotCry,
        RobotDeath,
        Explosion,
        MechanicalMovement
    }

    /// <summary>
    /// Central SFX router. Clips live under Resources/Audio/SFX so every scene
    /// can use the same library without scene-specific references.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private static readonly Dictionary<GameSfx, string> Paths = new()
        {
            { GameSfx.MenuClick, "Audio/SFX/sfx_teclas_menu" },
            { GameSfx.Jump, "Audio/SFX/sfx_salto_2" },
            { GameSfx.PlayerCry, "Audio/SFX/sfx_graznido_2" },
            { GameSfx.PlayerLaser, "Audio/SFX/sfx_disparo_laser" },
            { GameSfx.Rockfall, "Audio/SFX/sfx_rocas_cayendo" },
            { GameSfx.CrabClaw, "Audio/SFX/sfx_tenazas_robot" },
            { GameSfx.TankShot, "Audio/SFX/sfx_disparo_tanque_2" },
            { GameSfx.TankStomp, "Audio/SFX/sfx_tanque_pisoton" },
            { GameSfx.RobotCry, "Audio/SFX/sfx_graznido_robot" },
            { GameSfx.RobotDeath, "Audio/SFX/sfx_muerte_robot_2" },
            { GameSfx.Explosion, "Audio/SFX/sfx_explosion" },
            { GameSfx.MechanicalMovement, "Audio/SFX/sfx_movimiento_mecanico_4" }
        };

        private static GameAudio instance;
        private readonly Dictionary<GameSfx, AudioClip> clips = new();
        private AudioSource oneShotSource;
        private AudioSource loopSource;

        public static GameAudio Instance => EnsureInstance();
        public static GameSfx? LastPlayed { get; private set; }
        public static bool IsMechanicalLoopPlaying =>
            instance != null && instance.loopSource != null && instance.loopSource.isPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            LastPlayed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            oneShotSource = CreateSource("OneShots", false);
            loopSource = CreateSource("MechanicalLoop", true);
        }

        public static void Play(GameSfx id, float volume = 1f)
        {
            GameAudio audio = EnsureInstance();
            AudioClip clip = audio.LoadClip(id);
            if (clip == null) return;
            audio.oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            LastPlayed = id;
        }

        public static void StartMechanicalLoop(float volume = 0.38f)
        {
            GameAudio audio = EnsureInstance();
            AudioClip clip = audio.LoadClip(GameSfx.MechanicalMovement);
            if (clip == null) return;
            audio.loopSource.clip = clip;
            audio.loopSource.volume = Mathf.Clamp01(volume);
            if (!audio.loopSource.isPlaying) audio.loopSource.Play();
            LastPlayed = GameSfx.MechanicalMovement;
        }

        public static void StopMechanicalLoop()
        {
            if (instance == null || instance.loopSource == null) return;
            instance.loopSource.Stop();
            instance.loopSource.clip = null;
        }

        public static bool HasClip(GameSfx id)
        {
            return EnsureInstance().LoadClip(id) != null;
        }

        public static string ResourcePath(GameSfx id)
        {
            return Paths.TryGetValue(id, out string path) ? path : string.Empty;
        }

        private static GameAudio EnsureInstance()
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<GameAudio>();
            if (instance != null) return instance;
            GameObject root = new GameObject("GameAudio");
            instance = root.AddComponent<GameAudio>();
            return instance;
        }

        private AudioClip LoadClip(GameSfx id)
        {
            if (clips.TryGetValue(id, out AudioClip clip)) return clip;
            string path = ResourcePath(id);
            clip = string.IsNullOrEmpty(path) ? null : Resources.Load<AudioClip>(path);
            clips[id] = clip;
            if (clip == null) Debug.LogWarning("Audio no encontrado: " + id + " (" + path + ")");
            return clip;
        }

        private AudioSource CreateSource(string childName, bool loop)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
