namespace Player.StateMachine
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    
    public class WeightedLocomotion
    {
        public enum Phase
        {
            Inactive,
            Start,
            Loop,
            Stop
        }
        
        public Phase CurrentPhase { get; private set; } = Phase.Inactive;
        public bool IsInterruptible => CurrentPhase == Phase.Start || CurrentPhase == Phase.Stop;
        public bool IsLooping => CurrentPhase == Phase.Loop;
        
        private readonly Animator animator;
        private readonly Func<string> getStartAnimation;
        private readonly Func<string> getLoopAnimation;
        private readonly Func<string> getStopAnimation;
        private readonly float startTransitionDuration;
        private readonly float loopTransitionDuration;
        private readonly float stopTransitionDuration;
        
        private static readonly HashSet<int> _loggedMissingStates = new();
        private static readonly int IsEquippedHash = Animator.StringToHash("IsEquipped");
        
        public WeightedLocomotion(
            Animator animator,
            Func<string> getStartAnimation,
            Func<string> getLoopAnimation,
            Func<string> getStopAnimation,
            float startDuration = 0.1f,
            float loopDuration = 0.25f,
            float stopDuration = 0.1f)
        {
            this.animator = animator;
            this.getStartAnimation = getStartAnimation;
            this.getLoopAnimation = getLoopAnimation;
            this.getStopAnimation = getStopAnimation;
            this.startTransitionDuration = startDuration;
            this.loopTransitionDuration = loopDuration;
            this.stopTransitionDuration = stopDuration;
        }
        
        public void Begin()
        {
            CurrentPhase = Phase.Start;
            string startAnim = getStartAnimation();
            SafeCrossFade(startAnim, startTransitionDuration);
        }
        
        public void RequestStop()
        {
            if (CurrentPhase == Phase.Loop)
            {
                CurrentPhase = Phase.Stop;
                string stopAnim = getStopAnimation();
                SafeCrossFade(stopAnim, stopTransitionDuration);
            }
        }
        
        public void ForceLoop()
        {
            CurrentPhase = Phase.Loop;
            string loopAnim = getLoopAnimation();
            SafeCrossFade(loopAnim, loopTransitionDuration);
        }
        
        public void Reset()
        {
            CurrentPhase = Phase.Inactive;
        }
        
        public bool Update(bool hasInput)
        {
            switch (CurrentPhase)
            {
                case Phase.Inactive:
                    return false;
                
                case Phase.Start:
                    if (IsStartComplete())
                    {
                        CurrentPhase = Phase.Loop;
                        string loopAnim = getLoopAnimation();
                        SafeCrossFade(loopAnim, loopTransitionDuration);
                    }
                    return false;
                
                case Phase.Loop:
                    if (!hasInput)
                    {
                        CurrentPhase = Phase.Stop;
                        string stopAnim = getStopAnimation();
                        SafeCrossFade(stopAnim, stopTransitionDuration);
                    }
                    return false;
                
                case Phase.Stop:
                    if (hasInput)
                    {
                        CurrentPhase = Phase.Loop;
                        string loopAnim = getLoopAnimation();
                        SafeCrossFade(loopAnim, loopTransitionDuration);
                        return false;
                    }
                    
                    if (IsStopComplete())
                    {
                        CurrentPhase = Phase.Inactive;
                        return true;
                    }
                    return false;
                
                default:
                    return false;
            }
        }
        
        public bool IsStartComplete(float threshold = 0.9f)
        {
            string startAnim = getStartAnimation();
            return IsInAnimatorState(startAnim) && GetNormalizedTime() >= threshold;
        }
        
        public bool IsStopComplete(float threshold = 0.9f)
        {
            string stopAnim = getStopAnimation();
            return IsInAnimatorState(stopAnim) && GetNormalizedTime() >= threshold;
        }
        
        private void SafeCrossFade(string stateName, float duration, int layer = 0)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (string.IsNullOrEmpty(stateName)) return;
            
            if (layer < 0) layer = 0;
            if (layer >= animator.layerCount) layer = 0;

            if (TryCrossFadeWithSubStatePaths(stateName, duration, layer))
            {
                return;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(layer, stateHash))
            {
                animator.CrossFadeInFixedTime(stateHash, duration, layer);
                return;
            }

            string layerName = animator.GetLayerName(layer);
            string fullPath = $"{layerName}.{stateName}";
            int fullPathHash = Animator.StringToHash(fullPath);
            if (animator.HasState(layer, fullPathHash))
            {
                animator.CrossFadeInFixedTime(fullPathHash, duration, layer);
                return;
            }

            int logKey = (animator.GetInstanceID() * 397) ^ stateName.GetHashCode();
            if (!_loggedMissingStates.Contains(logKey))
            {
                _loggedMissingStates.Add(logKey);
                Debug.LogWarning($"[WeightedLocomotion] State '{stateName}' not found. Run Tools → Setup Player Animator Controller.", animator);
            }
        }

        private bool TryCrossFadeWithSubStatePaths(string stateName, float duration, int layer)
        {
            string layerName = animator.GetLayerName(layer);
            bool isEquipped = animator.GetBool(IsEquippedHash);

            string[] preferredPaths = isEquipped
                ? new[]
                {
                    $"{layerName}.Grounded.Equip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Equip Jump.{stateName}",
                    $"{layerName}.Grounded.Unequip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Unequip Jump.{stateName}",
                }
                : new[]
                {
                    $"{layerName}.Grounded.Unequip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Unequip Jump.{stateName}",
                    $"{layerName}.Grounded.Equip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Equip Jump.{stateName}",
                };

            foreach (string path in preferredPaths)
            {
                int pathHash = Animator.StringToHash(path);
                if (animator.HasState(layer, pathHash))
                {
                    animator.CrossFadeInFixedTime(pathHash, duration, layer);
                    return true;
                }
            }

            return false;
        }
        
        private bool IsInAnimatorState(string stateName, int layer = 0)
        {
            if (animator == null) return false;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            return stateInfo.IsName(stateName);
        }
        
        private float GetNormalizedTime(int layer = 0)
        {
            if (animator == null) return 0f;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            return stateInfo.normalizedTime;
        }
    }
}
