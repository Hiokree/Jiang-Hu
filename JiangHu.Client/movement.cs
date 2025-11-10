using BepInEx.Configuration;
using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace JiangHu
{
    public class NewMovement : MonoBehaviour
    {
        private Player player;
        private bool _Enable_New_Movement = false;
        private ConfigEntry<bool> showMovementConfig;

        private bool showGUI = false;
        private Rect windowRect = new Rect(350, 100, 300, 200);

        void Awake()
        {
            LoadConfig(); // Load config first

            if (!_Enable_New_Movement)
            {
                return; // Don't patch if disabled
            }

            PatchMovementSystems();
        }

        private void LoadConfig()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "FPSconfig.json");

                Console.WriteLine($"\x1b[36m🔧 [Jiang Hu] Enable_New_Movement = {_Enable_New_Movement}\x1b[0m");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine("⚠️ [Jiang Hu] FPSconfig.json not found!");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JObject.Parse(json); 

                if (config == null) return;

                if (config.TryGetValue("Enable_New_Movement", out var movementValue))
                    _Enable_New_Movement = movementValue.Value<bool>();

                Console.WriteLine($"🔧 [Jiang Hu] Enable_New_Movement = {_Enable_New_Movement}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Jiang Hu] Error loading FPS config: {ex.Message}");
            }
        }

        private void SaveConfigToJson()
        {
            try
            {
                var configDict = new Dictionary<string, object>
                {
                    { "Enable_New_Movement", _Enable_New_Movement }
                };

                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "FPSconfig.json");

                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                string json = JsonConvert.SerializeObject(configDict, Formatting.Indented);
                File.WriteAllText(configPath, json);

                Debug.Log("✅ [Mount Hua] FPS settings saved to JSON");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [Mount Hua] Error saving FPS settings: {ex.Message}");
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

                Console.WriteLine("\x1b[36m🔧 [Jiang Hu] PatchMovementSystems() CALLED\x1b[0m");

                // Patch jump physics for perfect air control
                var originalJumpMethod = typeof(JumpStateClass).GetMethod("method_1", BindingFlags.NonPublic | BindingFlags.Instance);
                var prefixJumpMethod = typeof(NewMovement).GetMethod("JumpPhysicsPrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalJumpMethod, new HarmonyMethod(prefixJumpMethod));

                // Patch state transitions for instant transitions
                var originalTransitionBlend = typeof(TransitionStateClass).GetMethod("BlendMotion", BindingFlags.Public | BindingFlags.Instance);
                var prefixTransitionBlend = typeof(NewMovement).GetMethod("TransitionBlendPrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalTransitionBlend, new HarmonyMethod(prefixTransitionBlend));

                // Patch sprint for instant direction changes
                var originalSprintUpdate = typeof(SprintStateClass).GetMethod("ManualAnimatorMoveUpdate", BindingFlags.Public | BindingFlags.Instance);
                var prefixSprintUpdate = typeof(NewMovement).GetMethod("SprintUpdatePrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalSprintUpdate, new HarmonyMethod(prefixSprintUpdate));

                // Use POSTFIX instead to modify results after original logic runs
                var originalRunMethod = typeof(RunStateClass).GetMethod("method_1", BindingFlags.NonPublic | BindingFlags.Instance);
                var postfixRunMethod = typeof(NewMovement).GetMethod("RunStateUpdatePostfix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalRunMethod, postfix: new HarmonyMethod(postfixRunMethod));

                // Patch JumpLandingStateClass for instant state transitions  
                var originalLandingMethod = typeof(JumpLandingStateClass).GetMethod("ManualAnimatorMoveUpdate", BindingFlags.Public | BindingFlags.Instance);
                var prefixLandingMethod = typeof(NewMovement).GetMethod("JumpLandingPrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalLandingMethod, new HarmonyMethod(prefixLandingMethod));


                // Patch to remove all stamina consumption cooldowns
                var originalConsumeMethod = typeof(PlayerPhysicalClass.GClass773).GetMethod("Consume", BindingFlags.Public | BindingFlags.Instance);
                var prefixConsumeMethod = typeof(NewMovement).GetMethod("StaminaConsumePrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalConsumeMethod, new HarmonyMethod(prefixConsumeMethod));

                // Patch to remove stamina thresholds
                var originalCanAddMethod = typeof(GClass774).GetMethod("CanAdd", BindingFlags.Public | BindingFlags.Instance);
                var prefixCanAddMethod = typeof(NewMovement).GetMethod("CanAddPrefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(originalCanAddMethod, new HarmonyMethod(prefixCanAddMethod));

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
        /// Instant run response - zero inertia when stopping, instant acceleration when moving
        /// </summary>
        /// <summary>
        /// Instant run response - modifies the original method instead of skipping it
        /// </summary>
        public static void RunStateUpdatePostfix(RunStateClass __instance, float deltaTime)
        {
            try
            {
                // Force instant stop when no direction input
                if (__instance.Direction.IsZero())
                {
                    __instance.Vector2_3 = Vector2.zero;
                    __instance.MovementContext.SmoothedCharacterMovementSpeed = 0f;
                    __instance.Float_2 = 0f; // Reset no-input timer
                }
                else
                {
                    // Instant acceleration when moving - bypass smoothing
                    __instance.Vector2_3 = __instance.Direction.normalized * __instance.Float_7;
                    __instance.MovementContext.SmoothedCharacterMovementSpeed = __instance.Float_7;
                    __instance.Float_2 = 0f; // Reset no-input timer
                }
            }
            catch (Exception e)
            {
                Debug.Log($"JiangHu: Run state postfix error - {e}");
            }
        }

        /// <summary>
        /// Complete jump landing patch with instant state transitions and zero penalties
        /// </summary>
        public static bool JumpLandingPrefix(JumpLandingStateClass __instance, float deltaTime)
        {
            try
            {
                // Preserve original state transition logic but remove delays
                if (__instance.Float_1 > __instance.Float_2)
                {
                    __instance.Bool_0 = true;

                    // INSTANT STATE TRANSITION: Force exit landing state immediately
                    if (__instance.MovementContext.CurrentState == __instance)
                    {
                        var runState = __instance.MovementContext.GetNewState(EPlayerState.Run, false);
                        __instance.MovementContext.ProcessStateEnter(runState);
                    }
                }

                if (__instance.Vector2_0.IsZero())
                {
                    __instance.Float_1 += deltaTime;
                    // ZERO SPEED PENALTY: Maintain full speed instead of lerping to 0.35f
                    __instance.MovementContext.SmoothedCharacterMovementSpeed = __instance.MovementContext.ClampedSpeed;
                }
                else
                {
                    // INSTANT RESPONSE: Full speed immediately when input exists
                    __instance.MovementContext.SmoothedCharacterMovementSpeed = __instance.MovementContext.ClampedSpeed;
                    __instance.Float_1 = 0f; // Reset timer for instant state transition
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
                return true; // Fallback to original
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


        /// <summary>
        /// Removes all stamina consumption cooldowns for instant movement response
        /// </summary>
        public static bool StaminaConsumePrefix(PlayerPhysicalClass.GClass773 __instance, BasePhysicalClass physical, bool fromHit = false)
        {
            try
            {
                // Remove downtime cooldowns for all consumption types
                __instance.Downtime = 0f;

                // Allow immediate consumption regardless of previous usage
                if ((__instance.PrimaryTarget & PlayerPhysicalClass.EConsumptionTarget.Base) != (PlayerPhysicalClass.EConsumptionTarget)0)
                {
                    physical.Stamina.Consume(__instance, fromHit);
                }
                if ((__instance.PrimaryTarget & PlayerPhysicalClass.EConsumptionTarget.Hands) != (PlayerPhysicalClass.EConsumptionTarget)0)
                {
                    physical.HandsStamina.Consume(__instance, false);
                }
                if ((__instance.PrimaryTarget & PlayerPhysicalClass.EConsumptionTarget.Oxygen) != (PlayerPhysicalClass.EConsumptionTarget)0)
                {
                    physical.Oxygen.Consume(__instance, false);
                }

                return false; // Skip original method
            }
            catch (Exception e)
            {
                Debug.Log($"JiangHu: Stamina consume error - {e}");
                return true; // Fallback to original
            }
        }

        /// <summary>
        /// Patches GClass774 to remove stamina thresholds that block actions
        /// </summary>
        public static bool CanAddPrefix(GClass774 __instance, PlayerPhysicalClass.GClass773 consumption, ref bool __result)
        {
            try
            {
                // Remove all start thresholds - allow actions regardless of current stamina
                __result = true;
                return false; // Skip original method
            }
            catch (Exception e)
            {
                Debug.Log($"JiangHu: CanAdd error - {e}");
                return true; // Fallback to original
            }
        }



        public void SetConfig(ConfigEntry<bool> showMovementConfig)
        {
            this.showMovementConfig = showMovementConfig;
        }

        // ADD THIS UPDATE METHOD
        void Update()
        {
            // Remove this: if (Input.GetKeyDown(KeyCode.F6)) { showGUI = !showGUI; }

            // Use the config value to control GUI visibility
            showGUI = showMovementConfig.Value;

            if (player == null || player.MovementContext == null)
                return;

            ModifyMovementContext();
            ModifyRunStateFields();
        }

        void OnGUI()
        {
            if (!showGUI) return;

            windowRect = GUI.Window(12352, windowRect, DrawFPSConfigWindow, "Mount Hua Sword Summit 华山论剑");
        }

        // ADD THIS WINDOW DRAWING METHOD
        void DrawFPSConfigWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
            {
                showGUI = false;
                showMovementConfig.Value = false; // Update config when closing
                return;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 25, 20));
            GUILayout.Space(20);

            // Enable New Movement Toggle
            GUILayout.BeginVertical("box");

            bool newEnableMovement = GUILayout.Toggle(_Enable_New_Movement, " Enable New Movement  启用轻功", GUILayout.Height(30));
            if (newEnableMovement != _Enable_New_Movement)
            {
                _Enable_New_Movement = newEnableMovement;
                SaveConfigToJson();

                if (_Enable_New_Movement)
                {
                    PatchMovementSystems();
                    Debug.Log("✅ [Mount Hua] Zero-inertia movement enabled");
                }
                else
                {
                    Debug.Log("✅ [Mount Hua] Zero-inertia movement disabled");
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("Restart server to apply  重启服务器生效");
        }
    }
}