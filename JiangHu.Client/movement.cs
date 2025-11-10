using Comfort.Logs;
using EFT;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace JiangHu
{
    /// <summary>
    /// Client-side movement system for zero-inertia experience
    /// Works with server-side inertia configuration for complete zero-inertia
    /// Handles runtime smoothing that can't be configured server-side
    /// </summary>
    public class NewMovement : MonoBehaviour
    {
        private static bool movementPatched = false;
        private Player player;

        void Awake()
        {
            if (!movementPatched)
            {
                PatchMovementSystems();
                movementPatched = true;
            }
        }

        void Start()
        {
            player = FindObjectOfType<Player>();
        }

        /// <summary>
        /// Applies Harmony patches for runtime zero-inertia
        /// Server handles configuration, client handles runtime smoothing
        /// </summary>
        private void PatchMovementSystems()
        {
            try
            {
                var harmony = new Harmony("jianghu.movement");

                // Patch jump physics for perfect air control
                var originalJumpMethod = typeof(JumpStateClass).GetMethod("method_1", BindingFlags.NonPublic | BindingFlags.Instance);
                var prefixJumpMethod = typeof(NewMovement).GetMethod("JumpPhysicsPrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalJumpMethod, new HarmonyMethod(prefixJumpMethod));

                // Patch jump landing to remove penalties
                var originalLandingMethod = typeof(JumpLandingStateClass).GetMethod("ManualAnimatorMoveUpdate", BindingFlags.Public | BindingFlags.Instance);
                var prefixLandingMethod = typeof(NewMovement).GetMethod("JumpLandingPrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalLandingMethod, new HarmonyMethod(prefixLandingMethod));

                // Patch state transitions for instant transitions
                var originalTransitionBlend = typeof(TransitionStateClass).GetMethod("BlendMotion", BindingFlags.Public | BindingFlags.Instance);
                var prefixTransitionBlend = typeof(NewMovement).GetMethod("TransitionBlendPrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalTransitionBlend, new HarmonyMethod(prefixTransitionBlend));

                // Patch sprint for instant direction changes
                var originalSprintUpdate = typeof(SprintStateClass).GetMethod("ManualAnimatorMoveUpdate", BindingFlags.Public | BindingFlags.Instance);
                var prefixSprintUpdate = typeof(NewMovement).GetMethod("SprintUpdatePrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalSprintUpdate, new HarmonyMethod(prefixSprintUpdate));

                Debug.Log("JiangHu: Zero-inertia client patches applied");
            }
            catch (Exception e)
            {
                Debug.Log($"JiangHu: Movement patching failed - {e}");
            }
        }

        /// <summary>
        /// Perfect jump physics with full air strafing and momentum preservation
        /// Overrides client-side jump calculations for instant air control
        /// </summary>
        public static bool JumpPhysicsPrefix(JumpStateClass __instance, float deltaTime)
        {
            try
            {
                float num = __instance.Float_2 - __instance.Float_3;

                // Enhanced jump height with reduced gravity for better control
                __instance.Vector3_2 = ((__instance.Float_2 < __instance.Float_3) ? Vector3.zero :
                    (__instance.Vector3_1 * __instance.Float_5 * 1.5f + Physics.gravity * num * 0.3f));

                // Full instant air strafing - use current input direction
                Vector3 currentInputDirection = __instance.Vector2_0.IsZero() ? Vector3.zero :
                    __instance.MovementContext.TransformVector(new Vector3(__instance.Vector2_0.x, 0f, __instance.Vector2_0.y).normalized);

                Vector3 movementDirection = currentInputDirection;

                // Only maintain original direction when no input
                if (__instance.Vector2_0.IsZero())
                {
                    movementDirection = __instance.Vector3_0;
                }

                float airSpeed = __instance.Float_1;

                if (__instance.EjumpState_0 == JumpStateClass.EJumpState.Jump)
                {
                    // Instant air control - no lerping
                    __instance.Float_0 = airSpeed;

                    // Update movement direction for instant strafing
                    if (!__instance.Vector2_0.IsZero())
                    {
                        __instance.Vector3_0 = currentInputDirection;
                    }
                }

                Vector3 vector2 = (movementDirection * __instance.Float_0 + __instance.Vector3_2) * deltaTime;

                if (__instance.EjumpState_0 == JumpStateClass.EJumpState.Bumbped)
                {
                    vector2.y = Mathf.Min(vector2.y, 0f);
                }

                float y = __instance.MovementContext.TransformPosition.y;
                __instance.Float_9 = __instance.MovementContext.TransformPosition.y + __instance.Vector3_2.y * deltaTime;

                __instance.LimitMotion(ref vector2, deltaTime);
                __instance.MovementContext.ApplyMotion(vector2, deltaTime);

                if (__instance.MovementContext.TransformPosition.y - y < 0.001f || __instance.Vector3_2.y < 0f)
                {
                    __instance.Float_10 += deltaTime;
                }

                return false; // Skip original method
            }
            catch (Exception e)
            {
                Debug.Log($"JiangHu: Jump physics error - {e}");
                return true; // Run original method as fallback
            }
        }

        /// <summary>
        /// Removes jump landing penalties to preserve momentum
        /// </summary>
        public static bool JumpLandingPrefix(JumpLandingStateClass __instance, float deltaTime)
        {
            try
            {
                if (__instance.Float_1 > __instance.Float_2)
                {
                    __instance.Bool_0 = true;
                }

                if (__instance.Vector2_0.IsZero())
                {
                    __instance.Float_1 += deltaTime;
                    // No speed reduction after landing - preserve momentum
                    if (__instance.MovementContext.SmoothedCharacterMovementSpeed > 0.35f)
                    {
                        __instance.MovementContext.SmoothedCharacterMovementSpeed = __instance.MovementContext.ClampedSpeed;
                    }
                }

                __instance.ProcessRotation(deltaTime);

                if (!__instance.Bool_0)
                {
                    __instance.ProcessAnimatorMovement(deltaTime);
                }

                return false; // Skip original method
            }
            catch (Exception e)
            {
                Debug.Log($"JiangHu: Jump landing error - {e}");
                return true; // Run original method as fallback
            }
        }

        /// <summary>
        /// Instant state transitions - no motion preservation delays
        /// </summary>
        public static bool TransitionBlendPrefix(TransitionStateClass __instance, ref Vector3 motion, float deltaTime)
        {
            // Zero transition inertia - pass motion through unchanged
            // This preserves 100% momentum during state changes
            return false; // Skip original method entirely
        }

        /// <summary>
        /// Instant sprint response - no direction lerping
        /// </summary>
        public static bool SprintUpdatePrefix(SprintStateClass __instance, float deltaTime)
        {
            try
            {
                if (__instance.Bool_0)
                {
                    return false;
                }

                if ((Math.Abs(__instance.Direction.y) < 1E-45f || __instance.Bool_5 || !__instance.MovementContext.CanWalk) && Time.frameCount > __instance.Int_0)
                {
                    __instance.MovementContext.PlayerAnimatorEnableSprint(false, false);
                    if (Mathf.Abs(__instance.Direction.x) < 1E-45f || __instance.Bool_5)
                    {
                        __instance.MovementContext.PlayerAnimatorEnableInert(false);
                    }
                }
                else if (__instance.MovementContext.IsSprintEnabled)
                {
                    __instance.MovementContext.ProcessSpeedLimits(deltaTime);

                    // Instant sprint direction - no lerping
                    __instance.MovementContext.MovementDirection = __instance.Direction;

                    __instance.MovementContext.SetUpDiscreteDirection(GClass2076.ConvertToMovementDirection(__instance.Direction));
                    __instance.Direction = Vector2.zero;
                    __instance.Int_0 = Time.frameCount;
                    __instance.MovementContext.SprintAcceleration(deltaTime);
                    __instance.UpdateRotationAndPosition(deltaTime);
                }
                else
                {
                    __instance.MovementContext.PlayerAnimatorEnableSprint(false, false);
                }

                if (!__instance.MovementContext.CanSprint)
                {
                    __instance.MovementContext.PlayerAnimatorEnableSprint(false, false);
                }

                if (!__instance.MovementContext.PlayerAnimator.Animator.IsInTransition(0))
                {
                    __instance.MovementContext.ObstacleCollisionFacade.RecalculateCollision(__instance.velocityThreshold);
                }

                return false; // Skip original method
            }
            catch (Exception e)
            {
                Debug.Log($"JiangHu: Sprint update error - {e}");
                return true;
            }
        }

        void Update()
        {
            if (player == null || player.MovementContext == null)
                return;

            // Continuous field modification for instant response
            ModifyMovementContext();
            ModifyRunStateFields();
        }

        /// <summary>
        /// Real-time field modification for instant response
        /// Bypasses client-side smoothing that can't be configured server-side
        /// </summary>
        private void ModifyMovementContext()
        {
            try
            {
                var movementContext = player.MovementContext;
                // Bypass speed smoothing for instant response
                movementContext.SmoothedCharacterMovementSpeed = movementContext.ClampedSpeed;
                // Bypass pose smoothing
                movementContext.SmoothedPoseLevel = movementContext.PoseLevel;
                // Bypass tilt smoothing
                movementContext.SmoothedTilt = movementContext.Tilt;
            }
            catch (Exception e)
            {
                // Silent fail - runs every frame
            }
        }

        /// <summary>
        /// Modifies RunStateClass fields for instant acceleration
        /// </summary>
        private void ModifyRunStateFields()
        {
            try
            {
                var movementContext = player.MovementContext;
                if (movementContext.CurrentState is RunStateClass runState)
                {
                    // Use reflection to access private acceleration fields
                    var float9Field = typeof(RunStateClass).GetField("Float_9", BindingFlags.NonPublic | BindingFlags.Instance);
                    var float10Field = typeof(RunStateClass).GetField("Float_10", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (float9Field != null) float9Field.SetValue(runState, 1000f); // Max speed acceleration
                    if (float10Field != null) float10Field.SetValue(runState, 1000f); // Max direction acceleration

                    // Modify smoothed direction values for instant response
                    var vector3Field = typeof(RunStateClass).GetField("Vector2_3", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (vector3Field != null && runState.Direction != null && !runState.Direction.IsZero())
                    {
                        vector3Field.SetValue(runState, runState.Direction.normalized);
                    }
                }
            }
            catch (Exception e)
            {
                // Silent fail - runs every frame
            }
        }
    }
}