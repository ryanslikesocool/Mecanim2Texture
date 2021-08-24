#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Mec2Tex
{
    internal class ErrorUtility : EditorView
    {
        public static ErrorUtility Instance { get; private set; }

        private List<Error> errors;

        public ErrorUtility()
        {
            Instance = this;
            errors = new List<Error>();
        }

        public override void View()
        {
            foreach (Error error in errors)
            {
                GUILayout.Label(error.ToErrorString());
            }
        }

        public static bool SetError(Error error, bool condition) => Instance.ApplyError(error, condition);

        public static bool HasError(Error error) => Instance.errors.Contains(error);

        private bool ApplyError(Error error, bool condition)
        {
            if (condition && !errors.Contains(error))
            {
                errors.Add(error);
            }
            else if (!condition && errors.Contains(error))
            {
                errors.Remove(error);
            }

            return condition;
        }

        public static void ClearErrors() => Instance.errors.Clear();
    }
}
#endif