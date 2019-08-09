﻿using System;
using BepInEx;
using Harmony;
using KKAPI.Utilities;
using XUnity.AutoTranslator.Plugin.Core;
using LogLevel = BepInEx.Logging.LogLevel;

namespace KK_QuickAccessBox
{
    public static class TranslationHelper
    {
        private static readonly Action<string, Action<string>> _translatorCallback;
        private static readonly Traverse _translatorGet;

        static TranslationHelper()
        {
            var dtl = Traverse.Create(Type.GetType("DynamicTranslationLoader.Text.TextTranslator, DynamicTranslationLoader", false));
            // public static string TryGetTranslation(string toTranslate)
            _translatorGet = dtl.Method("TryGetTranslation", new[] {typeof(string)});
            if (!_translatorGet.MethodExists())
                Logger.Log(LogLevel.Warning, "[KK_QuickAccessBox] Could not find method DynamicTranslationLoader.Text.TextTranslator.TryGetTranslation, item translations will be limited or unavailable");

            var xua = Type.GetType("XUnity.AutoTranslator.Plugin.Core.AutoTranslator, XUnity.AutoTranslator.Plugin.Core", false);
            if (xua != null)
                _translatorCallback = (s, action) => AutoTranslator.Default.TranslateAsync(
                    s, result =>
                    {
                        if (result.Succeeded) action(result.TranslatedText);
                    });
            else
            {
                Logger.Log(LogLevel.Warning, "[KK_QuickAccessBox] Could not find method AutoTranslator.Default.TranslateAsync, item translations will be limited or unavailable");
                _translatorCallback = null;
            }
        }

        public static void Translate(string input, Action<string> updateAction)
        {
            if (_translatorGet.MethodExists())
            {
                var result = _translatorGet.GetValue<string>(input);
                if (result != null)
                {
                    updateAction(result);
                    return;
                }
            }

            // Make sure there's a valid value set in case we need to wait
            updateAction(input);

            if (_translatorCallback != null)
            {
                // XUA needs to run on the main thread
                ThreadingHelper.StartSyncInvoke(() => _translatorCallback(input, updateAction));
            }
        }
    }
}
