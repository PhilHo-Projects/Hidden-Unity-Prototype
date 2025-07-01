using System.Collections;
using Core.WebSocket;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Image = UnityEngine.UI.Image;
using Core.Singletons;
using UnityEngine.InputSystem;

namespace Core.PongGame
{
    public class PongGameManager : MonoBehaviour
    {
        [Header("Game Objects")]
        [SerializeField] private GameObject leftPaddle;
        [SerializeField] private GameObject rightPaddle;
        [SerializeField] private GameObject ball;
        [SerializeField] private GameObject topWall;
        [SerializeField] private GameObject bottomWall;
        [SerializeField] private GameObject leftWall;
        [SerializeField] private GameObject rightWall;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI leftScoreText;
        [SerializeField] private TextMeshProUGUI rightScoreText;
        [SerializeField] private TextMeshProUGUI leftMessageText;
        [SerializeField] private TextMeshProUGUI rightMessageText;
        [SerializeField] private Image resetTimerImage;
        [SerializeField] private CanvasGroup messages;
        [SerializeField] private CanvasGroup endGameButtons;
        [SerializeField] private Button restartGameButton;
        [SerializeField] private Button exitPongGameButton;
        [SerializeField] private Button exitPongGameButtonTwo;

        [Header("Game Settings")]
        [SerializeField] private float paddleSpeed = 5f;
        [SerializeField] private float ballSpeed = 8f;
        [SerializeField] private float resetTime = 2f;
        [SerializeField] private int winningScore = 5;
        
        [Header("AI Settings")]
        [SerializeField] private bool useAI = true;
        [SerializeField] private bool goodAI = true;
        [SerializeField] private float aiSpeed = 5f;
        [SerializeField] private float badAIChangeDirectionInterval = 1f;
        private float _badAITimer;
        private int _badAIDirection = 1;
        
        private int _leftScore;
        private int _rightScore;
        private bool _isGameActive;
        private bool _isResetting;

        private Vector3 _initialBallPosition;
        private PongPhysics _physics;

        private byte ClientId => WebSocketNetworkHandler.Instance.ClientId;
        private MovementHandler _movementHandler;

        private GameObject ActivePaddle => ClientId % 2 == 1 ? leftPaddle : rightPaddle;
        private string PaddleId => ActivePaddle.gameObject.name;
        
        private GameObject OtherPaddle => ClientId % 2 == 1 ? rightPaddle : leftPaddle;
        private string OtherPaddleId => OtherPaddle.gameObject.name;
        
        private Vector2 _paddleInput = Vector2.zero;
        private void Start()
        {
            _initialBallPosition = Vector3.zero;
            _physics = InitializePhysics();
            endGameButtons.alpha = 0;

            ResetGame();
            Time.fixedDeltaTime = 0.01f;
            _movementHandler = WebSocketNetworkHandler.Instance.MovementHandler;
            _movementHandler.RegisterObject(PaddleId, ActivePaddle);
            _movementHandler.RegisterObject(OtherPaddleId, OtherPaddle);
            exitPongGameButton.onClick.AddListener(() =>
            {
                CleanUpPong();
                SceneLoader.Instance.LoadScene("Intro");
            });
            exitPongGameButtonTwo.onClick.AddListener(() => SceneLoader.Instance.LoadScene("Intro"));
            restartGameButton.onClick.AddListener(ResetGame);

            InputManager.Instance.InputActionHandlers["Move"].Performed += DetectInput;
            InputManager.Instance.InputActionHandlers["Move"].Canceled += DetectInput;
            JoinPongRoom();
        }

        private void Update()
        {
            if (!_isGameActive) return;
            AIPlayer();
        }

        private void FixedUpdate()
        {
            if (!_isGameActive) return;
            
            HandlePaddleMovement();

            if (_isResetting) return;

            Vector2 newBallPosition = _physics.UpdateBallPosition(
                ball.transform.position,
                leftPaddle.transform.position,
                rightPaddle.transform.position,
                Time.fixedDeltaTime,
                out var leftScored,
                out var rightScored
            );

            if (leftScored || rightScored)
                OnBallScore(leftScored);
            else
                ball.transform.position = newBallPosition;
        }
        private void JoinPongRoom()
        {
            var joinData = new StringPacket
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomJoin,
                Text = "pongRoom"
            };

            Debug.Log(" did i really join?");
            WebSocketNetworkHandler.Instance.SendPacket(joinData);
        }
        private PongPhysics InitializePhysics()
        {
            return new PongPhysics(
                ballSpeed,
                ball,
                leftPaddle,
                rightPaddle,
                topWall,
                bottomWall,
                leftWall,
                rightWall
            );
        }

        private void DetectInput(InputAction.CallbackContext context)
        {                
            _paddleInput = context.ReadValue<Vector2>();
        }


        private void HandlePaddleMovement()
        {
            Vector3 movement = new Vector3(0, _paddleInput.y, 0);

            if (movement != Vector3.zero)
            {
                Vector3 newPosition = ActivePaddle.transform.position + paddleSpeed * Time.deltaTime * movement;

                float paddleHeight = ActivePaddle.transform.lossyScale.y;
                float topLimit = topWall.transform.position.y - topWall.transform.lossyScale.y * 0.5f;
                float bottomLimit = bottomWall.transform.position.y + bottomWall.transform.lossyScale.y * 0.5f;

                newPosition.y = Mathf.Clamp(
                    newPosition.y,
                    bottomLimit + paddleHeight * 0.5f,
                    topLimit - paddleHeight * 0.5f
                );

                ActivePaddle.transform.position = newPosition;

                SendPaddlePosition();
            }
        }


        private void ResetGame()
        {
            _leftScore = 0;
            _rightScore = 0;
            UpdateScores();
            StartCoroutine(ResetBallWithServe(Random.value > 0.5f));
            leftMessageText.text = "";
            rightMessageText.text = "";
            _isGameActive = true;
            endGameButtons.alpha = 0;

        }

        private void UpdateScores()
        {
            leftScoreText.text = _leftScore.ToString();
            rightScoreText.text = _rightScore.ToString();
        }

        private void OnBallScore(bool leftSideScored)
        {
            if (leftSideScored)
                _leftScore++;
            else
                _rightScore++;
            
            UpdateScores();
            ball.gameObject.SetActive(false);
            if (_leftScore >= winningScore || _rightScore >= winningScore)
                GameOver();
            else
                StartCoroutine(ResetBallWithServe(leftSideScored));
        }
        
        private IEnumerator ResetBallWithServe(bool leftSideScored)
        {
            _isResetting = true;
            resetTimerImage.fillAmount = 1f;
            resetTimerImage.DOFillAmount(0f, resetTime);
    
            yield return new WaitForSeconds(resetTime);
            ball.transform.position = _initialBallPosition;
            ball.gameObject.SetActive(true);
            _isResetting = false;
            _physics.ResetBall(_initialBallPosition, leftSideScored);
        }

        private void GameOver()
        {
            _isGameActive = false;
    
            if (_leftScore > _rightScore)
                leftMessageText.text = "Left Wins!";
            else
                rightMessageText.text = "Right Wins!";
            
            endGameButtons.alpha = 1;
        }
        
        private void SendPaddlePosition()
        {
            _movementHandler.SendPositionUpdate(PaddleId, ActivePaddle.transform.position);
        }
        
        private void AIPlayer()
        {
            if (!useAI) return;

            if (goodAI)
            {
                // Good AI: Align paddle with ball's y-position
                float targetY = ball.transform.position.y;
                float newY = Mathf.MoveTowards(rightPaddle.transform.position.y, targetY, aiSpeed * Time.deltaTime);
                rightPaddle.transform.position = new Vector3(rightPaddle.transform.position.x, newY, rightPaddle.transform.position.z);
            }
            else
            {
                // Bad AI: Move up and down randomly
                _badAITimer -= Time.deltaTime;
                if (_badAITimer <= 0)
                {
                    _badAIDirection = Random.value > 0.5f ? 1 : -1;
                    _badAITimer = badAIChangeDirectionInterval;
                }

                float newY = rightPaddle.transform.position.y + (_badAIDirection * aiSpeed * Time.deltaTime);
        
                // Ensure the paddle stays within bounds
                float paddleHalfHeight = rightPaddle.transform.lossyScale.y * 0.5f;
                float topLimit = topWall.transform.position.y - topWall.transform.lossyScale.y * 0.5f - paddleHalfHeight;
                float bottomLimit = bottomWall.transform.position.y + bottomWall.transform.lossyScale.y * 0.5f + paddleHalfHeight;
                newY = Mathf.Clamp(newY, bottomLimit, topLimit);

                rightPaddle.transform.position = new Vector3(rightPaddle.transform.position.x, newY, rightPaddle.transform.position.z);
            }
        }

        private void CleanUpPong()
        {
            var leaveData = new StringPacket
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomLeave,
                Text = "pongRoom"
            };
            WebSocketNetworkHandler.Instance.SendPacket(leaveData);
            
            InputManager.Instance.InputActionHandlers["Move"].Performed -= DetectInput;
            InputManager.Instance.InputActionHandlers["Move"].Canceled -= DetectInput;
            
            restartGameButton.onClick.RemoveListener(ResetGame);
            exitPongGameButton.onClick.RemoveAllListeners();
            exitPongGameButtonTwo.onClick.RemoveAllListeners();
            
            _movementHandler.UnregisterObject(PaddleId);
            _movementHandler.UnregisterObject(OtherPaddleId);
            _movementHandler = null;
            
            // Reset game state
            _leftScore = 0;
            _rightScore = 0;
            _isGameActive = false;
            _isResetting = false;
            _paddleInput = Vector2.zero;
            _badAITimer = 0f;
            _badAIDirection = 1;
            endGameButtons.alpha = 0;


            resetTimerImage.DOKill();
            leftMessageText.text = "";
            rightMessageText.text = "";
            _initialBallPosition = Vector3.zero;

        }

    }
}
