using System;
using NidoCero;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NidoCero.Editor
{
    public static class IntegratedUIBuilder
    {
        private const string SceneRoot = "Assets/_Game/Scenes";
        private const string CatalogPath = "Assets/_Game/Generated/Data/GameCatalog.asset";
        private const string SpriteRoot = "Assets/_Game/UI/Sprites/";

        private static Font runtimeFont;

        [MenuItem("Nido Cero/Apply Integrated UI")]
        public static void ApplyFromMenu()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("GameCatalog is missing.");
            Apply(catalog);
            EditorSceneManager.OpenScene(SceneRoot + "/00_Launcher.unity");
        }

        public static void Apply(GameCatalog catalog)
        {
            ValidateSprites();
            BuildLauncher(catalog);
            BuildMainHud();
            AssetDatabase.SaveAssets();
        }

        private static void BuildLauncher(GameCatalog catalog)
        {
            Scene scene = EditorSceneManager.OpenScene(SceneRoot + "/00_Launcher.unity");
            DestroySceneObject(scene, "HUD_Launcher");

            Camera camera = Camera.main;
            if (camera != null) camera.rect = new Rect(0f, 0f, 1f, 1f);

            Canvas canvas = CreateCanvas("HUD_Launcher");
            Panel(canvas.transform, "Background", Vector2.zero, Vector2.one,
                new Color(0.075f, 0.095f, 0.125f, 1f));
            Panel(canvas.transform, "HorizonGlow", new Vector2(0f, 0.39f), new Vector2(1f, 0.56f),
                new Color(0.18f, 0.2f, 0.2f, 0.72f));
            Panel(canvas.transform, "MenuBase", new Vector2(0f, 0f), new Vector2(1f, 0.42f),
                new Color(0.045f, 0.045f, 0.052f, 0.99f));
            Panel(canvas.transform, "AccentLine", new Vector2(0.06f, 0.59f), new Vector2(0.33f, 0.596f),
                new Color(0.1f, 0.7f, 0.88f, 0.9f));

            Text title = TextBlock(canvas.transform, "GameTitle", "NIDO CERO",
                new Vector2(0.06f, 0.73f), new Vector2(0.54f, 0.89f), 68,
                TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            Text subtitle = TextBlock(canvas.transform, "Subtitle", "TODO TIENE UN COSTO",
                new Vector2(0.06f, 0.61f), new Vector2(0.45f, 0.69f), 23,
                TextAnchor.MiddleLeft, new Color(0.62f, 0.69f, 0.76f));
            subtitle.fontStyle = FontStyle.Italic;
            TextBlock(canvas.transform, "StoryLine",
                "AZUL CONTRA EL PROTOCOLO DE PERFECCIÓN",
                new Vector2(0.59f, 0.16f), new Vector2(0.95f, 0.22f), 15,
                TextAnchor.MiddleCenter, new Color(0.55f, 0.7f, 0.76f));

            Image hero = ImageBlock(canvas.transform, "AzulPortrait",
                new Vector2(0.63f, 0.23f), new Vector2(0.91f, 0.76f), Sprite("profile_booby_1785020453755.jpg"));
            hero.preserveAspect = true;
            hero.color = new Color(1f, 1f, 1f, 0.82f);

            Button newGame = MenuButton(canvas.transform, "NewGameButton", "NUEVA PARTIDA",
                new Vector2(0.06f, 0.29f), new Vector2(0.29f, 0.355f));
            Button options = MenuButton(canvas.transform, "OptionsButton", "OPCIONES",
                new Vector2(0.06f, 0.215f), new Vector2(0.29f, 0.28f));
            Button credits = MenuButton(canvas.transform, "CreditsButton", "CRÉDITOS",
                new Vector2(0.06f, 0.14f), new Vector2(0.29f, 0.205f));
            Button quit = MenuButton(canvas.transform, "QuitButton", "SALIR",
                new Vector2(0.06f, 0.065f), new Vector2(0.29f, 0.13f));
            TextBlock(canvas.transform, "Version", "PRE-ALPHA 0.2 · 1280 × 720",
                new Vector2(0.77f, 0.03f), new Vector2(0.97f, 0.08f), 12,
                TextAnchor.MiddleRight, new Color(0.4f, 0.45f, 0.5f));

            GameObject optionsPanel = OverlayWindow(canvas.transform, "OptionsPanel", "OPCIONES");
            TextBlock(optionsPanel.transform, "VolumeLabel", "VOLUMEN GENERAL",
                new Vector2(0.12f, 0.58f), new Vector2(0.52f, 0.69f), 18,
                TextAnchor.MiddleLeft, Color.white);
            Text volumeValue = TextBlock(optionsPanel.transform, "VolumeValue", "100%",
                new Vector2(0.58f, 0.58f), new Vector2(0.76f, 0.69f), 20,
                TextAnchor.MiddleCenter, new Color(0.25f, 0.8f, 1f));
            Button volumeDown = SolidButton(optionsPanel.transform, "VolumeDown", "−",
                new Vector2(0.78f, 0.58f), new Vector2(0.86f, 0.69f), new Color(0.12f, 0.2f, 0.26f));
            Button volumeUp = SolidButton(optionsPanel.transform, "VolumeUp", "+",
                new Vector2(0.87f, 0.58f), new Vector2(0.95f, 0.69f), new Color(0.12f, 0.2f, 0.26f));
            TextBlock(optionsPanel.transform, "FullscreenLabel", "PANTALLA COMPLETA",
                new Vector2(0.12f, 0.42f), new Vector2(0.52f, 0.53f), 18,
                TextAnchor.MiddleLeft, Color.white);
            Text fullscreenValue = TextBlock(optionsPanel.transform, "FullscreenValue", "DESACTIVADA",
                new Vector2(0.55f, 0.42f), new Vector2(0.78f, 0.53f), 16,
                TextAnchor.MiddleCenter, new Color(0.25f, 0.8f, 1f));
            Button fullscreen = SolidButton(optionsPanel.transform, "FullscreenButton", "CAMBIAR",
                new Vector2(0.79f, 0.42f), new Vector2(0.95f, 0.53f), new Color(0.12f, 0.3f, 0.38f));
            TextBlock(optionsPanel.transform, "ResolutionInfo",
                "Diseñado para navegador en formato 1280 × 720.",
                new Vector2(0.12f, 0.27f), new Vector2(0.9f, 0.36f), 14,
                TextAnchor.MiddleLeft, new Color(0.65f, 0.7f, 0.74f));
            Button closeOptions = SolidButton(optionsPanel.transform, "CloseOptions", "VOLVER",
                new Vector2(0.35f, 0.09f), new Vector2(0.65f, 0.2f), new Color(0.16f, 0.34f, 0.42f));

            GameObject creditsPanel = OverlayWindow(canvas.transform, "CreditsPanel", "CRÉDITOS");
            TextBlock(creditsPanel.transform, "CreditsText",
                "NIDO CERO\n\nDiseño, arte, narrativa, música y programación\nEquipo GameJam2026\n\nTema: TODO TIENE UN COSTO",
                new Vector2(0.12f, 0.23f), new Vector2(0.88f, 0.72f), 18,
                TextAnchor.MiddleCenter, new Color(0.82f, 0.88f, 0.9f));
            Button closeCredits = SolidButton(creditsPanel.transform, "CloseCredits", "VOLVER",
                new Vector2(0.35f, 0.09f), new Vector2(0.65f, 0.2f), new Color(0.16f, 0.34f, 0.42f));

            MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();
            controller.Configure(catalog, optionsPanel, creditsPanel, volumeValue, fullscreenValue,
                "01_CinematicIntro");
            UnityEventTools.AddPersistentListener(newGame.onClick, controller.NewGame);
            UnityEventTools.AddPersistentListener(options.onClick, controller.OpenOptions);
            UnityEventTools.AddPersistentListener(credits.onClick, controller.OpenCredits);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);
            UnityEventTools.AddPersistentListener(volumeDown.onClick, controller.VolumeDown);
            UnityEventTools.AddPersistentListener(volumeUp.onClick, controller.VolumeUp);
            UnityEventTools.AddPersistentListener(fullscreen.onClick, controller.ToggleFullscreen);
            UnityEventTools.AddPersistentListener(closeOptions.onClick, controller.ClosePanels);
            UnityEventTools.AddPersistentListener(closeCredits.onClick, controller.ClosePanels);

            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildMainHud()
        {
            Scene scene = EditorSceneManager.OpenScene(SceneRoot + "/02_MainScene.unity");
            DestroySceneObject(scene, "HUD_MainScene");
            DestroySceneObject(scene, "Card_Planes_World");

            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player == null) throw new InvalidOperationException("MainScene player is missing.");

            Canvas canvas = CreateCanvas("HUD_MainScene");
            Panel(canvas.transform, "TopShade", new Vector2(0f, 0.84f), new Vector2(1f, 1f),
                new Color(0.025f, 0.04f, 0.055f, 0.84f));
            Panel(canvas.transform, "BottomShade", new Vector2(0f, 0f), new Vector2(1f, 0.145f),
                new Color(0.012f, 0.022f, 0.028f, 1f));

            Image profile = ImageBlock(canvas.transform, "AzulProfile",
                new Vector2(0.012f, 0.855f), new Vector2(0.092f, 0.982f),
                Sprite("profile_booby_1785020453755.jpg"));
            profile.preserveAspect = true;

            BarParts life = StatusBar(canvas.transform, "LifeBar", "VIDA",
                new Vector2(0.105f, 0.932f), new Vector2(0.36f, 0.972f),
                new Color(0.86f, 0.12f, 0.1f));
            BarParts stamina = StatusBar(canvas.transform, "StaminaBar", "STAMINA",
                new Vector2(0.105f, 0.872f), new Vector2(0.36f, 0.912f),
                new Color(0.94f, 0.73f, 0.08f));

            Text timer = TextBlock(canvas.transform, "Timer", "00:00",
                new Vector2(0.455f, 0.93f), new Vector2(0.545f, 0.988f), 30,
                TextAnchor.MiddleCenter, Color.white);
            Text floor = TextBlock(canvas.transform, "Floor", "PISO 3",
                new Vector2(0.39f, 0.875f), new Vector2(0.49f, 0.92f), 15,
                TextAnchor.MiddleRight, new Color(0.95f, 0.8f, 0.38f));
            Text key = TextBlock(canvas.transform, "Key", "SIN LLAVE",
                new Vector2(0.51f, 0.875f), new Vector2(0.61f, 0.92f), 15,
                TextAnchor.MiddleLeft, new Color(0.95f, 0.8f, 0.38f));
            Text objective = TextBlock(canvas.transform, "Objective",
                "Derrota enemigos y recoge sus cubos de decisión",
                new Vector2(0.62f, 0.86f), new Vector2(0.88f, 0.925f), 13,
                TextAnchor.MiddleCenter, new Color(0.75f, 0.82f, 0.86f));
            Button pauseButton = SolidButton(canvas.transform, "PauseButton", "MENÚ  [ESC]",
                new Vector2(0.895f, 0.91f), new Vector2(0.985f, 0.978f),
                new Color(0.12f, 0.18f, 0.24f, 0.95f));

            Text[] elementValues = new Text[3];
            Sprite[] hudElementIcons =
            {
                Sprite("element_fire.jpg"), Sprite("element_water.jpg"), Sprite("element_nature.jpg")
            };
            string[] elementNames = { "FUEGO", "AGUA", "VEGETACIÓN" };
            for (int i = 0; i < 3; i++)
            {
                float minX = 0.012f + i * 0.064f;
                elementValues[i] = Counter(canvas.transform, "Element_" + elementNames[i], elementNames[i],
                    hudElementIcons[i], new Vector2(minX, 0.015f), new Vector2(minX + 0.055f, 0.13f));
            }

            Text[] statValues = new Text[6];
            Sprite[] hudStatIcons =
            {
                Sprite("stat_attack.jpg"), Sprite("stat_defense.jpg"), Sprite("stat_health.jpg"),
                Sprite("stat_agility.jpg"), Sprite("stat_speed.jpg"), Sprite("stat_stamina.jpg")
            };
            string[] statNames = { "ATAQUE", "DEFENSA", "VIDA", "AGILIDAD", "VELOCIDAD", "STAMINA" };
            for (int i = 0; i < 6; i++)
            {
                float minX = 0.675f + i * 0.052f;
                statValues[i] = Counter(canvas.transform, "Stat_" + statNames[i], statNames[i],
                    hudStatIcons[i], new Vector2(minX, 0.015f), new Vector2(minX + 0.046f, 0.13f));
            }

            GameObject controlsBar = Panel(canvas.transform, "ControlsBar",
                new Vector2(0.22f, 0.022f), new Vector2(0.66f, 0.074f),
                new Color(0.02f, 0.035f, 0.045f, 0.9f));
            TextBlock(controlsBar.transform, "Controls",
                "[A / D] MOVER   [SHIFT] CORRER   [ESPACIO] SALTAR   [CLICK] DISPARAR   [E] INTERACTUAR   [P / ESC] PAUSA",
                Vector2.zero, Vector2.one, 10, TextAnchor.MiddleCenter, new Color(0.88f, 0.93f, 0.94f));
            Text crosshair = TextBlock(canvas.transform, "Crosshair", "+",
                new Vector2(0.487f, 0.475f), new Vector2(0.513f, 0.525f), 22,
                TextAnchor.MiddleCenter, Color.white);
            crosshair.fontStyle = FontStyle.Bold;

            GameObject pausePanel = Panel(canvas.transform, "PausePanel", Vector2.zero, Vector2.one,
                new Color(0f, 0f, 0f, 0.72f));
            GameObject pauseWindow = Panel(pausePanel.transform, "PauseWindow",
                new Vector2(0.35f, 0.19f), new Vector2(0.65f, 0.8f),
                new Color(0.045f, 0.065f, 0.085f, 0.98f));
            Text pauseTitle = TextBlock(pauseWindow.transform, "PauseTitle", "PAUSA",
                new Vector2(0.1f, 0.74f), new Vector2(0.9f, 0.91f), 36,
                TextAnchor.MiddleCenter, Color.white);
            pauseTitle.fontStyle = FontStyle.Bold;
            Button resume = SolidButton(pauseWindow.transform, "ResumeButton", "CONTINUAR",
                new Vector2(0.18f, 0.52f), new Vector2(0.82f, 0.65f), new Color(0.1f, 0.38f, 0.48f));
            Button restart = SolidButton(pauseWindow.transform, "RestartButton", "REINICIAR ESCENA",
                new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.47f), new Color(0.15f, 0.24f, 0.3f));
            Button mainMenu = SolidButton(pauseWindow.transform, "MainMenuButton", "MENÚ PRINCIPAL",
                new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.29f), new Color(0.3f, 0.16f, 0.14f));

            GameObject bossPanel = Panel(canvas.transform, "BossPanel",
                new Vector2(0.29f, 0.77f), new Vector2(0.71f, 0.845f),
                new Color(0.12f, 0.02f, 0.04f, 0.9f));
            Text bossText = TextBlock(bossPanel.transform, "BossText", "NÚCLEO CERO · RELÉS",
                new Vector2(0f, 0.55f), new Vector2(1f, 1f), 15,
                TextAnchor.MiddleCenter, Color.white);
            Slider bossSlider = SliderBar(bossPanel.transform, "BossHealth",
                new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.38f), new Color(0.85f, 0.18f, 0.12f));
            Image damageFlash = Panel(canvas.transform, "DamageFlash", Vector2.zero, Vector2.one,
                new Color(0.78f, 0.02f, 0.025f, 0f)).GetComponent<Image>();
            damageFlash.raycastTarget = false;
            damageFlash.gameObject.SetActive(false);

            HudController hud = canvas.gameObject.AddComponent<HudController>();
            hud.ConfigureEnhanced(life.value, stamina.value, life.fill, stamina.fill,
                statValues, elementValues, floor, key, objective, timer,
                pausePanel, bossPanel, bossText, bossSlider);
            hud.ConfigureDamageFlash(damageFlash);
            hud.BindPlayer(player);
            UnityEventTools.AddPersistentListener(pauseButton.onClick, hud.TogglePause);
            UnityEventTools.AddPersistentListener(resume.onClick, hud.TogglePause);
            UnityEventTools.AddPersistentListener(restart.onClick, hud.RestartScene);
            UnityEventTools.AddPersistentListener(mainMenu.onClick, hud.ReturnToMainMenu);

            BuildCardChoice(canvas);
            BuildDialogue(canvas);
            BuildFinalSacrifice(canvas);
            pausePanel.SetActive(false);
            bossPanel.SetActive(false);
            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildCardChoice(Canvas canvas)
        {
            GameObject overlay = Panel(canvas.transform, "CardChoiceOverlay", Vector2.zero, Vector2.one,
                new Color(0.012f, 0.018f, 0.026f, 0.95f));
            Text heading = TextBlock(overlay.transform, "DecisionTitle",
                "ELIGE UNA MEJORA · LAS OTRAS DOS DESAPARECEN",
                new Vector2(0.12f, 0.875f), new Vector2(0.88f, 0.96f), 25,
                TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.34f));
            heading.fontStyle = FontStyle.Bold;
            TextBlock(overlay.transform, "DecisionHint", "Haz clic o presiona 1, 2 o 3",
                new Vector2(0.3f, 0.055f), new Vector2(0.7f, 0.105f), 15,
                TextAnchor.MiddleCenter, Color.white);

            GameObject[] cardObjects = new GameObject[3];
            Image[] backgrounds = new Image[3];
            Image[] elementIcons = new Image[3];
            Text[] elementLabels = new Text[3];
            Text[] titles = new Text[3];
            Text[] descriptions = new Text[3];
            Image[] modifierIcons = new Image[9];
            Text[] modifierTexts = new Text[9];

            for (int i = 0; i < 3; i++)
            {
                float minX = 0.095f + i * 0.305f;
                GameObject card = Panel(overlay.transform, "Card_" + (i + 1),
                    new Vector2(minX, 0.14f), new Vector2(minX + 0.255f, 0.84f), Color.white);
                cardObjects[i] = card;
                backgrounds[i] = card.GetComponent<Image>();
                backgrounds[i].preserveAspect = false;
                elementIcons[i] = ImageBlock(card.transform, "ElementIcon",
                    new Vector2(0.33f, 0.72f), new Vector2(0.67f, 0.9f), Sprite("element_water.jpg"));
                elementIcons[i].preserveAspect = true;
                elementLabels[i] = TextBlock(card.transform, "ElementLabel", "AGUA",
                    new Vector2(0.12f, 0.65f), new Vector2(0.88f, 0.72f), 15,
                    TextAnchor.MiddleCenter, new Color(0.2f, 0.78f, 1f));
                titles[i] = TextBlock(card.transform, "Title", "TARJETA",
                    new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.64f), 22,
                    TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.25f));
                titles[i].fontStyle = FontStyle.Bold;
                descriptions[i] = TextBlock(card.transform, "Description", "DOS MEJORAS · UN COSTO",
                    new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.49f), 12,
                    TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.88f));

                for (int row = 0; row < 3; row++)
                {
                    int flat = i * 3 + row;
                    float top = 0.38f - row * 0.105f;
                    modifierIcons[flat] = ImageBlock(card.transform, "ModifierIcon_" + row,
                        new Vector2(0.12f, top - 0.07f), new Vector2(0.25f, top + 0.01f),
                        Sprite("stat_attack.jpg"));
                    modifierIcons[flat].preserveAspect = true;
                    modifierTexts[flat] = TextBlock(card.transform, "ModifierText_" + row, "+1 ATAQUE",
                        new Vector2(0.27f, top - 0.07f), new Vector2(0.9f, top + 0.01f), 16,
                        TextAnchor.MiddleLeft, row < 2
                            ? new Color(0.42f, 1f, 0.48f)
                            : new Color(1f, 0.32f, 0.3f));
                }
            }

            Sprite[] elementSprites =
            {
                Sprite("element_water.jpg"), Sprite("element_fire.jpg"), Sprite("element_nature.jpg"),
                Sprite("card_bg_water.jpg"), Sprite("card_bg_fire.jpg"), Sprite("card_bg_nature.jpg")
            };
            Sprite[] statSprites =
            {
                Sprite("stat_attack.jpg"), Sprite("stat_speed.jpg"), Sprite("stat_defense.jpg"),
                Sprite("stat_agility.jpg"), Sprite("stat_health.jpg"), Sprite("stat_stamina.jpg")
            };

            CardChoiceController controller = canvas.gameObject.AddComponent<CardChoiceController>();
            controller.Configure(overlay, cardObjects, backgrounds, elementIcons, elementLabels, titles,
                descriptions, modifierIcons, modifierTexts, elementSprites, statSprites);
            for (int i = 0; i < cardObjects.Length; i++)
                cardObjects[i].AddComponent<CardButtonProxy>().Configure(i, controller);
            overlay.SetActive(false);
        }

        private static void BuildDialogue(Canvas canvas)
        {
            GameObject overlay = Panel(canvas.transform, "DialogueOverlay", Vector2.zero, Vector2.one,
                Color.clear);
            GameObject window = Panel(overlay.transform, "DialogueWindow",
                new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.43f),
                new Color(0.025f, 0.045f, 0.055f, 0.97f));
            Panel(window.transform, "GeorgeAccent",
                new Vector2(0f, 0f), new Vector2(0.012f, 1f),
                new Color(0.38f, 0.78f, 0.42f));
            Text speaker = TextBlock(window.transform, "Speaker", "GEORGE ZOILO MIYAGI",
                new Vector2(0.055f, 0.73f), new Vector2(0.95f, 0.93f), 20,
                TextAnchor.MiddleLeft, new Color(0.62f, 1f, 0.68f));
            speaker.fontStyle = FontStyle.Bold;
            Text body = TextBlock(window.transform, "DialogueText",
                "Los humanos desaparecieron. Su control no.",
                new Vector2(0.055f, 0.25f), new Vector2(0.95f, 0.72f), 22,
                TextAnchor.MiddleLeft, Color.white);
            Text continueLabel = TextBlock(window.transform, "ContinueHint",
                "[E / ESPACIO / CLIC] CONTINUAR",
                new Vector2(0.5f, 0.05f), new Vector2(0.95f, 0.23f), 13,
                TextAnchor.MiddleRight, new Color(0.78f, 0.84f, 0.86f));

            DialogueController controller = canvas.gameObject.AddComponent<DialogueController>();
            controller.Configure(overlay, speaker, body, continueLabel);
            overlay.SetActive(false);
        }

        private static void BuildFinalSacrifice(Canvas canvas)
        {
            GameObject overlay = Panel(canvas.transform, "FinalSacrificeOverlay", Vector2.zero, Vector2.one,
                new Color(0.01f, 0.015f, 0.02f, 0.96f));
            Text title = TextBlock(overlay.transform, "SacrificeTitle", "TODO TIENE UN COSTO",
                new Vector2(0.15f, 0.75f), new Vector2(0.85f, 0.9f), 38,
                TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.25f));
            title.fontStyle = FontStyle.Bold;
            TextBlock(overlay.transform, "SacrificeMessage",
                "LIBERAR HÁBITATS\nSACRIFICAR TODAS LAS MEJORAS",
                new Vector2(0.15f, 0.48f), new Vector2(0.85f, 0.7f), 28,
                TextAnchor.MiddleCenter, Color.white);
            TextBlock(overlay.transform, "SacrificeExplanation",
                "Núcleo Cero vinculó la red a tus atributos y elementales.\nConfirmar devolverá a Azul a sus valores iniciales.",
                new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.47f), 17,
                TextAnchor.MiddleCenter, new Color(0.75f, 0.82f, 0.86f));

            GameObject progressBar = Panel(overlay.transform, "SacrificeProgressBar",
                new Vector2(0.25f, 0.23f), new Vector2(0.75f, 0.28f),
                new Color(0.09f, 0.12f, 0.14f, 1f));
            Image fill = Panel(progressBar.transform, "SacrificeProgressFill",
                new Vector2(0.01f, 0.12f), new Vector2(0.99f, 0.88f),
                new Color(0.35f, 0.86f, 0.48f)).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            Text progress = TextBlock(overlay.transform, "SacrificeProgressText",
                "MANTÉN [E / ESPACIO / CLIC] — CONFIRMAR",
                new Vector2(0.2f, 0.13f), new Vector2(0.8f, 0.21f), 16,
                TextAnchor.MiddleCenter, new Color(0.82f, 0.9f, 0.84f));

            FinalSacrificeController controller =
                canvas.gameObject.AddComponent<FinalSacrificeController>();
            controller.Configure(overlay, fill, progress);
            overlay.SetActive(false);
        }

        private static BarParts StatusBar(Transform parent, string name, string label, Vector2 min, Vector2 max,
            Color fillColor)
        {
            GameObject root = Panel(parent, name, min, max, new Color(0.03f, 0.035f, 0.045f, 0.94f));
            Image fill = Panel(root.transform, name + "Fill", new Vector2(0f, 0f), new Vector2(1f, 1f), fillColor)
                .GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            TextBlock(root.transform, "Label", label, new Vector2(0.02f, 0f), new Vector2(0.31f, 1f), 12,
                TextAnchor.MiddleLeft, Color.white);
            Text value = TextBlock(root.transform, "Value", "0 / 0", new Vector2(0.66f, 0f),
                new Vector2(0.98f, 1f), 13, TextAnchor.MiddleRight, Color.white);
            return new BarParts { fill = fill, value = value };
        }

        private static Text Counter(Transform parent, string name, string label, Sprite icon, Vector2 min, Vector2 max)
        {
            GameObject root = Panel(parent, name, min, max, new Color(0.02f, 0.03f, 0.04f, 0.76f));
            Image image = ImageBlock(root.transform, "Icon", new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.98f),
                icon);
            image.preserveAspect = true;
            Text value = TextBlock(root.transform, "Value", "0", new Vector2(0f, 0.08f), new Vector2(1f, 0.36f),
                15, TextAnchor.MiddleCenter, Color.white);
            TextBlock(root.transform, "Label", label, new Vector2(-0.1f, -0.12f), new Vector2(1.1f, 0.08f),
                8, TextAnchor.MiddleCenter, new Color(0.65f, 0.72f, 0.75f));
            return value;
        }

        private static Slider SliderBar(Transform parent, string name, Vector2 min, Vector2 max, Color fillColor)
        {
            GameObject root = Panel(parent, name, min, max, new Color(0.08f, 0.08f, 0.09f, 1f));
            Slider slider = root.AddComponent<Slider>();
            GameObject fillArea = Panel(root.transform, "Fill Area", new Vector2(0.01f, 0.12f),
                new Vector2(0.99f, 0.88f), Color.clear);
            Image fill = Panel(fillArea.transform, "Fill", Vector2.zero, Vector2.one, fillColor)
                .GetComponent<Image>();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            return slider;
        }

        private static GameObject OverlayWindow(Transform parent, string name, string title)
        {
            GameObject overlay = Panel(parent, name, new Vector2(0.28f, 0.18f), new Vector2(0.72f, 0.82f),
                new Color(0.035f, 0.055f, 0.075f, 0.99f));
            Text heading = TextBlock(overlay.transform, "Title", title, new Vector2(0.08f, 0.78f),
                new Vector2(0.92f, 0.94f), 30, TextAnchor.MiddleCenter, Color.white);
            heading.fontStyle = FontStyle.Bold;
            Panel(overlay.transform, "Accent", new Vector2(0.18f, 0.75f), new Vector2(0.82f, 0.76f),
                new Color(0.1f, 0.7f, 0.88f));
            return overlay;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject host = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            result.transform.SetParent(parent, false);
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            result.GetComponent<Image>().color = color;
            return result;
        }

        private static Text TextBlock(Transform parent, string name, string content, Vector2 min, Vector2 max,
            int fontSize, TextAnchor alignment, Color color)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            result.transform.SetParent(parent, false);
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = result.GetComponent<Text>();
            text.font = RuntimeFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            Shadow shadow = result.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static Image ImageBlock(Transform parent, string name, Vector2 min, Vector2 max, Sprite sprite)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            result.transform.SetParent(parent, false);
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = result.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            return image;
        }

        private static Button MenuButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            Button button = SolidButton(parent, name, label, min, max, new Color(1f, 1f, 1f, 0.015f));
            Text text = button.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 21;
            text.fontStyle = FontStyle.Bold;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.015f);
            colors.highlightedColor = new Color(0.08f, 0.55f, 0.72f, 0.3f);
            colors.pressedColor = new Color(0.08f, 0.55f, 0.72f, 0.55f);
            button.colors = colors;
            return button;
        }

        private static Button SolidButton(Transform parent, string name, string label, Vector2 min, Vector2 max,
            Color color)
        {
            GameObject result = Panel(parent, name, min, max, color);
            Button button = result.AddComponent<Button>();
            button.targetGraphic = result.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
            button.colors = colors;
            Text text = TextBlock(result.transform, "Label", label, new Vector2(0.06f, 0f),
                new Vector2(0.94f, 1f), 16, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        private static Sprite Sprite(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + fileName);
            if (sprite == null) throw new InvalidOperationException("Missing UI sprite: " + fileName);
            return sprite;
        }

        private static void ValidateSprites()
        {
            string[] required =
            {
                "profile_booby_1785020453755.jpg",
                "element_fire.jpg", "element_water.jpg", "element_nature.jpg",
                "card_bg_fire.jpg", "card_bg_water.jpg", "card_bg_nature.jpg",
                "stat_attack.jpg", "stat_defense.jpg", "stat_health.jpg",
                "stat_agility.jpg", "stat_speed.jpg", "stat_stamina.jpg"
            };
            foreach (string file in required)
                if (AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + file) == null)
                    throw new InvalidOperationException("Missing integrated UI asset: " + SpriteRoot + file);
        }

        private static void DestroySceneObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform target = root.name == name ? root.transform : FindRecursive(root.transform, name);
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                    return;
                }
            }
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Font RuntimeFont()
        {
            if (runtimeFont == null)
                runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return runtimeFont;
        }

        private sealed class BarParts
        {
            public Image fill;
            public Text value;
        }
    }
}
