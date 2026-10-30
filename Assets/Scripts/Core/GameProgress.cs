using UnityEngine;

namespace RataBlanca.Core
{
    /// <summary>
    /// Nombres de las escenas. Si renombras una escena, cámbialo aquí.
    /// </summary>
    public static class SceneNames
    {
        public const string Menu = "MenuPrincipal";
        public const string Game = "Juego";
    }

    /// <summary>
    /// Progreso guardado entre sesiones (PlayerPrefs).
    /// Por ahora solo guarda si el tutorial ya se vio; los volúmenes los
    /// guarda el AudioManager.
    /// </summary>
    public static class GameProgress
    {
        const string TutorialSeenKey = "rb.tutorialVisto";

        /// <summary>El menú lo activa para que la escena de juego muestre el tutorial al entrar.</summary>
        public static bool ShowTutorialOnEnter;

        public static bool TutorialSeen
        {
            get => PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TutorialSeenKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>"Nuevo juego": borra el progreso (no toca los volúmenes).</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(TutorialSeenKey);
            PlayerPrefs.Save();
        }
    }
}
