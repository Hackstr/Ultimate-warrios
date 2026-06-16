using UnityEngine;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Singleton audio manager. Provides Music (looped) and SFX (one-shot) playback.
    /// All AudioClip fields are null by default — assign them in the Inspector.
    /// Null clips are silently skipped (no errors).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton

        public static AudioManager Instance { get; private set; }

        #endregion

        #region Volume Controls

        [Header("Volume (0–1)")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.8f;

        #endregion

        #region AudioSources

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        #endregion

        #region Sound Slots — UI

        [Header("UI")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip menuOpen;
        [SerializeField] private AudioClip menuClose;
        [SerializeField] private AudioClip confirm;
        [SerializeField] private AudioClip cancel;

        #endregion

        #region Sound Slots — Planning

        [Header("Planning")]
        [SerializeField] private AudioClip actionQueued;
        [SerializeField] private AudioClip actionUndone;
        [SerializeField] private AudioClip timerPulse;
        [SerializeField] private AudioClip timerTicking;
        [SerializeField] private AudioClip timeExpired;

        #endregion

        #region Sound Slots — Combat

        [Header("Combat")]
        [SerializeField] private AudioClip shoot;
        [SerializeField] private AudioClip hitLand;
        [SerializeField] private AudioClip armorBreak;
        [SerializeField] private AudioClip mutualCancel;
        [SerializeField] private AudioClip elimination;
        [SerializeField] private AudioClip dangerZoneWarn;

        #endregion

        #region Sound Slots — Hero Movement

        [Header("Hero Movement")]
        [SerializeField] private AudioClip heroMove;
        [SerializeField] private AudioClip heroTurn;

        #endregion

        #region Sound Slots — Feedback

        [Header("Feedback")]
        [SerializeField] private AudioClip victory;
        [SerializeField] private AudioClip defeat;
        [SerializeField] private AudioClip roundComplete;

        #endregion

        #region Sound Slots — Music

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip heroSelectMusic;
        [SerializeField] private AudioClip planningMusic;
        [SerializeField] private AudioClip executionMusic;
        [SerializeField] private AudioClip victoryMusic;
        [SerializeField] private AudioClip defeatMusic;

        #endregion

        #region Crossfade Settings

        [Header("Crossfade")]
        [SerializeField] private float _crossfadeDuration = 1f;

        private AudioSource _musicSourceB; // second source for crossfade
        private bool _usingSourceA = true;
        private float _crossfadeTimer;
        private bool _crossfading;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-load clips from Resources/Audio if not assigned via Inspector
            AutoLoadClips();

            // Create AudioSources
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = _masterVolume * _musicVolume;

            _musicSourceB = gameObject.AddComponent<AudioSource>();
            _musicSourceB.loop = true;
            _musicSourceB.playOnAwake = false;
            _musicSourceB.volume = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = _masterVolume * _sfxVolume;

            // Apply saved volume settings
            if (PlayerPrefs.GetInt("music_on", 1) == 0) SetMusicVolume(0f);
            if (PlayerPrefs.GetInt("sfx_on", 1) == 0) SetSFXVolume(0f);
        }

        private void OnEnable()
        {
            GameEvents.OnActionQueued += HandleActionQueued;
            GameEvents.OnActionUndone += HandleActionUndone;
            GameEvents.OnPlanningTimeExpired += HandlePlanningTimeExpired;
            GameEvents.OnStepResolved += HandleStepResolved;
            GameEvents.OnArmorChanged += HandleArmorChanged;
            GameEvents.OnHeroEliminated += HandleHeroEliminated;
            GameEvents.OnMatchStarted += HandleMatchStarted;
            GameEvents.OnMatchEnded += HandleMatchEnded;
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
            GameEvents.OnDangerZoneExpanded += HandleDangerZoneExpanded;
        }

        private void OnDisable()
        {
            GameEvents.OnActionQueued -= HandleActionQueued;
            GameEvents.OnActionUndone -= HandleActionUndone;
            GameEvents.OnPlanningTimeExpired -= HandlePlanningTimeExpired;
            GameEvents.OnStepResolved -= HandleStepResolved;
            GameEvents.OnArmorChanged -= HandleArmorChanged;
            GameEvents.OnHeroEliminated -= HandleHeroEliminated;
            GameEvents.OnMatchStarted -= HandleMatchStarted;
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
            GameEvents.OnDangerZoneExpanded -= HandleDangerZoneExpanded;
        }

        private void Update()
        {
            if (!_crossfading) return;

            _crossfadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_crossfadeTimer / _crossfadeDuration);

            var fadeOut = _usingSourceA ? _musicSourceB : _musicSource;
            var fadeIn = _usingSourceA ? _musicSource : _musicSourceB;

            float targetVol = _masterVolume * _musicVolume;
            fadeIn.volume = Mathf.Lerp(0f, targetVol, t);
            fadeOut.volume = Mathf.Lerp(targetVol, 0f, t);

            if (t >= 1f)
            {
                _crossfading = false;
                fadeOut.Stop();
                fadeOut.clip = null;
            }
        }

        #endregion

        #region Public API — Core

        /// <summary>
        /// Play a one-shot SFX. Null clips are silently ignored.
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume);
        }

        /// <summary>
        /// Crossfade to a new music track. Null clip stops music.
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                StopMusic();
                return;
            }

            var incoming = _usingSourceA ? _musicSource : _musicSourceB;
            var outgoing = _usingSourceA ? _musicSourceB : _musicSource;

            // If nothing is playing, just start immediately
            if (!outgoing.isPlaying && !incoming.isPlaying)
            {
                incoming.clip = clip;
                incoming.volume = _masterVolume * _musicVolume;
                incoming.Play();
                return;
            }

            // If same clip is already playing on the active source, do nothing
            var activeSource = _usingSourceA ? _musicSource : _musicSourceB;
            if (activeSource.isPlaying && activeSource.clip == clip) return;

            // Crossfade
            _usingSourceA = !_usingSourceA;
            var fadeIn = _usingSourceA ? _musicSource : _musicSourceB;
            fadeIn.clip = clip;
            fadeIn.volume = 0f;
            fadeIn.Play();

            _crossfadeTimer = 0f;
            _crossfading = true;
        }

        /// <summary>
        /// Stop all music immediately.
        /// </summary>
        public void StopMusic()
        {
            _crossfading = false;
            _musicSource.Stop();
            _musicSource.clip = null;
            _musicSourceB.Stop();
            _musicSourceB.clip = null;
        }

        public void SetMusicVolume(float vol)
        {
            _musicVolume = Mathf.Clamp01(vol);
            if (!_crossfading)
            {
                var active = _usingSourceA ? _musicSource : _musicSourceB;
                active.volume = _masterVolume * _musicVolume;
            }
        }

        public void SetSFXVolume(float vol)
        {
            _sfxVolume = Mathf.Clamp01(vol);
            _sfxSource.volume = _masterVolume * _sfxVolume;
        }

        public void SetMasterVolume(float vol)
        {
            _masterVolume = Mathf.Clamp01(vol);
            SetMusicVolume(_musicVolume);
            SetSFXVolume(_sfxVolume);
        }

        #endregion

        #region Convenience — UI

        public void PlayButtonClick() => PlaySFX(buttonClick);
        public void PlayMenuOpen() => PlaySFX(menuOpen);
        public void PlayMenuClose() => PlaySFX(menuClose);
        public void PlayConfirm() => PlaySFX(confirm);
        public void PlayCancel() => PlaySFX(cancel);

        #endregion

        #region Convenience — Planning

        public void PlayActionQueued() => PlaySFX(actionQueued);
        public void PlayActionUndone() => PlaySFX(actionUndone);
        public void PlayTimerPulse() => PlaySFX(timerPulse);
        public void PlayTimerTicking() => PlaySFX(timerTicking);
        public void PlayTimeExpired() => PlaySFX(timeExpired);

        #endregion

        #region Convenience — Combat

        public void PlayShoot() => PlaySFX(shoot);
        public void PlayHit() => PlaySFX(hitLand);
        public void PlayArmorBreak() => PlaySFX(armorBreak);
        public void PlayMutualCancel() => PlaySFX(mutualCancel);
        public void PlayElimination() => PlaySFX(elimination);
        public void PlayDangerZoneWarn() => PlaySFX(dangerZoneWarn);

        #endregion

        #region Convenience — Hero Movement

        public void PlayHeroMove() => PlaySFX(heroMove);
        public void PlayHeroTurn() => PlaySFX(heroTurn);

        #endregion

        #region Convenience — Feedback

        public void PlayVictory() => PlaySFX(victory);
        public void PlayDefeat() => PlaySFX(defeat);
        public void PlayRoundComplete() => PlaySFX(roundComplete);

        #endregion

        #region Convenience — Music

        public void PlayMenuMusic() => PlayMusic(menuMusic);
        public void PlayHeroSelectMusic() => PlayMusic(heroSelectMusic);
        public void PlayPlanningMusic() => PlayMusic(planningMusic);
        public void PlayExecutionMusic() => PlayMusic(executionMusic);
        public void PlayVictoryMusic() => PlayMusic(victoryMusic);
        public void PlayDefeatMusic() => PlayMusic(defeatMusic);

        /// <summary>
        /// Called by GameManager after determining the local player's result.
        /// </summary>
        public void PlayMatchEndMusic(bool isVictory)
        {
            if (isVictory)
                PlayVictoryMusic();
            else
                PlayDefeatMusic();
        }

        #endregion

        #region Event Handlers

        private void HandleActionQueued(int slotIndex, ActionType action)
        {
            PlayActionQueued();
        }

        private void HandleActionUndone(int slotIndex)
        {
            PlayActionUndone();
        }

        private void HandlePlanningTimeExpired()
        {
            PlaySFX(timeExpired);
        }

        private void HandleStepResolved(StepResult result)
        {
            if (result == null) return;

            // Shots fired
            if (result.P1Fired || result.P2Fired)
                PlayShoot();

            // Hits landed
            if (result.P1Hit || result.P2Hit)
                PlayHit();

            // Mutual cancel
            if (result.MutualCancel)
                PlayMutualCancel();

            // Armor broken
            if (result.P1ArmorBroken || result.P2ArmorBroken)
                PlayArmorBreak();

            // Eliminations
            if (result.P1Eliminated || result.P2Eliminated)
                PlayElimination();

            // Movement sounds
            if (result.P1Action == ActionType.Move || result.P2Action == ActionType.Move)
                PlayHeroMove();

            if (result.P1Action == ActionType.TurnLeft || result.P1Action == ActionType.TurnRight ||
                result.P1Action == ActionType.TurnAround ||
                result.P2Action == ActionType.TurnLeft || result.P2Action == ActionType.TurnRight ||
                result.P2Action == ActionType.TurnAround)
                PlayHeroTurn();
        }

        private void HandleArmorChanged(int playerIndex, bool hasArmor)
        {
            if (!hasArmor)
                PlayArmorBreak();
        }

        private void HandleHeroEliminated(int playerIndex)
        {
            PlayElimination();
        }

        private void HandleMatchStarted(MatchStartData data)
        {
            PlayPlanningMusic();
        }

        private void HandleMatchEnded(MatchResult result)
        {
            // Music is now handled by GameManager via PlayMatchEndMusic(),
            // which knows whether the local player won or lost.
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Matchmaking:
                    PlayMenuMusic();
                    break;
                case GamePhase.HeroSelect:
                    PlayHeroSelectMusic();
                    break;
                case GamePhase.Planning:
                    PlayPlanningMusic();
                    break;
                case GamePhase.Execution:
                    PlayExecutionMusic();
                    break;
                case GamePhase.PostRound:
                    PlaySFX(roundComplete);
                    break;
                case GamePhase.PostMatch:
                    // Music handled by HandleMatchEnded
                    break;
            }
        }

        private void HandleDangerZoneExpanded(Vector2Int[] tiles)
        {
            PlaySFX(dangerZoneWarn);
        }

        #endregion

        #region Auto-Load from Resources

        /// <summary>
        /// Loads AudioClips from Resources/Audio/SFX and Resources/Audio/Music
        /// for any field that is still null. File names must match the expected names.
        /// </summary>
        private void AutoLoadClips()
        {
            // UI
            buttonClick = LoadIfNull(buttonClick, "Audio/SFX/sfx_ui_click");
            menuOpen = LoadIfNull(menuOpen, "Audio/SFX/sfx_ui_navigate");
            menuClose = LoadIfNull(menuClose, "Audio/SFX/sfx_ui_navigate");
            confirm = LoadIfNull(confirm, "Audio/SFX/sfx_ui_confirm");
            cancel = LoadIfNull(cancel, "Audio/SFX/sfx_ui_cancel");

            // Planning
            actionQueued = LoadIfNull(actionQueued, "Audio/SFX/sfx_ui_queue");
            actionUndone = LoadIfNull(actionUndone, "Audio/SFX/sfx_ui_undo");
            timerPulse = LoadIfNull(timerPulse, "Audio/SFX/sfx_ui_countdown_tick");
            timerTicking = LoadIfNull(timerTicking, "Audio/SFX/sfx_ui_countdown_tick");
            timeExpired = LoadIfNull(timeExpired, "Audio/SFX/sfx_ui_countdown_tick");

            // Combat
            shoot = LoadIfNull(shoot, "Audio/SFX/sfx_shoot");
            hitLand = LoadIfNull(hitLand, "Audio/SFX/sfx_hit");
            armorBreak = LoadIfNull(armorBreak, "Audio/SFX/sfx_armor_break");
            mutualCancel = LoadIfNull(mutualCancel, "Audio/SFX/sfx_mutual_cancel");
            elimination = LoadIfNull(elimination, "Audio/SFX/sfx_death");

            // Movement
            heroMove = LoadIfNull(heroMove, "Audio/SFX/sfx_step");
            heroTurn = LoadIfNull(heroTurn, "Audio/SFX/sfx_turn");

            // Feedback
            victory = LoadIfNull(victory, "Audio/SFX/sfx_victory");
            defeat = LoadIfNull(defeat, "Audio/SFX/sfx_defeat");
            roundComplete = LoadIfNull(roundComplete, "Audio/SFX/sfx_round_end");

            // Music
            menuMusic = LoadIfNull(menuMusic, "Audio/Music/music_menu");
            heroSelectMusic = LoadIfNull(heroSelectMusic, "Audio/Music/music_menu");
            planningMusic = LoadIfNull(planningMusic, "Audio/Music/music_planning");
            executionMusic = LoadIfNull(executionMusic, "Audio/Music/music_execution");
            victoryMusic = LoadIfNull(victoryMusic, "Audio/SFX/sfx_victory");
            defeatMusic = LoadIfNull(defeatMusic, "Audio/SFX/sfx_defeat");

            int loaded = 0;
            if (buttonClick) loaded++; if (shoot) loaded++; if (menuMusic) loaded++;
            if (planningMusic) loaded++; if (executionMusic) loaded++;
            Debug.Log($"[AudioManager] Auto-loaded clips from Resources/Audio ({loaded}/5 key clips found)");
        }

        private static AudioClip LoadIfNull(AudioClip current, string resourcePath)
        {
            if (current != null) return current;
            return Resources.Load<AudioClip>(resourcePath);
        }

        #endregion
    }
}
