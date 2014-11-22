using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranslationTool.mod
{
    public enum Language
    {
        ENG,
        RU,
        DE,
        FR,
        SP

    }

    class Settings
    {
        public int usedFont = 0;
        public Language usedLanguage = Language.ENG;

    }
}
