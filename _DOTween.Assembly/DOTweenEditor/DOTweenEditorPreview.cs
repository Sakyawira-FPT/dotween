﻿// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2018/08/17 12:18
// License Copyright (c) Daniele Giardini
// This work is subject to the terms at http://dotween.demigiant.com/license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DG.DOTweenEditor
{
    public static class DOTweenEditorPreview
    {
        public static bool isPreviewing { get; private set; }

        static double _previewTime;
        static Action _onPreviewUpdated;
        static Object[] _uiGraphics;
        static readonly Type _TGraphic;
        static readonly List<Tween> _Tweens = new List<Tween>();

        static DOTweenEditorPreview()
        {
            Assembly uiAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "UnityEngine.UI");
            if (uiAssembly != null) {
                _TGraphic = uiAssembly.GetType("UnityEngine.UI.Graphic");
            }
        }

        #region Public Methods

        /// <summary>
        /// Starts the update loop of tween in the editor. Has no effect during playMode.
        /// </summary>
        /// <param name="onPreviewUpdated">Eventual callback to call after every update</param>
        public static void Start(Action onPreviewUpdated = null)
        {
            if (isPreviewing || EditorApplication.isPlayingOrWillChangePlaymode) return;

            isPreviewing = true;
            _onPreviewUpdated = onPreviewUpdated;
            _previewTime = EditorApplication.timeSinceStartup;
            if (_TGraphic != null) _uiGraphics = Object.FindObjectsOfType(_TGraphic);
            else _uiGraphics = null;
            EditorApplication.update += PreviewUpdate;
        }

        /// <summary>
        /// Stops the update loop and clears the onPreviewUpdated callback.
        /// </summary>
        /// <param name="resetTweenTargets">If TRUE also resets the tweened objects to their original state.
        /// Note that this works by calling Rewind on all tweens, so it will work correctly
        /// only if you have a single tween type per object and it wasn't killed</param>
        /// <param name="clearTweens">If TRUE also kills any cached tween</param>
        public static void Stop(bool resetTweenTargets = false, bool clearTweens = true)
        {
            isPreviewing = false;
            _uiGraphics = null;
            EditorApplication.update -= PreviewUpdate;
            _onPreviewUpdated = null;
            if (resetTweenTargets) {
                foreach (Tween t in _Tweens) {
                    try {
                        if (t.isFrom) t.Complete();
                        else t.Rewind();
                    } catch {
                        // Ignore
                    }
                }
            }
            if (clearTweens) _Tweens.Clear();
            else ValidateTweens();
        }

        /// <summary>
        /// Readies the tween for editor preview by setting its UpdateType to Manual plus eventual extra settings.
        /// </summary>
        /// <param name="t">The tween to ready</param>
        /// <param name="clearCallbacks">If TRUE (recommended) removes all callbacks (OnComplete/Rewind/etc)</param>
        /// <param name="preventAutoKill">If TRUE prevents the tween from being auto-killed at completion</param>
        /// <param name="andPlay">If TRUE starts playing the tween immediately</param>
        public static void PrepareTweenForPreview(Tween t, bool clearCallbacks = true, bool preventAutoKill = true, bool andPlay = true)
        {
            _Tweens.Add(t);
            t.SetUpdate(UpdateType.Manual);
            if (preventAutoKill) t.SetAutoKill(false);
            if (clearCallbacks) {
                t.OnComplete(null)
                    .OnStart(null).OnPlay(null).OnPause(null).OnUpdate(null).OnWaypointChange(null)
                    .OnStepComplete(null).OnRewind(null).OnKill(null);
            }
            if (andPlay) t.Play();
        }

        #endregion

        #region Methods

        static void PreviewUpdate()
        {
            double currTime = _previewTime;
            _previewTime = EditorApplication.timeSinceStartup;
            float elapsed = (float)(_previewTime - currTime);
            DOTween.ManualUpdate(elapsed, elapsed);
            // Force visual refresh of UI objects
            // (a simple SceneView.RepaintAll won't work with UI elements)
            if (_uiGraphics != null) {
                foreach (Object obj in _uiGraphics) EditorUtility.SetDirty(obj);
            }

            if (_onPreviewUpdated != null) _onPreviewUpdated();
        }

        static void ValidateTweens()
        {
            for (int i = _Tweens.Count - 1; i > -1; --i) {
                if (_Tweens[i] == null || !_Tweens[i].active) _Tweens.RemoveAt(i);
            }
        }

        #endregion
    }
}