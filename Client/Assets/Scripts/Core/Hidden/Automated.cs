using System.Collections.Generic;
using UnityEngine.UI;
using static Core.Hidden.HiddenGameGlobals;
using Random = UnityEngine.Random;

namespace Core.Hidden
{
    public class Automated 
    {
        private readonly HiddenMain _gameRef;
        public Automated(HiddenMain gameReference) 
        {
            _gameRef = gameReference;
        }
        
        // Returns the position that was played, or -1 if no move was made
        public int SimulateOpponentMove(GridData opponentGrid, Image[] otherBoard)
        {
            List<int> availablePositions = new List<int>();
            for (int i = 0; i < opponentGrid.Marks.Length; i++)
            {
                if (!opponentGrid.Marks[i])
                {
                    availablePositions.Add(i);
                }
            }

            if (availablePositions.Count > 0)
            {
                int randomPosition = availablePositions[Random.Range(0, availablePositions.Count)];
                string[] colors = { ColorGreenSelect, ColorBlueSelect, ColorRedSelect };
                string randomColor = colors[Random.Range(0, colors.Length)];

                opponentGrid.Marks[randomPosition] = true;
                opponentGrid.Color[randomPosition] = randomColor;
                
                otherBoard[randomPosition].color = GetColorFromHex(randomColor);
                
                return randomPosition;
            }
            
            return -1;
        }
        
        public void ForcePlayerMove(GridData playerGrid)
        {
            List<int> availablePositions = new List<int>();
            for (int i = 0; i < playerGrid.Marks.Length; i++)
            {
                if (!playerGrid.Marks[i])
                {
                    availablePositions.Add(i);
                }
            }

            if (availablePositions.Count > 0)
            {
                int randomPosition = availablePositions[Random.Range(0, availablePositions.Count)];
                string[] colors = { ColorGreenSelect, ColorBlueSelect, ColorRedSelect };
                string randomColor = colors[Random.Range(0, colors.Length)];

                _gameRef.ApplyMove(randomPosition, randomColor);
            }
        }

        public void ForceShieldSelection(GridData playerGrid)
        {
            List<int> markedPositions = new List<int>();
            for (int i = 0; i < playerGrid.Marks.Length; i++)
            {
                // Only include positions that have the player's pieces
                if (playerGrid.Marks[i] && !string.IsNullOrEmpty(playerGrid.Color[i]))
                {
                    markedPositions.Add(i);
                }
            }

            if (markedPositions.Count > 0)
            {
                int randomPosition = markedPositions[Random.Range(0, markedPositions.Count)];
                _gameRef.ApplyShield(randomPosition);
            }
            else
            {
                _gameRef.ShieldSelectionMode = false;
            }
        }


    }
}