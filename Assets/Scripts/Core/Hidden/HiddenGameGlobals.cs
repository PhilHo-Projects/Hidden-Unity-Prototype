using UnityEngine;

namespace Core.Hidden
{
    public static class HiddenGameGlobals
    {
        public struct GridData
        {
            public bool[] Marks;
            public string[] Color;
            public bool[] Immune;
        }

        public enum GameState
        {
            SetupAI,
            Setup,
            Battle,
            EndGame
        }
        
        public enum TMPTextType
        {
            PlayerName,
            PlayerTurn,
            OpponentTurn,
            CurrentRound,
            Announcement,
            GameOver,
            PlayerScore,
            OpponentScore,
            PlayAgain,
            Countdown
        }
        
        public const string ColorGreenSelect = "#A6E22E";
        public const string ColorBlueSelect = "#4591DB";
        public const string ColorRedSelect = "#CC3941";
        
        public static Color ColorYellowBlinker => GetColorFromHex("#D7AF13");
        public static Color ColorRedBlinker => GetColorFromHex("#FF6265");
        

        public static readonly Vector3 CenterPosition = new Vector3(0, 0, 0);
        public static readonly Vector3 LeftPosition = new Vector3(-450, 0, 0);
        public static readonly Vector3 RightPosition = new Vector3(450, 0, 0);
        public static readonly Vector3 OffscreenPosition = new Vector3(1500, 0, 0);

        public const float SlideDuration = 0.5f;
        public const float FadeDuration = 0.3f;
        
        
        public static Color GetColorFromHex(string hexColor)
        {
            ColorUtility.TryParseHtmlString(hexColor, out Color color);
            return color;
        }

    }
}