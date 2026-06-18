using UnityEngine;
using UnityEngine.Localization.Settings;

namespace BOTF3D.Core
{
    public static class Loc
    {
        private const string Table = "StringTableCollection";

        public static string Get(string key, string fallback = null)
        {
            if (!Application.isPlaying)
                return fallback ?? key;

            var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(Table, key);
            if (op.IsDone && op.OperationException == null)
                return op.Result ?? fallback ?? key;

            return fallback ?? key;
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            return string.Format(Get(key, fallback), args);
        }
    }
}
