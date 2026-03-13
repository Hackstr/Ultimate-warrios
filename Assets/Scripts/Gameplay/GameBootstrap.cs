using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalDuelist.UI;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Runtime bootstrapper. Creates the full UI hierarchy, gameplay objects,
    /// and wires all serialized references programmatically.
    /// All screens are mobile portrait-first (1080x1920 reference).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Game Data (wired by BootstrapSceneCreator)")]
        [SerializeField] private HeroConfig[] _heroes;
        [SerializeField] private MapConfig _defaultMap;

        [Header("Grid Materials (assign via Tactical Duelist > Setup Project)")]
        [SerializeField] private Material _matFloor;
        [SerializeField] private Material _matFloorAlt;
        [SerializeField] private Material _matWall;
        [SerializeField] private Material _matDangerZone;
        [SerializeField] private Material _matHighlightMove;
        [SerializeField] private Material _matHighlightShoot;
        [SerializeField] private Material _matSpawnP1;
        [SerializeField] private Material _matSpawnP2;

        [Header("Hero Materials")]
        [SerializeField] private Material _matHeroP1;
        [SerializeField] private Material _matHeroP2;

        #endregion

        #region Constants

        private const BindingFlags FIELD_FLAGS =
            BindingFlags.NonPublic | BindingFlags.Instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Debug.Log("[GameBootstrap] Starting runtime UI construction...");

            // Ensure PlatformBootstrap runs first (registers IPlatformAuth etc.)
            EnsurePlatformBootstrap();

            UIFactory.CreateEventSystem();
            var canvas = UIFactory.CreateCanvas("GameCanvas");
            var root = canvas.transform;

            var mainMenu = BuildMainMenu(root);
            var heroSelect = BuildHeroSelect(root);
            var matchmaking = BuildMatchmaking(root);
            var planning = BuildPlanning(root);
            var result = BuildResult(root);
            var hud = BuildHUD(root);

            var gridView = new GameObject("GridView").AddComponent<GridView>();
            WireGridViewMaterials(gridView);
            var execCtrl = new GameObject("ExecutionController").AddComponent<ExecutionController>();

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<Camera>();
            }
            var cameraCtrl = cam.gameObject.GetComponent<CameraController>();
            if (cameraCtrl == null)
                cameraCtrl = cam.gameObject.AddComponent<CameraController>();

            HeroView3D hero1View = null;
            HeroView3D hero2View = null;
            try
            {
                hero1View = CreatePlaceholderHero("Hero_P1", _matHeroP1);
                hero2View = CreatePlaceholderHero("Hero_P2", _matHeroP2);
                hero1View.gameObject.SetActive(false);
                hero2View.gameObject.SetActive(false);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameBootstrap] 3D heroes failed (non-fatal): {ex.Message}");
            }

            if (hero1View != null) Wire(execCtrl, "_hero1View", hero1View);
            if (hero2View != null) Wire(execCtrl, "_hero2View", hero2View);
            Wire(execCtrl, "_cameraController", cameraCtrl);
            Wire(execCtrl, "_gridView", gridView);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.SetupRuntime(
                mainMenu, heroSelect, matchmaking,
                planning, result, hud,
                execCtrl, gridView, cameraCtrl,
                _defaultMap);

            Debug.Log("[GameBootstrap] UI construction complete.");
        }

        #endregion

        #region Screen Builders — Main Menu

        private MainMenuScreen BuildMainMenu(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "MainMenuScreen", UIFactory.BgDark);
            var screen = panel.gameObject.AddComponent<MainMenuScreen>();

            var layout = UIFactory.AddVertical(panel.gameObject, 0f,
                new RectOffset(60, 60, 0, 40));
            layout.childAlignment = TextAnchor.MiddleCenter;

            UIFactory.CreateSpacer(panel, 300f);

            var title = UIFactory.CreateText(panel, "Title", "TACTICAL\nDUELIST",
                56f, TextAlignmentOptions.Center, UIFactory.AccentGold);
            UIFactory.AddLayoutElement(title.gameObject, prefH: 160f);
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = -10f;

            var subtitle = UIFactory.CreateText(panel, "Subtitle", "Simultaneous-Turn Tactical Game",
                20f, TextAlignmentOptions.Center, UIFactory.TextGray);
            UIFactory.AddLayoutElement(subtitle.gameObject, prefH: 36f);

            UIFactory.CreateSpacer(panel, 120f);

            var (offlineBtn, offlineLbl) = UIFactory.CreateButton(panel, "PlayOfflineButton",
                "PLAY OFFLINE", UIFactory.AccentPrimary, null, 600f, 80f);
            UIFactory.AddLayoutElement(offlineBtn.gameObject, prefH: 80f, prefW: 600f);
            offlineLbl.fontSize = 28f;
            offlineLbl.fontStyle = FontStyles.Bold;

            UIFactory.CreateSpacer(panel, 16f);

            var (onlineBtn, onlineLbl) = UIFactory.CreateButton(panel, "PlayOnlineButton",
                "PLAY ONLINE", UIFactory.AccentBlue, null, 600f, 80f);
            UIFactory.AddLayoutElement(onlineBtn.gameObject, prefH: 80f, prefW: 600f);
            onlineLbl.fontSize = 28f;
            onlineLbl.fontStyle = FontStyles.Bold;

            var flexSpacer = UIFactory.CreateContainer(panel, "FlexSpacer");
            UIFactory.AddLayoutElement(flexSpacer.gameObject, flexH: true);

            var version = UIFactory.CreateText(panel, "Version", "v0.1.0 — Pre-Alpha",
                16f, TextAlignmentOptions.Center, UIFactory.TextGray);
            UIFactory.AddLayoutElement(version.gameObject, prefH: 30f);

            Wire(screen, "_titleText", title);
            Wire(screen, "_playOfflineButton", offlineBtn);
            Wire(screen, "_playOnlineButton", onlineBtn);

            return screen;
        }

        #endregion

        #region Screen Builders — Hero Select

        private HeroSelectScreen BuildHeroSelect(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "HeroSelectScreen", UIFactory.BgDark);
            var screen = panel.gameObject.AddComponent<HeroSelectScreen>();

            var mainLayout = UIFactory.AddVertical(panel.gameObject, 0f,
                new RectOffset(0, 0, 0, 0));
            mainLayout.childForceExpandWidth = true;
            mainLayout.childControlWidth = true;

            // ── Header bar (top ~8%) ──
            var header = UIFactory.CreatePanel(panel, "Header", UIFactory.PanelBg);
            UIFactory.AddLayoutElement(header.gameObject, prefH: 100f, minH: 80f);
            var headerH = UIFactory.AddHorizontal(header.gameObject, 12f,
                new RectOffset(20, 20, 16, 16));
            headerH.childAlignment = TextAnchor.MiddleCenter;
            headerH.childForceExpandWidth = false;
            headerH.childControlWidth = false;
            headerH.childControlHeight = false;

            var (backBtn, backLbl) = UIFactory.CreateButton(header, "BackButton", "< BACK",
                UIFactory.ButtonBg, null, 160f, 56f);
            backLbl.fontSize = 22f;

            var titleText = UIFactory.CreateText(header, "Title", "CHOOSE YOUR HERO",
                30f, TextAlignmentOptions.Center, UIFactory.TextWhite);
            titleText.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(titleText.gameObject, flexW: true, prefH: 56f);

            var headerSpacer = UIFactory.CreateContainer(header, "Spacer");
            headerSpacer.sizeDelta = new Vector2(160f, 56f);

            // ── Hero card scroll row — visible background so users see the scroll area ──
            var cardSection = UIFactory.CreatePanel(panel, "CardSection",
                new Color(0.08f, 0.08f, 0.12f, 1f));
            UIFactory.AddLayoutElement(cardSection.gameObject, prefH: 160f, minH: 140f);

            var (cardScroll, cardContent) = UIFactory.CreateScrollView(
                cardSection, "CardScroll", horizontal: true, vertical: false);
            UIFactory.Stretch(cardScroll.GetComponent<RectTransform>());

            var cardHLG = cardContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardHLG.spacing = 12f;
            cardHLG.padding = new RectOffset(16, 16, 10, 10);
            cardHLG.childAlignment = TextAnchor.MiddleCenter;
            cardHLG.childForceExpandWidth = false;
            cardHLG.childForceExpandHeight = false;
            cardHLG.childControlWidth = false;
            cardHLG.childControlHeight = false;

            var cardFitter = cardContent.gameObject.AddComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            // ── Info panel (center, flexible ~50%) ──
            var infoPanel = UIFactory.CreatePanel(panel, "InfoPanel", UIFactory.PanelBg);
            UIFactory.AddLayoutElement(infoPanel.gameObject, flexH: true, minH: 400f);
            var infoLayout = UIFactory.AddVertical(infoPanel.gameObject, 8f,
                new RectOffset(40, 40, 24, 24));
            infoLayout.childAlignment = TextAnchor.UpperCenter;

            var heroName = UIFactory.CreateText(infoPanel, "HeroName", "--",
                40f, TextAlignmentOptions.Center, UIFactory.AccentGold);
            heroName.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(heroName.gameObject, prefH: 52f);

            var diffDots = UIFactory.CreateDifficultyDots(infoPanel, "Difficulty", 3, 5, 20f, 8f);
            UIFactory.AddLayoutElement(diffDots.gameObject, prefH: 30f);

            UIFactory.CreateSpacer(infoPanel, 4f);

            var specialName = UIFactory.CreateText(infoPanel, "SpecialName", "Special: --",
                24f, TextAlignmentOptions.Left, UIFactory.AccentBlue);
            specialName.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(specialName.gameObject, prefH: 34f);

            var specialDesc = UIFactory.CreateText(infoPanel, "SpecialDesc", "",
                18f, TextAlignmentOptions.Left, UIFactory.TextGray);
            specialDesc.enableWordWrapping = true;
            UIFactory.AddLayoutElement(specialDesc.gameObject, prefH: 50f);

            UIFactory.CreateSpacer(infoPanel, 8f);

            // Stat bars — full width
            var (stepsBar, stepsVal) = BuildStatRow(infoPanel, "Steps", UIFactory.AccentBlue);
            var (rangeBar, rangeVal) = BuildStatRow(infoPanel, "Range", UIFactory.AccentGreen);
            var (cdBar, cdVal) = BuildStatRow(infoPanel, "Cooldown", UIFactory.AccentRed);
            var (armorBar, armorVal) = BuildStatRow(infoPanel, "Armor", UIFactory.AccentGold);
            var (speedBar, speedVal) = BuildStatRow(infoPanel, "Speed", new Color(0.5f, 0.3f, 0.8f));

            UIFactory.CreateSpacer(infoPanel, 8f);

            // 3D preview placeholder
            var previewArea = UIFactory.CreatePanel(infoPanel, "PreviewArea",
                new Color(0.08f, 0.08f, 0.12f));
            UIFactory.AddLayoutElement(previewArea.gameObject, prefH: 160f, flexH: true);
            var previewLabel = UIFactory.CreateText(previewArea, "Label", "3D PREVIEW",
                18f, TextAlignmentOptions.Center, UIFactory.TextGray);
            UIFactory.Stretch(previewLabel.GetComponent<RectTransform>());

            // ── Bottom: SELECT button (~10%) ──
            var bottomBar = UIFactory.CreatePanel(panel, "BottomBar", new Color(0.08f, 0.08f, 0.12f));
            UIFactory.AddLayoutElement(bottomBar.gameObject, prefH: 120f, minH: 100f);
            var bottomLayout = UIFactory.AddVertical(bottomBar.gameObject, 0f,
                new RectOffset(60, 60, 16, 24));
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;

            var (selectBtn, selectBtnText) = UIFactory.CreateButton(bottomBar, "SelectButton",
                "SELECT", UIFactory.AccentPrimary, null, 600f, 72f);
            selectBtnText.fontSize = 28f;
            selectBtnText.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(selectBtn.gameObject, prefH: 72f, prefW: 600f);

            // ── Pass device overlay (ignoreLayout so VLG doesn't override stretch) ──
            var passOverlay = UIFactory.CreatePanel(panel, "PassDeviceOverlay",
                new Color(0, 0, 0, 0.85f));
            UIFactory.Stretch(passOverlay);
            passOverlay.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var passVL = UIFactory.AddVertical(passOverlay.gameObject, 24f);
            passVL.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.CreateSpacer(passOverlay, 400f);
            var passMsg = UIFactory.CreateText(passOverlay.transform, "PassMsg",
                "Pass the device\nto the other player",
                32f, TextAlignmentOptions.Center, UIFactory.TextWhite);
            UIFactory.AddLayoutElement(passMsg.gameObject, prefH: 100f);
            var (passTapBtn, passTapLbl) = UIFactory.CreateButton(passOverlay.transform, "TapToContinue",
                "TAP TO CONTINUE", UIFactory.AccentPrimary, null, 500f, 72f);
            passTapLbl.fontSize = 24f;
            passTapLbl.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(passTapBtn.gameObject, prefH: 72f, prefW: 500f);
            passOverlay.gameObject.SetActive(false);

            // ── Hero card prefab template (ignoreLayout — inactive template) ──
            var cardPrefab = BuildHeroCardTemplate(panel);
            var cardLE = cardPrefab.AddComponent<LayoutElement>();
            cardLE.ignoreLayout = true;
            cardLE.preferredWidth = 120f;
            cardLE.preferredHeight = 120f;
            cardLE.minWidth = 100f;
            cardLE.minHeight = 100f;

            // ── Wire fields ──
            Wire(screen, "_availableHeroes", _heroes);
            Wire(screen, "_titleText", titleText);
            Wire(screen, "_backButton", backBtn);
            Wire(screen, "_heroCardContainer", cardContent.transform);
            Wire(screen, "_heroCardPrefab", cardPrefab);
            Wire(screen, "_heroNameText", heroName);
            Wire(screen, "_difficultyText", (TextMeshProUGUI)null);
            Wire(screen, "_difficultyDotsContainer", diffDots);
            Wire(screen, "_specialNameText", specialName);
            Wire(screen, "_specialDescText", specialDesc);
            Wire(screen, "_stepsBar", stepsBar);
            Wire(screen, "_rangeBar", rangeBar);
            Wire(screen, "_cooldownBar", cdBar);
            Wire(screen, "_armorBar", armorBar);
            Wire(screen, "_speedBar", speedBar);
            Wire(screen, "_stepsValueText", stepsVal);
            Wire(screen, "_rangeValueText", rangeVal);
            Wire(screen, "_cooldownValueText", cdVal);
            Wire(screen, "_armorValueText", armorVal);
            Wire(screen, "_speedValueText", speedVal);
            Wire(screen, "_previewSpawnPoint", previewArea.transform);
            Wire(screen, "_selectButton", selectBtn);
            Wire(screen, "_selectButtonText", selectBtnText);
            Wire(screen, "_passDeviceOverlay", passOverlay.gameObject);
            Wire(screen, "_passDeviceTapButton", passTapBtn);

            panel.gameObject.SetActive(false);
            return screen;
        }

        #endregion

        #region Screen Builders — Matchmaking

        private MatchmakingScreen BuildMatchmaking(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "MatchmakingScreen", UIFactory.BgDark);
            var screen = panel.gameObject.AddComponent<MatchmakingScreen>();

            var layout = UIFactory.AddVertical(panel.gameObject, 24f,
                new RectOffset(60, 60, 0, 80));
            layout.childAlignment = TextAnchor.MiddleCenter;

            UIFactory.CreateSpacer(panel, 500f);

            var status = UIFactory.CreateText(panel, "StatusText", "Finding opponent...",
                32f, TextAlignmentOptions.Center);
            UIFactory.AddLayoutElement(status.gameObject, prefH: 50f);

            var timer = UIFactory.CreateText(panel, "TimerText", "0:00",
                24f, TextAlignmentOptions.Center, UIFactory.TextGray);
            UIFactory.AddLayoutElement(timer.gameObject, prefH: 36f);

            UIFactory.CreateSpacer(panel, 60f);

            var (cancelBtn, cancelLbl) = UIFactory.CreateButton(panel, "CancelButton",
                "CANCEL", UIFactory.AccentRed, null, 400f, 72f);
            cancelLbl.fontSize = 28f;
            UIFactory.AddLayoutElement(cancelBtn.gameObject, prefH: 72f, prefW: 400f);

            Wire(screen, "_statusText", status);
            Wire(screen, "_timerText", timer);
            Wire(screen, "_cancelButton", cancelBtn);

            panel.gameObject.SetActive(false);
            return screen;
        }

        #endregion

        #region Screen Builders — Planning

        private PlanningScreen BuildPlanning(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "PlanningScreen", Color.clear);
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var screen = panel.gameObject.AddComponent<PlanningScreen>();

            var mainLayout = UIFactory.AddVertical(panel.gameObject, 0f,
                new RectOffset(0, 0, 0, 0));
            mainLayout.childForceExpandWidth = true;
            mainLayout.childControlWidth = true;

            // ── Transparent gap — 3D grid visible (HUD is shown separately) ──
            var gridWindow = UIFactory.CreateContainer(panel, "GridWindow");
            UIFactory.AddLayoutElement(gridWindow.gameObject, flexH: true, minH: 200f);

            // ── Hidden player label (used by PlanningScreen logic, not displayed) ──
            var playerLabel = UIFactory.CreateText(panel, "PlayerLabel", "",
                1f, TextAlignmentOptions.Left, Color.clear);
            playerLabel.gameObject.SetActive(false);

            // ── Bottom panel — timer + queue + buttons ──
            var bottomPanel = UIFactory.CreatePanel(panel, "BottomPanel",
                new Color(0.05f, 0.05f, 0.08f, 0.95f));
            UIFactory.AddLayoutElement(bottomPanel.gameObject, prefH: 360f, minH: 320f);
            var bottomVL = UIFactory.AddVertical(bottomPanel.gameObject, 6f,
                new RectOffset(12, 12, 8, 12));
            bottomVL.childForceExpandWidth = true;
            bottomVL.childControlWidth = true;

            // ── Timer + Queue row ──
            var timerQueueRow = UIFactory.CreateContainer(bottomPanel, "TimerQueueRow");
            UIFactory.AddLayoutElement(timerQueueRow.gameObject, prefH: 52f, minH: 48f);
            var tqH = UIFactory.AddHorizontal(timerQueueRow.gameObject, 8f,
                new RectOffset(4, 4, 0, 0));
            tqH.childAlignment = TextAnchor.MiddleCenter;
            tqH.childForceExpandWidth = false;
            tqH.childControlWidth = false;
            tqH.childForceExpandHeight = false;

            // Timer badge
            var timerBg = UIFactory.CreateImage(timerQueueRow, "TimerBg",
                new Color(0.8f, 0.3f, 0.2f, 0.9f), 48f, 48f);
            var timerText = UIFactory.CreateText(timerBg.transform, "TimerText", "30",
                22f, TextAlignmentOptions.Center, UIFactory.TextWhite);
            timerText.fontStyle = FontStyles.Bold;
            UIFactory.Stretch(timerText.GetComponent<RectTransform>());
            UIFactory.AddLayoutElement(timerBg.gameObject, prefW: 48f, prefH: 48f, minW: 44f);

            // Queue strip — horizontal slots (flexible width)
            var queueStrip = UIFactory.CreateContainer(timerQueueRow, "QueueStrip");
            UIFactory.AddLayoutElement(queueStrip.gameObject, flexW: true, prefH: 48f);
            var queueH = UIFactory.AddHorizontal(queueStrip.gameObject, 6f,
                new RectOffset(4, 4, 0, 0));
            queueH.childAlignment = TextAnchor.MiddleCenter;
            queueH.childForceExpandWidth = true;
            queueH.childControlWidth = true;
            queueH.childForceExpandHeight = false;

            // ── Action buttons ──
            // Row 1: Move, Turn Left, Turn Right, Turn Around
            var row1 = UIFactory.CreateContainer(bottomPanel, "Row1");
            UIFactory.AddLayoutElement(row1.gameObject, prefH: 56f);
            var row1H = UIFactory.AddHorizontal(row1.gameObject, 6f,
                new RectOffset(0, 0, 0, 0));
            row1H.childAlignment = TextAnchor.MiddleCenter;
            row1H.childForceExpandWidth = true;
            row1H.childControlWidth = true;
            row1H.childControlHeight = false;

            var (moveBtn, moveLbl) = UIFactory.CreateButton(row1, "MoveBtn", "MOVE",
                new Color(0.15f, 0.35f, 0.55f, 1f), null, 0f, 52f);
            moveLbl.fontSize = 18f;
            moveLbl.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(moveBtn.gameObject, flexW: true, prefH: 52f);

            var (turnLBtn, turnLLbl) = UIFactory.CreateButton(row1, "TurnLBtn", "< TURN",
                new Color(0.35f, 0.35f, 0.15f, 1f), null, 0f, 52f);
            turnLLbl.fontSize = 16f;
            UIFactory.AddLayoutElement(turnLBtn.gameObject, flexW: true, prefH: 52f);

            var (turnRBtn, turnRLbl) = UIFactory.CreateButton(row1, "TurnRBtn", "TURN >",
                new Color(0.35f, 0.35f, 0.15f, 1f), null, 0f, 52f);
            turnRLbl.fontSize = 16f;
            UIFactory.AddLayoutElement(turnRBtn.gameObject, flexW: true, prefH: 52f);

            var (turnABtn, turnALbl) = UIFactory.CreateButton(row1, "TurnABtn", "180",
                new Color(0.35f, 0.35f, 0.15f, 1f), null, 0f, 52f);
            turnALbl.fontSize = 18f;
            turnALbl.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(turnABtn.gameObject, flexW: true, prefH: 52f);

            // Row 2: Shoot, Wait, Special
            var row2 = UIFactory.CreateContainer(bottomPanel, "Row2");
            UIFactory.AddLayoutElement(row2.gameObject, prefH: 56f);
            var row2H = UIFactory.AddHorizontal(row2.gameObject, 6f,
                new RectOffset(0, 0, 0, 0));
            row2H.childAlignment = TextAnchor.MiddleCenter;
            row2H.childForceExpandWidth = true;
            row2H.childControlWidth = true;
            row2H.childControlHeight = false;

            var (shootBtn, shootLbl) = UIFactory.CreateButton(row2, "ShootBtn", "SHOOT",
                UIFactory.AccentRed, null, 0f, 52f);
            shootLbl.fontSize = 20f;
            shootLbl.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(shootBtn.gameObject, flexW: true, prefH: 52f);

            var (waitBtn, waitLbl) = UIFactory.CreateButton(row2, "WaitBtn", "WAIT",
                new Color(0.25f, 0.25f, 0.30f, 1f), null, 0f, 52f);
            waitLbl.fontSize = 18f;
            UIFactory.AddLayoutElement(waitBtn.gameObject, flexW: true, prefH: 52f);

            var (specialBtn, specialLbl) = UIFactory.CreateButton(row2, "SpecialBtn", "SPECIAL",
                new Color(0.45f, 0.20f, 0.60f, 1f), null, 0f, 52f);
            specialLbl.fontSize = 20f;
            specialLbl.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(specialBtn.gameObject, flexW: true, prefH: 52f);

            // Shoot cooldown overlay
            var shootCdOverlay = UIFactory.CreatePanel(shootBtn.transform, "CooldownOverlay",
                new Color(0, 0, 0, 0.7f));
            var shootCdText = UIFactory.CreateText(shootCdOverlay, "CdText", "2",
                20f, TextAlignmentOptions.Center, UIFactory.AccentRed);
            UIFactory.Stretch(shootCdText.GetComponent<RectTransform>());
            shootCdOverlay.gameObject.SetActive(false);

            // Special lock overlay
            var specialLockOverlay = UIFactory.CreatePanel(specialBtn.transform, "LockOverlay",
                new Color(0, 0, 0, 0.7f));
            UIFactory.CreateText(specialLockOverlay, "LockIcon", "LOCKED",
                14f, TextAlignmentOptions.Center, UIFactory.TextGray);
            specialLockOverlay.gameObject.SetActive(false);

            // Row 3: Undo + Confirm
            var row3 = UIFactory.CreateContainer(bottomPanel, "Row3");
            UIFactory.AddLayoutElement(row3.gameObject, prefH: 60f);
            var row3H = UIFactory.AddHorizontal(row3.gameObject, 12f,
                new RectOffset(0, 0, 0, 0));
            row3H.childAlignment = TextAnchor.MiddleCenter;
            row3H.childForceExpandWidth = true;
            row3H.childControlWidth = true;
            row3H.childControlHeight = false;

            var (undoBtn, undoLbl) = UIFactory.CreateButton(row3, "UndoBtn", "UNDO",
                UIFactory.ButtonBg, null, 0f, 56f);
            undoLbl.fontSize = 20f;
            UIFactory.AddLayoutElement(undoBtn.gameObject, flexW: true, prefH: 56f);

            var (confirmBtn, confirmText) = UIFactory.CreateButton(row3, "ConfirmBtn",
                "CONFIRM", UIFactory.AccentPrimary, null, 0f, 56f);
            confirmText.fontSize = 22f;
            confirmText.fontStyle = FontStyles.Bold;
            var confirmLE = UIFactory.AddLayoutElement(confirmBtn.gameObject, prefH: 56f);
            confirmLE.flexibleWidth = 2f;

            // ── Pass device overlay (ignoreLayout so VLG doesn't override stretch) ──
            var planPassOverlay = UIFactory.CreatePanel(panel, "PlanPassDeviceOverlay",
                new Color(0, 0, 0, 0.85f));
            UIFactory.Stretch(planPassOverlay);
            planPassOverlay.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var planPassVL = UIFactory.AddVertical(planPassOverlay.gameObject, 24f);
            planPassVL.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.CreateSpacer(planPassOverlay, 400f);
            var planPassMsg = UIFactory.CreateText(planPassOverlay.transform, "PassMsg",
                "Pass the device\nto Player 2",
                32f, TextAlignmentOptions.Center, UIFactory.TextWhite);
            UIFactory.AddLayoutElement(planPassMsg.gameObject, prefH: 100f);
            var (planPassTapBtn, planPassTapLbl) = UIFactory.CreateButton(
                planPassOverlay.transform, "TapToContinue",
                "TAP TO CONTINUE", UIFactory.AccentPrimary, null, 500f, 72f);
            planPassTapLbl.fontSize = 24f;
            planPassTapLbl.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(planPassTapBtn.gameObject, prefH: 72f, prefW: 500f);
            planPassOverlay.gameObject.SetActive(false);

            // ── Action slot prefab template (ignoreLayout — inactive template) ──
            var slotPrefab = BuildActionSlotTemplate(panel);
            var slotLE = slotPrefab.AddComponent<LayoutElement>();
            slotLE.ignoreLayout = true;
            slotLE.flexibleWidth = 1f;
            slotLE.preferredHeight = 48f;
            slotLE.preferredWidth = 56f;

            // ── Wire fields ──
            Wire(screen, "_moveButton", moveBtn);
            Wire(screen, "_turnLeftButton", turnLBtn);
            Wire(screen, "_turnRightButton", turnRBtn);
            Wire(screen, "_turnAroundButton", turnABtn);
            Wire(screen, "_shootButton", shootBtn);
            Wire(screen, "_waitButton", waitBtn);
            Wire(screen, "_specialButton", specialBtn);
            Wire(screen, "_shootCooldownOverlay", shootCdOverlay.gameObject);
            Wire(screen, "_shootCooldownText", shootCdText);
            Wire(screen, "_specialLockOverlay", specialLockOverlay.gameObject);
            Wire(screen, "_queueContainer", queueStrip.transform);
            Wire(screen, "_queueSlotPrefab", slotPrefab);
            Wire(screen, "_timerText", timerText);
            Wire(screen, "_timerBackground", timerBg);
            Wire(screen, "_confirmButton", confirmBtn);
            Wire(screen, "_undoButton", undoBtn);
            Wire(screen, "_confirmButtonText", confirmText);
            Wire(screen, "_playerLabel", playerLabel);
            Wire(screen, "_passDeviceOverlay", planPassOverlay.gameObject);
            Wire(screen, "_passDeviceTapButton", planPassTapBtn);
            Wire(screen, "_passDeviceMessage", planPassMsg);

            panel.gameObject.SetActive(false);
            return screen;
        }

        #endregion

        #region Screen Builders — Result

        private ResultScreen BuildResult(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "ResultScreen", UIFactory.BgDark);
            var screen = panel.gameObject.AddComponent<ResultScreen>();

            var layout = UIFactory.AddVertical(panel.gameObject, 0f,
                new RectOffset(40, 40, 0, 40));
            layout.childAlignment = TextAnchor.MiddleCenter;

            UIFactory.CreateSpacer(panel, 200f);

            var resultTitle = UIFactory.CreateText(panel, "ResultTitle", "VICTORY",
                52f, TextAlignmentOptions.Center, UIFactory.AccentGold);
            resultTitle.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(resultTitle.gameObject, prefH: 80f);

            var resultSub = UIFactory.CreateText(panel, "ResultSubtitle", "",
                22f, TextAlignmentOptions.Center, UIFactory.TextGray);
            UIFactory.AddLayoutElement(resultSub.gameObject, prefH: 36f);

            UIFactory.CreateSpacer(panel, 40f);

            // Hero portraits row
            var heroRow = UIFactory.CreateContainer(panel, "HeroRow");
            UIFactory.AddLayoutElement(heroRow.gameObject, prefH: 180f);
            var heroRowH = UIFactory.AddHorizontal(heroRow.gameObject, 40f);
            heroRowH.childAlignment = TextAnchor.MiddleCenter;

            // P1 side
            var p1Col = UIFactory.CreateContainer(heroRow, "P1");
            UIFactory.AddVertical(p1Col.gameObject, 8f, new RectOffset(0, 0, 0, 0));
            var p1Portrait = UIFactory.CreateImage(p1Col, "P1Portrait", UIFactory.PanelBg, 120f, 120f);
            var p1Name = UIFactory.CreateText(p1Col, "P1Name", "P1",
                20f, TextAlignmentOptions.Center);
            UIFactory.AddLayoutElement(p1Name.gameObject, prefH: 28f);
            var p1WinFrame = UIFactory.CreateImage(p1Col, "P1WinFrame", UIFactory.AccentGold, 130f, 4f);
            p1WinFrame.gameObject.SetActive(false);

            UIFactory.CreateText(heroRow, "VS", "VS",
                32f, TextAlignmentOptions.Center, UIFactory.TextGray);

            // P2 side
            var p2Col = UIFactory.CreateContainer(heroRow, "P2");
            UIFactory.AddVertical(p2Col.gameObject, 8f, new RectOffset(0, 0, 0, 0));
            var p2Portrait = UIFactory.CreateImage(p2Col, "P2Portrait", UIFactory.PanelBg, 120f, 120f);
            var p2Name = UIFactory.CreateText(p2Col, "P2Name", "P2",
                20f, TextAlignmentOptions.Center);
            UIFactory.AddLayoutElement(p2Name.gameObject, prefH: 28f);
            var p2WinFrame = UIFactory.CreateImage(p2Col, "P2WinFrame", UIFactory.AccentGold, 130f, 4f);
            p2WinFrame.gameObject.SetActive(false);

            UIFactory.CreateSpacer(panel, 24f);

            var scoreText = UIFactory.CreateText(panel, "Score", "0 -- 0",
                40f, TextAlignmentOptions.Center, UIFactory.TextWhite);
            scoreText.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(scoreText.gameObject, prefH: 56f);

            UIFactory.CreateSpacer(panel, 16f);

            // Round details
            var roundContainer = UIFactory.CreateContainer(panel, "RoundDetails");
            UIFactory.AddLayoutElement(roundContainer.gameObject, prefH: 120f, flexH: true);
            UIFactory.AddVertical(roundContainer.gameObject, 8f, new RectOffset(0, 0, 0, 0));

            var roundPrefab = BuildRoundDetailTemplate(panel);
            roundPrefab.AddComponent<LayoutElement>().ignoreLayout = true;

            UIFactory.CreateSpacer(panel, 24f);

            var (rematchBtn, rematchLbl) = UIFactory.CreateButton(panel, "RematchBtn",
                "REMATCH", UIFactory.AccentPrimary, null, 500f, 80f);
            rematchLbl.fontSize = 28f;
            rematchLbl.fontStyle = FontStyles.Bold;
            UIFactory.AddLayoutElement(rematchBtn.gameObject, prefH: 80f, prefW: 500f);

            UIFactory.CreateSpacer(panel, 12f);

            var (menuBtn, menuLbl) = UIFactory.CreateButton(panel, "MainMenuBtn",
                "MAIN MENU", UIFactory.ButtonBgSecondary, null, 500f, 72f);
            menuLbl.fontSize = 24f;
            menuLbl.color = UIFactory.TextLight;
            UIFactory.AddLayoutElement(menuBtn.gameObject, prefH: 72f, prefW: 500f);

            UIFactory.CreateSpacer(panel, 20f);

            // Wire
            Wire(screen, "_resultTitleText", resultTitle);
            Wire(screen, "_resultSubtitleText", resultSub);
            Wire(screen, "_p1Portrait", p1Portrait);
            Wire(screen, "_p2Portrait", p2Portrait);
            Wire(screen, "_p1NameText", p1Name);
            Wire(screen, "_p2NameText", p2Name);
            Wire(screen, "_p1WinnerFrame", p1WinFrame.gameObject);
            Wire(screen, "_p2WinnerFrame", p2WinFrame.gameObject);
            Wire(screen, "_scoreText", scoreText);
            Wire(screen, "_roundDetailContainer", roundContainer.transform);
            Wire(screen, "_roundDetailPrefab", roundPrefab);
            Wire(screen, "_rematchButton", rematchBtn);
            Wire(screen, "_mainMenuButton", menuBtn);

            panel.gameObject.SetActive(false);
            return screen;
        }

        #endregion

        #region Screen Builders — HUD

        private HUD BuildHUD(Transform root)
        {
            var panel = UIFactory.CreateContainer(root, "HUD");
            UIFactory.Stretch(panel);
            var hud = panel.gameObject.AddComponent<HUD>();

            // Top info bar
            var topBar = UIFactory.CreatePanel(panel, "TopBar", new Color(0, 0, 0, 0.7f));
            UIFactory.SetAnchored(topBar, new Vector2(0, 0.92f), Vector2.one,
                Vector2.zero, Vector2.zero);
            var topH = UIFactory.AddHorizontal(topBar.gameObject, 8f,
                new RectOffset(16, 16, 8, 8));
            topH.childAlignment = TextAnchor.MiddleCenter;
            topH.childForceExpandWidth = false;
            topH.childControlWidth = false;

            // P1 info
            var p1Group = UIFactory.CreateContainer(topBar, "P1Info");
            var p1GH = UIFactory.AddHorizontal(p1Group.gameObject, 4f);
            p1GH.childForceExpandWidth = false;
            p1GH.childControlWidth = false;
            p1GH.childControlHeight = false;
            var p1Portrait = UIFactory.CreateImage(p1Group, "P1Portrait", UIFactory.PanelBg, 48f, 48f);
            var p1Col = UIFactory.CreateContainer(p1Group, "P1Text");
            UIFactory.AddVertical(p1Col.gameObject, 4f, new RectOffset(0, 0, 0, 0));
            var p1Name = UIFactory.CreateText(p1Col, "P1Name", "P1",
                18f, TextAlignmentOptions.Left);
            var p1Score = UIFactory.CreateText(p1Col, "P1Score", "0",
                14f, TextAlignmentOptions.Left, UIFactory.TextGray);
            var p1Armor = UIFactory.CreateImage(p1Group, "P1Armor", UIFactory.AccentBlue, 20f, 20f);
            var p1StatusFrame = UIFactory.CreateImage(p1Group, "P1Frame",
                new Color(0.2f, 0.6f, 0.3f), 4f, 48f);

            // Center info
            var centerCol = UIFactory.CreateContainer(topBar, "CenterInfo");
            UIFactory.AddLayoutElement(centerCol.gameObject, flexW: true);
            UIFactory.AddVertical(centerCol.gameObject, 4f, new RectOffset(0, 0, 0, 0));
            var roundText = UIFactory.CreateText(centerCol, "Round", "Round 1",
                18f, TextAlignmentOptions.Center);
            var phaseText = UIFactory.CreateText(centerCol, "Phase", "",
                14f, TextAlignmentOptions.Center, UIFactory.TextGray);
            var stepText = UIFactory.CreateText(centerCol, "Step", "",
                14f, TextAlignmentOptions.Center, UIFactory.TextGray);

            // P2 info
            var p2Group = UIFactory.CreateContainer(topBar, "P2Info");
            var p2GH = UIFactory.AddHorizontal(p2Group.gameObject, 4f);
            p2GH.childForceExpandWidth = false;
            p2GH.childControlWidth = false;
            p2GH.childControlHeight = false;
            var p2StatusFrame = UIFactory.CreateImage(p2Group, "P2Frame",
                new Color(0.2f, 0.6f, 0.3f), 4f, 48f);
            var p2Armor = UIFactory.CreateImage(p2Group, "P2Armor", UIFactory.AccentBlue, 20f, 20f);
            var p2Col = UIFactory.CreateContainer(p2Group, "P2Text");
            UIFactory.AddVertical(p2Col.gameObject, 4f, new RectOffset(0, 0, 0, 0));
            var p2Name = UIFactory.CreateText(p2Col, "P2Name", "P2",
                18f, TextAlignmentOptions.Right);
            var p2Score = UIFactory.CreateText(p2Col, "P2Score", "0",
                14f, TextAlignmentOptions.Right, UIFactory.TextGray);
            var p2Portrait = UIFactory.CreateImage(p2Group, "P2Portrait", UIFactory.PanelBg, 48f, 48f);

            // Timer
            var hudTimer = UIFactory.CreateText(panel, "HudTimer", "",
                24f, TextAlignmentOptions.Center);
            var hudTimerRt = hudTimer.GetComponent<RectTransform>();
            hudTimerRt.anchorMin = new Vector2(0.35f, 0.85f);
            hudTimerRt.anchorMax = new Vector2(0.65f, 0.91f);
            hudTimerRt.offsetMin = Vector2.zero;
            hudTimerRt.offsetMax = Vector2.zero;
            hudTimer.gameObject.SetActive(false);

            // Wire
            Wire(hud, "_roundText", roundText);
            Wire(hud, "_phaseText", phaseText);
            Wire(hud, "_stepText", stepText);
            Wire(hud, "_p1NameText", p1Name);
            Wire(hud, "_p1Portrait", p1Portrait);
            Wire(hud, "_p1ArmorIcon", p1Armor.gameObject);
            Wire(hud, "_p1StatusFrame", p1StatusFrame);
            Wire(hud, "_p2NameText", p2Name);
            Wire(hud, "_p2Portrait", p2Portrait);
            Wire(hud, "_p2ArmorIcon", p2Armor.gameObject);
            Wire(hud, "_p2StatusFrame", p2StatusFrame);
            Wire(hud, "_hudTimerText", hudTimer);
            Wire(hud, "_p1ScoreText", p1Score);
            Wire(hud, "_p2ScoreText", p2Score);

            panel.gameObject.SetActive(false);
            return hud;
        }

        #endregion

        #region Prefab Templates

        private GameObject BuildHeroCardTemplate(Transform parent)
        {
            var go = new GameObject("HeroCardTemplate", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.20f, 0.22f, 0.30f, 1f); // clearly visible card

            var border = UIFactory.CreateImage(go.transform, "Border",
                new Color(0.45f, 0.45f, 0.55f, 1f), 120f, 120f); // visible border
            UIFactory.Stretch(border.GetComponent<RectTransform>());
            border.raycastTarget = false;

            var portrait = UIFactory.CreateImage(go.transform, "Portrait",
                new Color(0.14f, 0.14f, 0.20f, 1f), 80f, 80f);
            var pRt = portrait.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.08f, 0.30f);
            pRt.anchorMax = new Vector2(0.92f, 0.95f);
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;

            var nameText = UIFactory.CreateText(go.transform, "Name", "Hero",
                14f, TextAlignmentOptions.Center, Color.white);
            nameText.fontStyle = FontStyles.Bold;
            var nRt = nameText.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0, 0);
            nRt.anchorMax = new Vector2(1, 0.28f);
            nRt.offsetMin = new Vector2(2, 2);
            nRt.offsetMax = new Vector2(-2, 0);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            var card = go.AddComponent<HeroCardUI>();
            Wire(card, "_portrait", portrait);
            Wire(card, "_background", bg);
            Wire(card, "_border", border);
            Wire(card, "_nameText", nameText);
            Wire(card, "_button", btn);

            go.SetActive(false);
            return go;
        }

        private GameObject BuildActionSlotTemplate(Transform parent)
        {
            // Simple colored square — just icon text centered
            var go = new GameObject("ActionSlotTemplate", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(56, 48);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.24f, 0.8f);

            // Single centered icon text (no vertical stacking — keeps it clean)
            var iconText = UIFactory.CreateText(go.transform, "Icon", "?",
                20f, TextAlignmentOptions.Center, UIFactory.TextWhite);
            iconText.fontStyle = TMPro.FontStyles.Bold;
            UIFactory.Stretch(iconText.GetComponent<RectTransform>());

            // Step number — tiny, top-left corner
            var stepText = UIFactory.CreateText(go.transform, "StepNum", "#1",
                9f, TextAlignmentOptions.TopLeft, new Color(0.7f, 0.7f, 0.8f, 0.7f));
            var stRt = stepText.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 0.6f);
            stRt.anchorMax = new Vector2(0.4f, 1f);
            stRt.offsetMin = new Vector2(3, 0);
            stRt.offsetMax = new Vector2(0, -1);

            // Label — hidden (not needed in compact view)
            var labelText = UIFactory.CreateText(go.transform, "Label", "",
                1f, TextAlignmentOptions.Center, Color.clear);
            labelText.gameObject.SetActive(false);

            // Lock overlay for cooldown
            var lockOverlay = UIFactory.CreatePanel(go.transform, "LockOverlay",
                new Color(0.25f, 0.10f, 0.10f, 0.8f));
            var lockText = UIFactory.CreateText(lockOverlay, "LockText", "CD",
                14f, TextAlignmentOptions.Center, UIFactory.AccentRed);
            lockText.fontStyle = TMPro.FontStyles.Bold;
            UIFactory.Stretch(lockText.GetComponent<RectTransform>());
            lockOverlay.gameObject.SetActive(false);

            var slot = go.AddComponent<ActionSlotUI>();
            Wire(slot, "_background", bg);
            Wire(slot, "_stepText", stepText);
            Wire(slot, "_iconText", iconText);
            Wire(slot, "_labelText", labelText);
            Wire(slot, "_lockOverlay", lockOverlay.gameObject);
            Wire(slot, "_lockText", lockText);

            go.SetActive(false);
            return go;
        }

        private GameObject BuildRoundDetailTemplate(Transform parent)
        {
            var go = new GameObject("RoundDetailTemplate", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 36);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.16f, 0.7f);

            var label = UIFactory.CreateText(go.transform, "Label", "Round 1: --",
                16f, TextAlignmentOptions.Center, UIFactory.TextGray);
            UIFactory.Stretch(label.GetComponent<RectTransform>());

            go.SetActive(false);
            return go;
        }

        #endregion

        #region Helpers

        private (Slider bar, TextMeshProUGUI valueText) BuildStatRow(
            Transform parent, string label, Color barColor)
        {
            var row = UIFactory.CreateContainer(parent, $"Stat_{label}");
            UIFactory.AddLayoutElement(row.gameObject, prefH: 36f);
            var rowH = UIFactory.AddHorizontal(row.gameObject, 8f,
                new RectOffset(0, 0, 0, 0));
            rowH.childAlignment = TextAnchor.MiddleCenter;
            rowH.childForceExpandWidth = false;
            rowH.childControlWidth = false;
            rowH.childControlHeight = false;

            var lbl = UIFactory.CreateText(row, "Label", label,
                18f, TextAlignmentOptions.Left, UIFactory.TextGray);
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 28);

            var bar = UIFactory.CreateStatBar(row, "Bar", barColor, 580f, 22f);

            var valText = UIFactory.CreateText(row, "Value", "0",
                18f, TextAlignmentOptions.Right, UIFactory.TextWhite);
            valText.fontStyle = FontStyles.Bold;
            valText.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 28);

            return (bar, valText);
        }

        /// <summary>
        /// Creates a placeholder 3D capsule with a MeshRenderer so HeroView3D
        /// can drive position/rotation and tint the material colour.
        /// </summary>
        /// <summary>
        /// Wires serialized Material assets to GridView fields.
        /// Materials are created by ProjectSetupWizard and assigned in Inspector.
        /// No Shader.Find() — materials are real assets included in the build.
        /// </summary>
        private void WireGridViewMaterials(GridView gridView)
        {
            Wire(gridView, "_floorMaterial",          _matFloor);
            Wire(gridView, "_floorAltMaterial",       _matFloorAlt);
            Wire(gridView, "_wallMaterial",            _matWall);
            Wire(gridView, "_dangerZoneMaterial",      _matDangerZone);
            Wire(gridView, "_highlightMoveMaterial",   _matHighlightMove);
            Wire(gridView, "_highlightShootMaterial",  _matHighlightShoot);
            Wire(gridView, "_spawnP1Material",         _matSpawnP1);
            Wire(gridView, "_spawnP2Material",         _matSpawnP2);

            int count = (_matFloor ? 1 : 0) + (_matFloorAlt ? 1 : 0) + (_matWall ? 1 : 0)
                      + (_matDangerZone ? 1 : 0) + (_matHighlightMove ? 1 : 0) + (_matHighlightShoot ? 1 : 0)
                      + (_matSpawnP1 ? 1 : 0) + (_matSpawnP2 ? 1 : 0);
            Debug.Log($"[GameBootstrap] GridView materials wired ({count}/8)");
        }

        private HeroView3D CreatePlaceholderHero(string name, Material baseMaterial)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && baseMaterial != null)
            {
                renderer.material = baseMaterial;
            }

            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var arrow = CreateDirectionArrow(go.transform, baseMaterial);

            var view = go.AddComponent<HeroView3D>();
            Wire(view, "_mainRenderer", renderer);
            Wire(view, "_directionArrow", arrow);

            return view;
        }

        private static GameObject CreateDirectionArrow(Transform parent, Material baseMaterial)
        {
            var arrowRoot = new GameObject("DirectionArrow");
            arrowRoot.transform.SetParent(parent, false);
            arrowRoot.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            arrowRoot.transform.localScale = Vector3.one;

            var cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = "ArrowCone";
            cone.transform.SetParent(arrowRoot.transform, false);
            cone.transform.localPosition = Vector3.zero;
            cone.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cone.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);

            var coneCol = cone.GetComponent<Collider>();
            if (coneCol != null) Object.Destroy(coneCol);

            var coneRenderer = cone.GetComponent<MeshRenderer>();
            if (coneRenderer != null && baseMaterial != null)
            {
                var arrowMat = new Material(baseMaterial);
                var brightColor = Color.Lerp(baseMaterial.color, Color.white, 0.5f);
                brightColor.a = 1f;
                arrowMat.color = brightColor;
                coneRenderer.material = arrowMat;
            }

            return arrowRoot;
        }

        private static void EnsurePlatformBootstrap()
        {
            if (Object.FindAnyObjectByType<Platform.PlatformBootstrap>() != null) return;

            var go = new GameObject("PlatformBootstrap");
            go.AddComponent<Platform.PlatformBootstrap>();
            Debug.Log("[GameBootstrap] Created PlatformBootstrap (was missing from scene)");
        }

        private static void Wire(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, FIELD_FLAGS);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[GameBootstrap] Field '{fieldName}' not found on {type.Name}");
            }
        }

        #endregion
    }
}
