namespace Player.StateMachine
{
    using UnityEngine;
    
    /// <summary>
    /// Resolves movement input to 8-directional animation suffixes.
    /// Maps 2D input vectors to the 8 cardinal/diagonal directions used in animation naming.
    /// </summary>
    public static class DirectionResolver
    {
        /// <summary>
        /// Eight-way directional enumeration matching animation naming conventions.
        /// </summary>
        public enum Direction
        {
            None,
            Forward,
            ForwardLeft,
            ForwardRight,
            Left,
            Right,
            Backward,
            BackwardLeft,
            BackwardRight
        }
        
        private const float DeadZone = 0.01f;
        
        /// <summary>
        /// Get the direction from 2D input.
        /// </summary>
        /// <param name="input">Movement input (x = strafe, y = forward/back)</param>
        /// <returns>The closest 8-way direction</returns>
        public static Direction GetDirection(Vector2 input)
        {
            // Check for dead zone
            if (input.sqrMagnitude < DeadZone * DeadZone)
            {
                return Direction.None;
            }
            
            // Calculate angle in degrees (-180 to 180)
            // Atan2(y, x) gives angle from positive X-axis
            // We treat Y as forward, X as right
            float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            
            // Map angle to 8 directions
            // Each direction covers 45 degrees
            // Forward: -22.5° to 22.5°
            // ForwardRight: 22.5° to 67.5°
            // Right: 67.5° to 112.5°
            // BackwardRight: 112.5° to 157.5°
            // Backward: 157.5° to 180° and -180° to -157.5°
            // BackwardLeft: -157.5° to -112.5°
            // Left: -112.5° to -67.5°
            // ForwardLeft: -67.5° to -22.5°
            
            if (angle >= -22.5f && angle < 22.5f)
            {
                return Direction.Forward;
            }
            else if (angle >= 22.5f && angle < 67.5f)
            {
                return Direction.ForwardRight;
            }
            else if (angle >= 67.5f && angle < 112.5f)
            {
                return Direction.Right;
            }
            else if (angle >= 112.5f && angle < 157.5f)
            {
                return Direction.BackwardRight;
            }
            else if (angle >= 157.5f || angle < -157.5f)
            {
                return Direction.Backward;
            }
            else if (angle >= -157.5f && angle < -112.5f)
            {
                return Direction.BackwardLeft;
            }
            else if (angle >= -112.5f && angle < -67.5f)
            {
                return Direction.Left;
            }
            else // angle >= -67.5f && angle < -22.5f
            {
                return Direction.ForwardLeft;
            }
        }
        
        /// <summary>
        /// Get the animation suffix for a direction (e.g., "FL" for ForwardLeft).
        /// </summary>
        public static string GetSuffix(Direction direction)
        {
            switch (direction)
            {
                case Direction.Forward:
                    return "F";
                case Direction.ForwardLeft:
                    return "FL";
                case Direction.ForwardRight:
                    return "FR";
                case Direction.Left:
                    return "L";
                case Direction.Right:
                    return "R";
                case Direction.Backward:
                    return "B";
                case Direction.BackwardLeft:
                    return "BL";
                case Direction.BackwardRight:
                    return "BR";
                case Direction.None:
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// Get the animation suffix directly from input.
        /// </summary>
        public static string GetSuffix(Vector2 input)
        {
            Direction direction = GetDirection(input);
            return GetSuffix(direction);
        }
        
        /// <summary>
        /// Check if direction is a cardinal direction (F, B, L, R).
        /// Used because some animations only have cardinal variants.
        /// </summary>
        public static bool IsCardinal(Direction direction)
        {
            return direction == Direction.Forward ||
                   direction == Direction.Backward ||
                   direction == Direction.Left ||
                   direction == Direction.Right;
        }
        
        /// <summary>
        /// Get the nearest cardinal direction (for fallback when diagonals unavailable).
        /// Diagonal directions are mapped to their primary component.
        /// </summary>
        public static Direction GetNearestCardinal(Direction direction)
        {
            switch (direction)
            {
                case Direction.Forward:
                case Direction.ForwardLeft:
                case Direction.ForwardRight:
                    return Direction.Forward;
                    
                case Direction.Backward:
                case Direction.BackwardLeft:
                case Direction.BackwardRight:
                    return Direction.Backward;
                    
                case Direction.Left:
                    return Direction.Left;
                    
                case Direction.Right:
                    return Direction.Right;
                    
                case Direction.None:
                default:
                    return Direction.None;
            }
        }
        
        /// <summary>
        /// Get the cardinal suffix (for Start/Stop animations that may not have diagonals).
        /// </summary>
        public static string GetCardinalSuffix(Vector2 input)
        {
            Direction direction = GetDirection(input);
            Direction cardinalDirection = GetNearestCardinal(direction);
            return GetSuffix(cardinalDirection);
        }
    }
}
