using UnityEngine;

namespace Core.PongGame
{
    public class PongPhysics
    {
        private struct Bounds
        {
            public Vector2 Min;
            public Vector2 Max;

            public static Bounds FromPositionAndSize(Vector2 position, Vector2 size)
            {
                Vector2 halfSize = size * 0.5f;
                return new Bounds
                {
                    Min = position - halfSize,
                    Max = position + halfSize
                };
            }

            public readonly  Vector2 GetSize()
            {
                return Max - Min;
            }
        }

        private readonly float _ballSpeed;
        private Vector2 _ballDirection;
        private Vector2 _previousBallPosition;
        private readonly Bounds _ballBounds;
        private readonly Bounds _leftPaddleBounds;
        private readonly Bounds _rightPaddleBounds;
        private readonly Bounds _topWallBounds;
        private readonly Bounds _bottomWallBounds;
        private readonly Bounds _leftWallBounds;
        private readonly Bounds _rightWallBounds;

        public PongPhysics(
            float ballSpeed,
            GameObject ball,
            GameObject leftPaddle,
            GameObject rightPaddle,
            GameObject topWall,
            GameObject bottomWall,
            GameObject leftWall,
            GameObject rightWall)
        {
            _ballSpeed = ballSpeed;
            
            // Store initial bounds for each object
            _ballBounds = GetObjectBounds(ball);
            _leftPaddleBounds = GetObjectBounds(leftPaddle);
            _rightPaddleBounds = GetObjectBounds(rightPaddle);
            _topWallBounds = GetObjectBounds(topWall);
            _bottomWallBounds = GetObjectBounds(bottomWall);
            _leftWallBounds = GetObjectBounds(leftWall);
            _rightWallBounds = GetObjectBounds(rightWall);
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            Transform objTransform = obj.transform;

            // Get the object's position and world scale
            Vector2 position = objTransform.position;
            Vector2 size = new Vector2(
                objTransform.lossyScale.x,
                objTransform.lossyScale.y
            );

            // Create bounds from position and size
            return Bounds.FromPositionAndSize(position, size);
        }

        public Vector2 UpdateBallPosition(Vector2 currentBallPosition, Vector2 leftPaddlePos, Vector2 rightPaddlePos, float deltaTime, out bool leftScore, out bool rightScore)
        {
            leftScore = false;
            rightScore = false;
            
            _previousBallPosition = currentBallPosition;
            Vector2 nextPosition = currentBallPosition + _ballSpeed * deltaTime * _ballDirection;

            // Create ball bounds at next position
            Bounds nextBallBounds = Bounds.FromPositionAndSize(nextPosition, _ballBounds.GetSize());

            // Check paddle collisions
            Bounds leftPaddleBounds = Bounds.FromPositionAndSize(leftPaddlePos, _leftPaddleBounds.GetSize());
            Bounds rightPaddleBounds = Bounds.FromPositionAndSize(rightPaddlePos, _rightPaddleBounds.GetSize());

            if (CheckCollision(nextBallBounds, leftPaddleBounds))
            {
                nextPosition.x = leftPaddleBounds.Max.x + _ballBounds.GetSize().x * 0.5f;
                HandlePaddleCollision(leftPaddlePos, _leftPaddleBounds.GetSize());
            }
            else if (CheckCollision(nextBallBounds, rightPaddleBounds))
            {
                nextPosition.x = rightPaddleBounds.Min.x - _ballBounds.GetSize().x * 0.5f;
                HandlePaddleCollision(rightPaddlePos, _rightPaddleBounds.GetSize());
            }
            
            // Debug visualization
            DrawBounds(leftPaddleBounds, Color.cyan);
            DrawBounds(rightPaddleBounds, Color.cyan);

            // Check wall collisions
            if (nextPosition.y + _ballBounds.GetSize().y * 0.5f > _topWallBounds.Min.y || 
                nextPosition.y - _ballBounds.GetSize().y * 0.5f < _bottomWallBounds.Max.y)
            {
                _ballDirection = new Vector2(_ballDirection.x, -_ballDirection.y);
            }

            // Check score triggers
            if (nextPosition.x < _leftWallBounds.Max.x)
            {
                rightScore = true;
                return currentBallPosition;
            }
            if (nextPosition.x > _rightWallBounds.Min.x)
            {
                leftScore = true;
                return currentBallPosition;
            }

            return nextPosition;
        }

        private bool CheckCollision(Bounds a, Bounds b)
        {
            return a.Max.x > b.Min.x &&
                   a.Min.x < b.Max.x &&
                   a.Max.y > b.Min.y &&
                   a.Min.y < b.Max.y;
        }

        private void HandlePaddleCollision(Vector2 paddlePosition, Vector2 paddleSize)
        {
            // Reverse horizontal direction
            _ballDirection.x = -_ballDirection.x;

            // Calculate relative impact point (-1 to 1) using actual paddle size
            float relativeIntersectY = (_previousBallPosition.y - paddlePosition.y) / (paddleSize.y * 0.5f);
    
            // Convert to angle (keep existing code)
            float maxAngle = 75f * Mathf.Deg2Rad;
            float bounceAngle = relativeIntersectY * maxAngle;
    
            _ballDirection = new Vector2(
                Mathf.Sign(_ballDirection.x) * Mathf.Cos(bounceAngle),
                Mathf.Sin(bounceAngle)
            ).normalized;
        }
        
        public void ResetBall(Vector2 initialPosition, bool serveToLeft)
        {
            float randomY = Random.Range(-0.2f, 0.2f);
            _ballDirection = new Vector2(serveToLeft ? 1 : -1, randomY).normalized;
            _previousBallPosition = initialPosition;
        }
        
        private void DrawBounds(Bounds bounds, Color color)
        {
            Vector2[] corners = {
                new(bounds.Min.x, bounds.Min.y),
                new(bounds.Max.x, bounds.Min.y),
                new(bounds.Max.x, bounds.Max.y),
                new(bounds.Min.x, bounds.Max.y)
            };

            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(corners[i], corners[(i + 1) % 4], color);
            }
        }

    }
}
