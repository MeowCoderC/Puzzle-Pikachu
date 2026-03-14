// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2015/03/12 16:03

using System;
using System.Collections.Generic;
using System.IO;
using DG.DemiEditor;
using DG.DOTweenEditor.Core;
using DG.DOTweenEditor.UI;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEditor;
using UnityEngine;
using DOTweenSettings = DG.Tweening.Core.DOTweenSettings;
#if false // UI_MARKER
using UnityEngine.UI;
#endif

#if false // TEXTMESHPRO_MARKER
    using TMPro;
#endif

namespace DG.DOTweenEditor
{
    [CustomEditor(typeof(DOTweenAnimation))]
    public class DOTweenAnimationInspector : ABSAnimationInspector
    {
        private enum FadeTargetType
        {
            CanvasGroup,
            Image
        }

        private enum ChooseTargetMode
        {
            None,
            BetweenCanvasGroupAndImage
        }

        private static readonly Dictionary<DOTweenAnimation.AnimationType, Type[]> _AnimationTypeToComponent = new()
        {
            {
                DOTweenAnimation.AnimationType.Move, new[]
                {
#if false // PHYSICS_MARKER
                typeof(Rigidbody),
#endif
#if true // PHYSICS2D_MARKER
                    typeof(Rigidbody2D),
#endif
#if false // UI_MARKER
                typeof(RectTransform),
#endif
                    typeof(Transform)
                }
            },
            {
                DOTweenAnimation.AnimationType.Rotate, new[]
                {
#if false // PHYSICS_MARKER
                typeof(Rigidbody),
#endif
#if true // PHYSICS2D_MARKER
                    typeof(Rigidbody2D),
#endif
                    typeof(Transform)
                }
            },
            { DOTweenAnimation.AnimationType.LocalMove, new[] { typeof(Transform) } },
            { DOTweenAnimation.AnimationType.LocalRotate, new[] { typeof(Transform) } },
            { DOTweenAnimation.AnimationType.Scale, new[] { typeof(Transform) } },
            {
                DOTweenAnimation.AnimationType.Color, new[]
                {
                    typeof(Light),
#if true // SPRITE_MARKER
                    typeof(SpriteRenderer),
#endif
#if false // UI_MARKER
                typeof(Image), typeof(Text), typeof(RawImage), typeof(Graphic),
#endif
                    typeof(Renderer)
                }
            },
            {
                DOTweenAnimation.AnimationType.Fade, new[]
                {
                    typeof(Light),
#if true // SPRITE_MARKER
                    typeof(SpriteRenderer),
#endif
#if false // UI_MARKER
                typeof(Image), typeof(Text), typeof(CanvasGroup), typeof(RawImage), typeof(Graphic),
#endif
                    typeof(Renderer)
                }
            },
#if false // UI_MARKER
            { DOTweenAnimation.AnimationType.Text, new[] { typeof(Text) } },
#endif
            {
                DOTweenAnimation.AnimationType.PunchPosition, new[]
                {
#if false // UI_MARKER
                typeof(RectTransform),
#endif
                    typeof(Transform)
                }
            },
            { DOTweenAnimation.AnimationType.PunchRotation, new[] { typeof(Transform) } },
            { DOTweenAnimation.AnimationType.PunchScale, new[] { typeof(Transform) } },
            {
                DOTweenAnimation.AnimationType.ShakePosition, new[]
                {
#if false // UI_MARKER
                typeof(RectTransform),
#endif
                    typeof(Transform)
                }
            },
            { DOTweenAnimation.AnimationType.ShakeRotation, new[] { typeof(Transform) } },
            { DOTweenAnimation.AnimationType.ShakeScale, new[] { typeof(Transform) } },
            { DOTweenAnimation.AnimationType.CameraAspect, new[] { typeof(Camera) } },
            { DOTweenAnimation.AnimationType.CameraBackgroundColor, new[] { typeof(Camera) } },
            { DOTweenAnimation.AnimationType.CameraFieldOfView, new[] { typeof(Camera) } },
            { DOTweenAnimation.AnimationType.CameraOrthoSize, new[] { typeof(Camera) } },
            { DOTweenAnimation.AnimationType.CameraPixelRect, new[] { typeof(Camera) } },
            { DOTweenAnimation.AnimationType.CameraRect, new[] { typeof(Camera) } },
#if false // UI_MARKER
            { DOTweenAnimation.AnimationType.UIWidthHeight, new[] { typeof(RectTransform) } },
            { DOTweenAnimation.AnimationType.FillAmount, new[] { typeof(Image) } },
#endif
        };

#if false // TK2D_MARKER
        static readonly Dictionary<DOTweenAnimation.AnimationType, Type[]> _Tk2dAnimationTypeToComponent = new Dictionary<DOTweenAnimation.AnimationType, Type[]>() {
            { DOTweenAnimation.AnimationType.Scale, new[] { typeof(tk2dBaseSprite), typeof(tk2dTextMesh) } },
            { DOTweenAnimation.AnimationType.Color, new[] { typeof(tk2dBaseSprite), typeof(tk2dTextMesh) } },
            { DOTweenAnimation.AnimationType.Fade, new[] { typeof(tk2dBaseSprite), typeof(tk2dTextMesh) } },
            { DOTweenAnimation.AnimationType.Text, new[] { typeof(tk2dTextMesh) } }
        };
#endif
#if false // TEXTMESHPRO_MARKER
        static readonly Dictionary<DOTweenAnimation.AnimationType, Type[]> _TMPAnimationTypeToComponent = new Dictionary<DOTweenAnimation.AnimationType, Type[]>() {
            { DOTweenAnimation.AnimationType.Color, new[] { typeof(TextMeshPro), typeof(TextMeshProUGUI) } },
            { DOTweenAnimation.AnimationType.Fade, new[] { typeof(TextMeshPro), typeof(TextMeshProUGUI) } },
            { DOTweenAnimation.AnimationType.Text, new[] { typeof(TextMeshPro), typeof(TextMeshProUGUI) } }
        };
#endif

        private static readonly string[] _AnimationType = new[]
        {
            "None",
            "Move", "LocalMove",
            "Rotate", "LocalRotate",
            "Scale",
            "Color", "Fade",
#if false // UI_MARKER
            "FillAmount",
            "Text",
#endif
#if false // TK2D_MARKER
            "Text",
#endif
#if false // TEXTMESHPRO_MARKER
            "Text",
#endif
#if false // UI_MARKER
            "UIWidthHeight",
#endif
            "Punch/Position", "Punch/Rotation", "Punch/Scale",
            "Shake/Position", "Shake/Rotation", "Shake/Scale",
            "Camera/Aspect", "Camera/BackgroundColor", "Camera/FieldOfView", "Camera/OrthoSize", "Camera/PixelRect", "Camera/Rect"
        };

        private static string[] _animationTypeNoSlashes; // _AnimationType list without slashes in values
        private static string[] _datString; // String representation of DOTweenAnimation enum (here for caching reasons)

        private DOTweenAnimation _src;
        private DOTweenSettings  _settings;
        private bool             _runtimeEditMode; // If TRUE allows to change and save stuff at runtime
        private bool             _refreshRequired; // If TRUE refreshes components data
        private int              _totComponentsOnSrc; // Used to determine if a Component is added or removed from the source
        private bool             _isLightSrc; // Used to determine if we're tweening a Light, to set the max Fade value to more than 1
#pragma warning disable 414
        private ChooseTargetMode _chooseTargetMode = ChooseTargetMode.None;
#pragma warning restore 414

        private static readonly GUIContent _GuiC_selfTarget_true = new(
            "SELF", "Will animate components on this gameObject"
        );

        private static readonly GUIContent _GuiC_selfTarget_false = new(
            "OTHER", "Will animate components on the given gameObject instead than on this one"
        );

        private static readonly GUIContent _GuiC_tweenTargetIsTargetGO_true = new(
            "Use As Tween Target", "Will set the tween target (via SetTarget, used to control a tween directly from a target) to the \"OTHER\" gameObject"
        );

        private static readonly GUIContent _GuiC_tweenTargetIsTargetGO_false = new(
            "Use As Tween Target", "Will set the tween target (via SetTarget, used to control a tween directly from a target) to the gameObject containing this animation, not the \"OTHER\" one"
        );

        #region MonoBehaviour Methods

        private void OnEnable()
        {
            this._src      = this.target as DOTweenAnimation;
            this._settings = DOTweenUtilityWindow.GetDOTweenSettings();

            this.onStartProperty        = this.serializedObject.FindProperty("onStart");
            this.onPlayProperty         = this.serializedObject.FindProperty("onPlay");
            this.onUpdateProperty       = this.serializedObject.FindProperty("onUpdate");
            this.onStepCompleteProperty = this.serializedObject.FindProperty("onStepComplete");
            this.onCompleteProperty     = this.serializedObject.FindProperty("onComplete");
            this.onRewindProperty       = this.serializedObject.FindProperty("onRewind");
            this.onTweenCreatedProperty = this.serializedObject.FindProperty("onTweenCreated");

            // Convert _AnimationType to _animationTypeNoSlashes
            var len = _AnimationType.Length;
            _animationTypeNoSlashes = new string[len];
            for (var i = 0; i < len; ++i)
            {
                var a = _AnimationType[i];
                a                          = a.Replace("/", "");
                _animationTypeNoSlashes[i] = a;
            }
        }

        private void OnDisable() { DOTweenPreviewManager.StopAllPreviews(); }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(3);
            EditorGUIUtils.SetGUIStyles();

            var playMode = Application.isPlaying;
            this._runtimeEditMode = this._runtimeEditMode && playMode;

            GUILayout.BeginHorizontal();
            EditorGUIUtils.InspectorLogo();
            GUILayout.Label(this._src.animationType.ToString() + (string.IsNullOrEmpty(this._src.id) ? "" : " [" + this._src.id + "]"), EditorGUIUtils.sideLogoIconBoldLabelStyle);
            // Up-down buttons
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("▲", DeGUI.styles.button.toolIco)) UnityEditorInternal.ComponentUtility.MoveComponentUp(this._src);
            if (GUILayout.Button("▼", DeGUI.styles.button.toolIco)) UnityEditorInternal.ComponentUtility.MoveComponentDown(this._src);
            GUILayout.EndHorizontal();

            if (playMode)
            {
                if (this._runtimeEditMode)
                {
                }
                else
                {
                    GUILayout.Space(8);
                    GUILayout.Label("Animation Editor disabled while in play mode", EditorGUIUtils.wordWrapLabelStyle);
                    if (!this._src.isActive)
                    {
                        GUILayout.Label("This animation has been toggled as inactive and won't be generated", EditorGUIUtils.wordWrapLabelStyle);
                        GUI.enabled = false;
                    }

                    if (GUILayout.Button(new GUIContent("Activate Edit Mode", "Switches to Runtime Edit Mode, where you can change animations values and restart them"))) this._runtimeEditMode = true;
                    GUILayout.Label("NOTE: when using DOPlayNext, the sequence is determined by the DOTweenAnimation Components order in the target GameObject's Inspector",
                        EditorGUIUtils.wordWrapLabelStyle);
                    GUILayout.Space(10);

                    if (!this._runtimeEditMode) return;
                }
            }

            Undo.RecordObject(this._src, "DOTween Animation");
            Undo.RecordObject(this._settings, "DOTween Animation");

//            _src.isValid = Validate(); // Moved down

            EditorGUIUtility.labelWidth = 110;

            if (playMode)
            {
                GUILayout.Space(4);
                DeGUILayout.Toolbar("Edit Mode Commands");
                DeGUILayout.BeginVBox(DeGUI.styles.box.stickyTop);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("TogglePause")) this._src.tween.TogglePause();
                if (GUILayout.Button("Rewind")) this._src.tween.Rewind();
                if (GUILayout.Button("Restart")) this._src.tween.Restart();
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Commit changes and restart"))
                {
                    this._src.tween.Rewind();
                    this._src.tween.Kill();
                    if (this._src.isValid)
                    {
                        this._src.CreateTween();
                        this._src.tween.Play();
                    }
                }

                GUILayout.Label(
                    "To apply your changes when exiting Play mode, use the Component's upper right menu and choose \"Copy Component\", then \"Paste Component Values\" after exiting Play mode",
                    DeGUI.styles.label.wordwrap);
                DeGUILayout.EndVBox();
            }
            else
            {
                GUILayout.BeginHorizontal();
                var hasManager = this._src.GetComponent<DOTweenVisualManager>() != null;
                EditorGUI.BeginChangeCheck();
                this._settings.showPreviewPanel = hasManager
                    ? DeGUILayout.ToggleButton(this._settings.showPreviewPanel, "Preview Controls", styles.custom.inlineToggle)
                    : DeGUILayout.ToggleButton(this._settings.showPreviewPanel, "Preview Controls", styles.custom.inlineToggle, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(this._settings);
                    DOTweenPreviewManager.StopAllPreviews();
                }

                if (!hasManager)
                    if (GUILayout.Button(new GUIContent("Add Manager", "Adds a manager component which allows you to choose additional options for this gameObject")))
                        this._src.gameObject.AddComponent<DOTweenVisualManager>();

                GUILayout.EndHorizontal();
            }

            // Preview in editor
            var isPreviewing = this._settings.showPreviewPanel ? DOTweenPreviewManager.PreviewGUI(this._src) : false;

            EditorGUI.BeginDisabledGroup(isPreviewing);
            // Choose target
            GUILayout.BeginHorizontal();
            this._src.isActive = EditorGUILayout.Toggle(new GUIContent("", "If unchecked, this animation will not be created"), this._src.isActive, GUILayout.Width(14));
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginChangeCheck();
            this._src.targetIsSelf = DeGUILayout.ToggleButton(this._src.targetIsSelf, this._src.targetIsSelf ? _GuiC_selfTarget_true : _GuiC_selfTarget_false,
                new Color(1f, 0.78f, 0f), DeGUI.colors.bg.toggleOn, new Color(0.33f, 0.14f, 0.02f), DeGUI.colors.content.toggleOn,
                null, GUILayout.Width(47)
            );
            var innerChanged = EditorGUI.EndChangeCheck();
            if (innerChanged)
            {
                this._src.targetGO = null;
                GUI.changed        = true;
            }

            if (this._src.targetIsSelf)
            {
                GUILayout.Label(_GuiC_selfTarget_true.tooltip);
            }
            else
            {
                using (new DeGUI.ColorScope(null, null, this._src.targetGO == null ? Color.red : Color.white))
                {
                    this._src.targetGO = (GameObject)EditorGUILayout.ObjectField(this._src.targetGO, typeof(GameObject), true);
                }

                this._src.tweenTargetIsTargetGO = DeGUILayout.ToggleButton(this._src.tweenTargetIsTargetGO,
                    this._src.tweenTargetIsTargetGO ? _GuiC_tweenTargetIsTargetGO_true : _GuiC_tweenTargetIsTargetGO_false,
                    GUILayout.Width(131)
                );
            }

            var check                        = EditorGUI.EndChangeCheck();
            if (check) this._refreshRequired = true;
            GUILayout.EndHorizontal();

            var targetGO = this._src.targetIsSelf ? this._src.gameObject : this._src.targetGO;

            if (targetGO == null)
            {
                // Uses external target gameObject but it's not set
                if (this._src.targetGO != null || this._src.target != null)
                {
                    this._src.targetGO = null;
                    this._src.target   = null;
                    GUI.changed        = true;
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                var prevAnimType = this._src.animationType;
//                _src.animationType = (DOTweenAnimation.AnimationType)EditorGUILayout.EnumPopup(_src.animationType, EditorGUIUtils.popupButton);
                GUI.enabled             = GUI.enabled && this._src.isActive;
                this._src.animationType = this.AnimationToDOTweenAnimationType(_AnimationType[EditorGUILayout.Popup(this.DOTweenAnimationTypeToPopupId(this._src.animationType), _AnimationType)]);
                this._src.autoGenerate = DeGUILayout.ToggleButton(this._src.autoGenerate,
                    new GUIContent("AutoGenerate", "If selected, the tween will be generated at startup (during Start for RectTransform position tween, Awake for all the others)"));
                if (this._src.autoGenerate) this._src.autoPlay = DeGUILayout.ToggleButton(this._src.autoPlay, new GUIContent("AutoPlay", "If selected, the tween will play automatically"));

                this._src.autoKill = DeGUILayout.ToggleButton(this._src.autoKill, new GUIContent("AutoKill", "If selected, the tween will be killed when it completes, and won't be reusable"));
                GUILayout.EndHorizontal();
                if (prevAnimType != this._src.animationType)
                {
                    // Set default optional values based on animation type
                    this._src.endValueTransform = null;
                    this._src.useTargetAsV3     = false;
                    switch (this._src.animationType)
                    {
                        case DOTweenAnimation.AnimationType.Move:
                        case DOTweenAnimation.AnimationType.LocalMove:
                        case DOTweenAnimation.AnimationType.Rotate:
                        case DOTweenAnimation.AnimationType.LocalRotate:
                        case DOTweenAnimation.AnimationType.Scale:
                            this._src.endValueV3    = Vector3.zero;
                            this._src.endValueFloat = 0;
                            this._src.optionalBool0 = this._src.animationType == DOTweenAnimation.AnimationType.Scale;

                            break;
                        case DOTweenAnimation.AnimationType.UIWidthHeight:
                            this._src.endValueV3    = Vector3.zero;
                            this._src.endValueFloat = 0;
                            this._src.optionalBool0 = this._src.animationType == DOTweenAnimation.AnimationType.UIWidthHeight;

                            break;
                        case DOTweenAnimation.AnimationType.FillAmount:
                            this._src.endValueFloat = 1;

                            break;
                        case DOTweenAnimation.AnimationType.Color:
                        case DOTweenAnimation.AnimationType.Fade:
                            this._isLightSrc        = targetGO.GetComponent<Light>() != null;
                            this._src.endValueFloat = 0;

                            break;
                        case DOTweenAnimation.AnimationType.Text:
                            this._src.optionalBool0 = true;

                            break;
                        case DOTweenAnimation.AnimationType.PunchPosition:
                        case DOTweenAnimation.AnimationType.PunchRotation:
                        case DOTweenAnimation.AnimationType.PunchScale:
                            this._src.endValueV3     = this._src.animationType == DOTweenAnimation.AnimationType.PunchRotation ? new Vector3(0, 180, 0) : Vector3.one;
                            this._src.optionalFloat0 = 1;
                            this._src.optionalInt0   = 10;
                            this._src.optionalBool0  = false;

                            break;
                        case DOTweenAnimation.AnimationType.ShakePosition:
                        case DOTweenAnimation.AnimationType.ShakeRotation:
                        case DOTweenAnimation.AnimationType.ShakeScale:
                            this._src.endValueV3     = this._src.animationType == DOTweenAnimation.AnimationType.ShakeRotation ? new Vector3(90, 90, 90) : Vector3.one;
                            this._src.optionalInt0   = 10;
                            this._src.optionalFloat0 = 90;
                            this._src.optionalBool0  = false;
                            this._src.optionalBool1  = true;

                            break;
                        case DOTweenAnimation.AnimationType.CameraAspect:
                        case DOTweenAnimation.AnimationType.CameraFieldOfView:
                        case DOTweenAnimation.AnimationType.CameraOrthoSize:
                            this._src.endValueFloat = 0;

                            break;
                        case DOTweenAnimation.AnimationType.CameraPixelRect:
                        case DOTweenAnimation.AnimationType.CameraRect:
                            this._src.endValueRect = new Rect(0, 0, 0, 0);

                            break;
                    }
                }

                if (this._src.animationType == DOTweenAnimation.AnimationType.None)
                {
                    this._src.isValid = false;
                    if (GUI.changed) EditorUtility.SetDirty(this._src);

                    return;
                }

                if (this._refreshRequired || prevAnimType != this._src.animationType || this.ComponentsChanged())
                {
                    this._refreshRequired = false;
                    this._src.isValid     = this.Validate(targetGO);
                    // See if we need to choose between multiple targets
#if false // UI_MARKER
                    if (_src.animationType == DOTweenAnimation.AnimationType.Fade && targetGO.GetComponent<CanvasGroup>() != null && targetGO.GetComponent<Image>() != null) {
                        _chooseTargetMode = ChooseTargetMode.BetweenCanvasGroupAndImage;
                        // Reassign target and forcedTargetType if lost
                        if (_src.forcedTargetType == DOTweenAnimation.TargetType.Unset) _src.forcedTargetType = _src.targetType;
                        switch (_src.forcedTargetType) {
                        case DOTweenAnimation.TargetType.CanvasGroup:
                            _src.target = targetGO.GetComponent<CanvasGroup>();
                            break;
                        case DOTweenAnimation.TargetType.Image:
                            _src.target = targetGO.GetComponent<Image>();
                            break;
                        }
                    } else {
#endif
                    this._chooseTargetMode     = ChooseTargetMode.None;
                    this._src.forcedTargetType = DOTweenAnimation.TargetType.Unset;
#if false // UI_MARKER
                    }
#endif
                }

                if (!this._src.isValid)
                {
                    GUI.color = Color.red;
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("No valid Component was found for the selected animation", EditorGUIUtils.wordWrapLabelStyle);
                    GUILayout.EndVertical();
                    GUI.color = Color.white;
                    if (GUI.changed) EditorUtility.SetDirty(this._src);

                    return;
                }

#if false // UI_MARKER
                // Special cases in which multiple target types could be used (set after validation)
                if (_chooseTargetMode == ChooseTargetMode.BetweenCanvasGroupAndImage && _src.forcedTargetType != DOTweenAnimation.TargetType.Unset) {
                    FadeTargetType fadeTargetType = (FadeTargetType)Enum.Parse(typeof(FadeTargetType), _src.forcedTargetType.ToString());
                    DOTweenAnimation.TargetType prevTargetType = _src.forcedTargetType;
                    _src.forcedTargetType =
 (DOTweenAnimation.TargetType)Enum.Parse(typeof(DOTweenAnimation.TargetType), EditorGUILayout.EnumPopup(_src.animationType + " Target", fadeTargetType).ToString());
                    if (_src.forcedTargetType != prevTargetType) {
                        // Target type change > assign correct target
                        switch (_src.forcedTargetType) {
                        case DOTweenAnimation.TargetType.CanvasGroup:
                            _src.target = targetGO.GetComponent<CanvasGroup>();
                            break;
                        case DOTweenAnimation.TargetType.Image:
                            _src.target = targetGO.GetComponent<Image>();
                            break;
                        }
                    }
                }
#endif

                GUILayout.BeginHorizontal();
                this._src.duration = EditorGUILayout.FloatField("Duration", this._src.duration);
                if (this._src.duration < 0) this._src.duration = 0;
                this._src.isSpeedBased = DeGUILayout.ToggleButton(this._src.isSpeedBased, new GUIContent("SpeedBased", "If selected, the duration will count as units/degree x second"),
                    DeGUI.styles.button.tool, GUILayout.Width(75));
                GUILayout.EndHorizontal();
                this._src.delay = EditorGUILayout.FloatField("Delay", this._src.delay);
                if (this._src.delay < 0) this._src.delay = 0;
                this._src.isIndependentUpdate = EditorGUILayout.Toggle("Ignore TimeScale", this._src.isIndependentUpdate);
                this._src.easeType            = EditorGUIUtils.FilteredEasePopup("Ease", this._src.easeType);
                if (this._src.easeType == Ease.INTERNAL_Custom) this._src.easeCurve = EditorGUILayout.CurveField("   Ease Curve", this._src.easeCurve);

                this._src.loops = EditorGUILayout.IntField(new GUIContent("Loops", "Set to -1 for infinite loops"), this._src.loops);
                if (this._src.loops < -1) this._src.loops                            = -1;
                if (this._src.loops > 1 || this._src.loops == -1) this._src.loopType = (LoopType)EditorGUILayout.EnumPopup("   Loop Type", this._src.loopType);
                this._src.id = EditorGUILayout.TextField("ID", this._src.id);

                var canBeRelative = true;
                // End value and eventual specific options
                switch (this._src.animationType)
                {
                    case DOTweenAnimation.AnimationType.Move:
                    case DOTweenAnimation.AnimationType.LocalMove:
                        this.GUIEndValueV3(targetGO, this._src.animationType == DOTweenAnimation.AnimationType.Move);
                        this._src.optionalBool0 = EditorGUILayout.Toggle("    Snapping", this._src.optionalBool0);
                        canBeRelative           = !this._src.useTargetAsV3;

                        break;
                    case DOTweenAnimation.AnimationType.Rotate:
                    case DOTweenAnimation.AnimationType.LocalRotate:
                        var isRigidbody2D = DOTweenModuleUtils.Physics.HasRigidbody2D(this._src);
                        if (isRigidbody2D)
                        {
                            this.GUIEndValueFloat();
                        }
                        else
                        {
                            this.GUIEndValueV3(targetGO);
                            this._src.optionalRotationMode = (RotateMode)EditorGUILayout.EnumPopup("    Rotation Mode", this._src.optionalRotationMode);
                        }

                        break;
                    case DOTweenAnimation.AnimationType.Scale:
                        if (this._src.optionalBool0)
                            this.GUIEndValueFloat();
                        else
                            this.GUIEndValueV3(targetGO);
                        this._src.optionalBool0 = EditorGUILayout.Toggle("Uniform Scale", this._src.optionalBool0);

                        break;
                    case DOTweenAnimation.AnimationType.UIWidthHeight:
                        if (this._src.optionalBool0)
                            this.GUIEndValueFloat();
                        else
                            this.GUIEndValueV2();
                        this._src.optionalBool0 = EditorGUILayout.Toggle("Uniform Scale", this._src.optionalBool0);

                        break;
                    case DOTweenAnimation.AnimationType.FillAmount:
                        this.GUIEndValueFloat();
                        if (this._src.endValueFloat < 0) this._src.endValueFloat = 0;
                        if (this._src.endValueFloat > 1) this._src.endValueFloat = 1;
                        canBeRelative = false;

                        break;
                    case DOTweenAnimation.AnimationType.Color:
                        this.GUIEndValueColor();
                        canBeRelative = false;

                        break;
                    case DOTweenAnimation.AnimationType.Fade:
                        this.GUIEndValueFloat();
                        if (this._src.endValueFloat < 0) this._src.endValueFloat                      = 0;
                        if (!this._isLightSrc && this._src.endValueFloat > 1) this._src.endValueFloat = 1;
                        canBeRelative = false;

                        break;
                    case DOTweenAnimation.AnimationType.Text:
                        this.GUIEndValueString();
                        this._src.optionalBool0 = EditorGUILayout.Toggle("Rich Text Enabled", this._src.optionalBool0);
                        this._src.optionalScrambleMode = (ScrambleMode)EditorGUILayout.EnumPopup("Scramble Mode", this._src.optionalScrambleMode);
                        this._src.optionalString = EditorGUILayout.TextField(new GUIContent("Custom Scramble", "Custom characters to use in case of ScrambleMode.Custom"), this._src.optionalString);

                        break;
                    case DOTweenAnimation.AnimationType.PunchPosition:
                    case DOTweenAnimation.AnimationType.PunchRotation:
                    case DOTweenAnimation.AnimationType.PunchScale:
                        this.GUIEndValueV3(targetGO);
                        canBeRelative          = false;
                        this._src.optionalInt0 = EditorGUILayout.IntSlider(new GUIContent("    Vibrato", "How much will the punch vibrate"), this._src.optionalInt0, 1, 50);
                        this._src.optionalFloat0 = EditorGUILayout.Slider(new GUIContent("    Elasticity", "How much the vector will go beyond the starting position when bouncing backwards"),
                            this._src.optionalFloat0, 0, 1);
                        if (this._src.animationType == DOTweenAnimation.AnimationType.PunchPosition) this._src.optionalBool0 = EditorGUILayout.Toggle("    Snapping", this._src.optionalBool0);

                        break;
                    case DOTweenAnimation.AnimationType.ShakePosition:
                    case DOTweenAnimation.AnimationType.ShakeRotation:
                    case DOTweenAnimation.AnimationType.ShakeScale:
                        this.GUIEndValueV3(targetGO);
                        canBeRelative          = false;
                        this._src.optionalInt0 = EditorGUILayout.IntSlider(new GUIContent("    Vibrato", "How much will the shake vibrate"), this._src.optionalInt0, 1, 50);
                        using (new GUILayout.HorizontalScope())
                        {
                            this._src.optionalFloat0              = EditorGUILayout.Slider(new GUIContent("    Randomness", "The shake randomness"), this._src.optionalFloat0, 0, 90);
                            this._src.optionalShakeRandomnessMode = (ShakeRandomnessMode)EditorGUILayout.EnumPopup(this._src.optionalShakeRandomnessMode, GUILayout.Width(70));
                        }

                        this._src.optionalBool1 = EditorGUILayout.Toggle(new GUIContent("    FadeOut", "If selected the shake will fade out, otherwise it will constantly play with full force"),
                            this._src.optionalBool1);
                        if (this._src.animationType == DOTweenAnimation.AnimationType.ShakePosition) this._src.optionalBool0 = EditorGUILayout.Toggle("    Snapping", this._src.optionalBool0);

                        break;
                    case DOTweenAnimation.AnimationType.CameraAspect:
                    case DOTweenAnimation.AnimationType.CameraFieldOfView:
                    case DOTweenAnimation.AnimationType.CameraOrthoSize:
                        this.GUIEndValueFloat();
                        canBeRelative = false;

                        break;
                    case DOTweenAnimation.AnimationType.CameraBackgroundColor:
                        this.GUIEndValueColor();
                        canBeRelative = false;

                        break;
                    case DOTweenAnimation.AnimationType.CameraPixelRect:
                    case DOTweenAnimation.AnimationType.CameraRect:
                        this.GUIEndValueRect();
                        canBeRelative = false;

                        break;
                }

                // Final settings
                if (canBeRelative) this._src.isRelative = EditorGUILayout.Toggle("    Relative", this._src.isRelative);

                // Events
                AnimationInspectorGUI.AnimationEvents(this, this._src);
            }

            EditorGUI.EndDisabledGroup();

            if (GUI.changed) EditorUtility.SetDirty(this._src);
        }

        #endregion

        #region Methods

        // Returns TRUE if the Component layout on the src gameObject changed (a Component was added or removed)
        private bool ComponentsChanged()
        {
            var prevTotComponentsOnSrc = this._totComponentsOnSrc;
            this._totComponentsOnSrc = this._src.gameObject.GetComponents<Component>().Length;

            return prevTotComponentsOnSrc != this._totComponentsOnSrc;
        }

        // Checks if a Component that can be animated with the given animationType is attached to the src
        private bool Validate(GameObject targetGO)
        {
            if (this._src.animationType == DOTweenAnimation.AnimationType.None) return false;

            Component srcTarget;
            // First check for external plugins
#if false // TK2D_MARKER
            if (_Tk2dAnimationTypeToComponent.ContainsKey(_src.animationType)) {
                foreach (Type t in _Tk2dAnimationTypeToComponent[_src.animationType]) {
                    srcTarget = targetGO.GetComponent(t);
                    if (srcTarget != null) {
                        _src.target = srcTarget;
                        _src.targetType = DOTweenAnimation.TypeToDOTargetType(t);
                        return true;
                    }
                }
            }
#endif
#if false // TEXTMESHPRO_MARKER
            if (_TMPAnimationTypeToComponent.ContainsKey(_src.animationType)) {
                foreach (Type t in _TMPAnimationTypeToComponent[_src.animationType]) {
                    srcTarget = targetGO.GetComponent(t);
                    if (srcTarget != null) {
                        _src.target = srcTarget;
                        _src.targetType = DOTweenAnimation.TypeToDOTargetType(t);
                        return true;
                    }
                }
            }
#endif
            // Then check for regular stuff
            if (_AnimationTypeToComponent.ContainsKey(this._src.animationType))
                foreach (var t in _AnimationTypeToComponent[this._src.animationType])
                {
                    srcTarget = targetGO.GetComponent(t);
                    if (srcTarget != null)
                    {
                        this._src.target     = srcTarget;
                        this._src.targetType = DOTweenAnimation.TypeToDOTargetType(t);

                        return true;
                    }
                }

            return false;
        }

        private DOTweenAnimation.AnimationType AnimationToDOTweenAnimationType(string animation)
        {
            if (_datString == null) _datString = Enum.GetNames(typeof(DOTweenAnimation.AnimationType));
            animation = animation.Replace("/", "");

            return (DOTweenAnimation.AnimationType)Array.IndexOf(_datString, animation);
        }

        private int DOTweenAnimationTypeToPopupId(DOTweenAnimation.AnimationType animation) { return Array.IndexOf(_animationTypeNoSlashes, animation.ToString()); }

        #endregion

        #region GUI Draw Methods

        private void GUIEndValueFloat()
        {
            GUILayout.BeginHorizontal();
            this.GUIToFromButton();
            this._src.endValueFloat = EditorGUILayout.FloatField(this._src.endValueFloat);
            GUILayout.EndHorizontal();
        }

        private void GUIEndValueColor()
        {
            GUILayout.BeginHorizontal();
            this.GUIToFromButton();
            this._src.endValueColor = EditorGUILayout.ColorField(this._src.endValueColor);
            GUILayout.EndHorizontal();
        }

        private void GUIEndValueV3(GameObject targetGO, bool optionalTransform = false)
        {
            GUILayout.BeginHorizontal();
            this.GUIToFromButton();
            if (this._src.useTargetAsV3)
            {
                var prevT = this._src.endValueTransform;
                this._src.endValueTransform = EditorGUILayout.ObjectField(this._src.endValueTransform, typeof(Transform), true) as Transform;
                if (this._src.endValueTransform != prevT && this._src.endValueTransform != null)
                {
#if false // UI_MARKER
                    // Check that it's a Transform for a Transform or a RectTransform for a RectTransform
                    if (targetGO.GetComponent<RectTransform>() != null) {
                        if (_src.endValueTransform.GetComponent<RectTransform>() == null) {
                            EditorUtility.DisplayDialog("DOTween Pro", "For Unity UI elements, the target must also be a UI element", "Ok");
                            _src.endValueTransform = null;
                        }
                    } else if (_src.endValueTransform.GetComponent<RectTransform>() != null) {
                        EditorUtility.DisplayDialog("DOTween Pro", "You can't use a UI target for a non UI object", "Ok");
                        _src.endValueTransform = null;
                    }
#endif
                }
            }
            else
            {
                this._src.endValueV3 = EditorGUILayout.Vector3Field("", this._src.endValueV3, GUILayout.Height(16));
            }

            if (optionalTransform)
                if (GUILayout.Button(this._src.useTargetAsV3 ? "target" : "value", EditorGUIUtils.sideBtStyle, GUILayout.Width(44)))
                    this._src.useTargetAsV3 = !this._src.useTargetAsV3;
            GUILayout.EndHorizontal();
#if false // UI_MARKER
            if (_src.useTargetAsV3 && _src.endValueTransform != null && _src.target is RectTransform) {
                EditorGUILayout.HelpBox("NOTE: when using a UI target, the tween will be created during Start instead of Awake", MessageType.Info);
            }
#endif
        }

        private void GUIEndValueV2()
        {
            GUILayout.BeginHorizontal();
            this.GUIToFromButton();
            this._src.endValueV2 = EditorGUILayout.Vector2Field("", this._src.endValueV2, GUILayout.Height(16));
            GUILayout.EndHorizontal();
        }

        private void GUIEndValueString()
        {
            GUILayout.BeginHorizontal();
            this.GUIToFromButton();
            this._src.endValueString = EditorGUILayout.TextArea(this._src.endValueString, EditorGUIUtils.wordWrapTextArea);
            GUILayout.EndHorizontal();
        }

        private void GUIEndValueRect()
        {
            GUILayout.BeginHorizontal();
            this.GUIToFromButton();
            this._src.endValueRect = EditorGUILayout.RectField(this._src.endValueRect);
            GUILayout.EndHorizontal();
        }

        private void GUIToFromButton()
        {
            if (GUILayout.Button(this._src.isFrom ? "FROM" : "TO", EditorGUIUtils.sideBtStyle, GUILayout.Width(90))) this._src.isFrom = !this._src.isFrom;
            GUILayout.Space(16);
        }

        #endregion
    }

    // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
    // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
    // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

    [InitializeOnLoad]
    internal static class Initializer
    {
        static Initializer() { DOTweenAnimation.OnReset += OnReset; }

        private static void OnReset(DOTweenAnimation src)
        {
            var settings = DOTweenUtilityWindow.GetDOTweenSettings();

            if (settings == null) return;

            Undo.RecordObject(src, "DOTweenAnimation");
            src.autoPlay = settings.defaultAutoPlay == AutoPlay.All || settings.defaultAutoPlay == AutoPlay.AutoPlayTweeners;
            src.autoKill = settings.defaultAutoKill;
            EditorUtility.SetDirty(src);
        }
    }
}