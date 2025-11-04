using EFT.UI;
using EFT.UI.Screens;
using System.Reflection;
using UnityEngine;

namespace JiangHu
{
    public class RemoveAlpha : MonoBehaviour
    {
        private void Start()
        {
            CurrentScreenSingletonClass.Instance.OnScreenChanged += OnScreenChanged;
        }

        private void OnScreenChanged(EEftScreenType screenType)
        {
            if (screenType == EEftScreenType.MainMenu)
            {
                Invoke(nameof(RemoveAlphaInstant), 0.05f);
            }
        }

        private void RemoveAlphaInstant()
        {
            MenuScreen menuScreen = FindObjectOfType<MenuScreen>();
            if (menuScreen != null)
            {
                RemoveAlphaWarning(menuScreen);
            }
        }

        private void RemoveAlphaWarning(MenuScreen menuScreen)
        {
            try
            {
                FieldInfo field = typeof(MenuScreen).GetField("_alphaWarningGameObject",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    field = typeof(MenuScreen).GetField("_warningGameObject",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (field != null)
                {
                    GameObject alphaWarning = field.GetValue(menuScreen) as GameObject;
                    if (alphaWarning != null && alphaWarning.activeInHierarchy)
                    {
                        alphaWarning.SetActive(false);
                    }
                }
            }
            catch (System.Exception)
            {
            }
        }

        private void OnDestroy()
        {
            if (CurrentScreenSingletonClass.Instance != null)
                CurrentScreenSingletonClass.Instance.OnScreenChanged -= OnScreenChanged;
        }
    }
}