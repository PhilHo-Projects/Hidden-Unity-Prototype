using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Core.Hidden.HiddenGameGlobals;

namespace Core.Hidden
{
    public class PowerUps : MonoBehaviour
    {
        [SerializeField] private HiddenMain hiddenMain;
        [SerializeField] private GameGUI gameGUI;
        [SerializeField] private TMP_Text specialUsedText;
        [SerializeField] private CanvasGroup[] specialsGroups;
        [SerializeField] private Button shieldButton;
        [SerializeField] private Button revealButton;
        [SerializeField] private Button extraButton;
        
        private bool _redPowerupUsed;
        private bool _greenPowerupUsed;
        private bool _bluePowerupUsed;
        
        private bool _redPowerupObtained;
        private bool _greenPowerupObtained;
        private bool _bluePowerupObtained;

        public bool redPowerActivated;

        private GameManager _gameManager;
        private bool IsPlayerTurn => hiddenMain != null && hiddenMain.IsMyTurn;

        private void Start()
        {
            _gameManager = GameManager.Instance;
            shieldButton.onClick.AddListener(ShieldSelectedPiece);
            revealButton.onClick.AddListener(RevealBoard);
            extraButton.onClick.AddListener(EnableExtraTurn);
        }

        public bool ProcessPowerup(string color)
        {
            switch (color)
            {
                case ColorGreenSelect when !_greenPowerupObtained:
                    _greenPowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(0));
                    return true;

                case ColorBlueSelect when !_bluePowerupObtained:
                    _bluePowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(1));
                    return true;

                case ColorRedSelect when !_redPowerupObtained:
                    _redPowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(2));
                    return true;
            }
            return false;
        }
        
        public void InitializePowerups()
        {
            ResetValues();
            foreach (CanvasGroup group in specialsGroups)
            {
                group.alpha = 0.1f;
                group.interactable = false;
            }
        }
        private IEnumerator ActivatePowerupUI(int index)
        {
            float duration = 0.5f;
            float startAlpha = specialsGroups[index].alpha;
            float targetAlpha = 1.0f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                specialsGroups[index].alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                yield return null;
            }

            specialsGroups[index].alpha = targetAlpha;
            specialsGroups[index].interactable = true;
        }
        private IEnumerator DeactivatePowerupUI(int index)
        {
            float duration = 0.5f;
            float startAlpha = specialsGroups[index].alpha;
            float targetAlpha = 0f;
            float elapsed = 0;

            specialsGroups[index].interactable = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                specialsGroups[index].alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                yield return null;
            }

            specialsGroups[index].alpha = targetAlpha;
        }

        private void ShieldSelectedPiece()
        {
            if (_greenPowerupUsed || !IsPlayerTurn) return;

            hiddenMain.ShieldSelectionMode = true;
            _greenPowerupUsed = true;

            _gameManager.DoTweenAnimations.RainbowText("Shield used", specialUsedText);
        }

        public void ShieldPieces(bool isOnline, GridData playerGrid)
        {
            if (_greenPowerupUsed) return;

            _gameManager.DoTweenAnimations.RainbowText("Shield used", specialUsedText);

            StartCoroutine(DeactivatePowerupUI(0));
            _greenPowerupUsed = true;
            List<int> immuneSquares = new List<int>(); 
    
            for (int i = 0; i < playerGrid.Marks.Length; i++)
            {
                if (playerGrid.Marks[i])
                {
                    Debug.Log("Shielding these indexes: " + i);
                    playerGrid.Immune[i] = true;
                    immuneSquares.Add(i);
                }
            }
    
            //if (isOnline) hiddenMain.SendImmuneStatus(immuneSquares.Select(x => (byte)x).ToArray());
            if (isOnline) hiddenMain.NetworkHandler.SendImmuneStatus(immuneSquares.Select(x => (byte)x).ToArray());
        }

        public void ShieldOpponentPieces(GridData opponentGrid, byte[] indexes)
        {
            _gameManager.DoTweenAnimations.RainbowText("Opponent used shield!!", specialUsedText);
    
            foreach (byte index in indexes)
            {
                Debug.Log("Shielding index on opponent grid: " + index);
                opponentGrid.Immune[index] = true;
        
                gameGUI.ShowShieldVisual(index, true);
            }
        }
        
        // This was for when we had the implementation of shielding every piece
        public void ResetGreenPowerUp(GridData playerGrid)
        {
            if (_greenPowerupUsed)
            {
                for (int i = 0; i < playerGrid.Marks.Length; i++)
                {
                    playerGrid.Immune[i] = false;
                }
                _greenPowerupUsed = false;

                specialUsedText.text = "";
            }
        }
        
        public void OnShieldApplied()
        {
            StartCoroutine(DeactivatePowerupUI(0));
        }

        private void RevealBoard()
        {
            if (_bluePowerupUsed || !IsPlayerTurn) return;
    
            _gameManager.DoTweenAnimations.RainbowText("Reveal board used", specialUsedText);
            StartCoroutine(DeactivatePowerupUI(1));
            gameGUI.SetSideBySideView(true);
            _bluePowerupUsed = true;
        }

        public void ResetBluePowerUp()
        {
            if (_bluePowerupUsed)
            {
                gameGUI.SetCenteredView(true);
                _bluePowerupUsed = false;
            }
        }

        private void EnableExtraTurn()
        {
            if (_redPowerupUsed || !IsPlayerTurn) return;
            redPowerActivated = true;
            _gameManager.DoTweenAnimations.RainbowText("Extra turn activated!", specialUsedText);
            StartCoroutine(DeactivatePowerupUI(2));
            _redPowerupUsed = true;
        }
        
        public void ResetRedPowerup()
        {
            if (_redPowerupUsed)
            {
                _gameManager.DoTweenAnimations.RainbowText("Extra turn applied!", specialUsedText);
                _redPowerupUsed = false;
            }
        }
        
        public void UpdatePowerupInteractivity(bool isPlayerTurn)
        {
            // Only allow interaction with powerups that are available and it's the player's turn
            if (specialsGroups[0].alpha > 0.5f && !_greenPowerupUsed)
                specialsGroups[0].interactable = isPlayerTurn;
        
            if (specialsGroups[1].alpha > 0.5f && !_bluePowerupUsed)
                specialsGroups[1].interactable = isPlayerTurn;
        
            if (specialsGroups[2].alpha > 0.5f && !_redPowerupUsed)
                specialsGroups[2].interactable = isPlayerTurn;
        }

        private void ResetValues()
        {
            _redPowerupUsed = false;
            _greenPowerupUsed = false;
            _bluePowerupUsed = false;
            _redPowerupObtained = false;
            _greenPowerupObtained = false;
            _bluePowerupObtained = false;
        }
    }
}
