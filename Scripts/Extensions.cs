#if UNITY_EDITOR
using UnityEngine;

namespace Mec2Tex
{
    internal static class Extensions
    {
        public static string ToButtonString(this BakeMode bakeMode) => bakeMode switch
        {
            BakeMode.Single => "Bake Animation (Single)",
            //BakeMode.Selection => "Bake Animations (Selection)",
            BakeMode.AllIndividual => "Bake Animations (All)",
            _ => "Cannot bake animations.  Invalid Bake Mode"
        };
    }
}
#endif