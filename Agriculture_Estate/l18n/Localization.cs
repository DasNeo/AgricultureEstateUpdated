using System.Collections.Generic;
using TaleWorlds.Localization;

namespace AgricultureEstate.l18n
{
    internal class Localization
    {
        public static TextObject SetTextVariables(string text, params KeyValuePair<string, string?>[] args)
        {
            TextObject textObject = new TextObject(text);
            foreach (var arg in args)
            {
                string? value = arg.Value;
                if (arg.Key == "GOLD_ICON")
                    value = "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">";
                textObject.SetTextVariable(arg.Key, value);
            }
            return textObject;
        }
    }
}
