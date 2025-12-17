using BepInEx.Configuration;
using Comfort.Common; // For Singleton<T>
using EFT;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using UnityEngine;



namespace JiangHu
{
    public class NewMovement : MonoBehaviour
    {
        private Player player;
        private static bool EnableAll, EnableFastMovement, EnableFastLeaning, EnableFastPoseTransition,
            EnableDoubleJump, EnableFastWeapon, EnableWiderFreelook, EnablePositionSwap;
        private Harmony harmony;
        private ConfigEntry<KeyboardShortcut> _swapHotkeyConfig;
        private float _lastSwapTime = 0f;
        private float SWAP_COOLDOWN = 0.5f;
        private float SWAP_DISTANCE = 30f;
        private KeyboardShortcut _swapHotkey;

        void Start()
        {
            player = FindObjectOfType<Player>();
            LoadConfig();

            if (!EnableAll) return;

            harmony = new Harmony("jianghu.movement");

            PatchCoreInitialization();
            if (EnableFastMovement) PatchFastMovement();
            if (EnableFastLeaning) PatchFastLeaning();
            if (EnableFastPoseTransition) PatchFastPoseTransition();
            if (EnableDoubleJump) PatchDoubleJump();
            if (EnableFastWeapon) PatchFastWeapon();
            if (EnableWiderFreelook) PatchWiderFreelook();
        }

        public void SetSwapHotkey(ConfigEntry<KeyboardShortcut> hotkeyConfig)
        {
            _swapHotkeyConfig = hotkeyConfig;
        }

        void Update()
        {
            if (!EnablePositionSwap) return;

            bool hotkeyPressed = false;

            if (_swapHotkeyConfig != null)
            {
                hotkeyPressed = _swapHotkeyConfig.Value.IsDown();
            }

            if (!hotkeyPressed && Input.GetKeyDown(KeyCode.F10))
            {
                hotkeyPressed = true;
            }

            if (hotkeyPressed)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld?.MainPlayer == null) return;

                Player myPlayer = gameWorld.MainPlayer;

                float timeSinceLastSwap = Time.time - _lastSwapTime;
                float cooldownLeft = SWAP_COOLDOWN - timeSinceLastSwap;

                if (cooldownLeft > 0)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        $"Swap cooldown: {cooldownLeft:F1}s left",
                        EFT.Communications.ENotificationDurationType.Default,
                        EFT.Communications.ENotificationIconType.Alert,
                        Color.cyan
                    );
                    return;
                }

                Player targetBot = FindVisibleBot(myPlayer);

                if (targetBot == null)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        $"No bot in sight (max {SWAP_DISTANCE}m)",
                        EFT.Communications.ENotificationDurationType.Default,
                        EFT.Communications.ENotificationIconType.Alert,
                        new Color(1f, 0.3f, 0f) // Dark orange
                    );
                    return;
                }

                _lastSwapTime = Time.time;
                PerformSwap(myPlayer, targetBot);
            }
        }


        void OnDestroy() => harmony?.UnpatchSelf();

        #region Config
        private void LoadConfig()
        {
            string configPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
            if (!File.Exists(configPath)) return;

            var config = JObject.Parse(File.ReadAllText(configPath));
            EnableAll = config["Enable_New_Movement"]?.Value<bool>() ?? true;
            EnableFastMovement = config["Enable_Fast_Movement"]?.Value<bool>() ?? true;
            EnableFastLeaning = config["Enable_Fast_Leaning"]?.Value<bool>() ?? true;
            EnableFastPoseTransition = config["Enable_Fast_Pose_Transition"]?.Value<bool>() ?? true;
            EnableDoubleJump = config["Enable_Double_Jump"]?.Value<bool>() ?? true;
            EnableFastWeapon = config["Enable_Fast_Weapon_Switching"]?.Value<bool>() ?? true;
            EnableWiderFreelook = config["Enable_Wider_Freelook_Angle"]?.Value<bool>() ?? true;
            EnablePositionSwap = config["Enable_Position_Swap"]?.Value<bool>() ?? true;
            SWAP_DISTANCE = config["Swap_Distance"]?.Value<float>() ?? 30f;
            SWAP_COOLDOWN = config["Swap_Cooldown"]?.Value<float>() ?? 30f;
        }
        #endregion

        #region Core Initialization
        private void PatchCoreInitialization()
        {
            harmony.Patch(typeof(MovementContext).GetMethod("Init"),
                postfix: new HarmonyMethod(typeof(NewMovement).GetMethod("MovementInitPostfix")));
        }

        public static void MovementInitPostfix(MovementContext __instance)
        {
            __instance.SmoothedCharacterMovementSpeed = __instance.ClampedSpeed;
            __instance.SmoothedPoseLevel = __instance.PoseLevel;
            __instance.SmoothedTilt = __instance.Tilt;
            __instance.PlayerAnimatorTransitionSpeed = 1000f;
        }
        #endregion

        #region Fast Movement
        private void PatchFastMovement()
        {
            harmony.Patch(AccessTools.Method(typeof(MovementContext), "get_ClampedSpeed"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "ClampedSpeedPostfix")));

            harmony.Patch(AccessTools.Method(typeof(MovementState), "ApplyMotion"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "ApplyMotionPrefix")));
        }

        static void ClampedSpeedPostfix(ref float __result)
        {
            if (!EnableFastMovement) return;
            __result *= 2f;
        }

        static void ApplyMotionPrefix(ref Vector3 motion, float deltaTime, MovementState __instance)
        {
            if (!EnableFastMovement) return;

            var movementContext = __instance.MovementContext;
            if (movementContext != null)
            {
                float multiplier = 2f; 

                float originalMagnitude = motion.magnitude;
                motion *= multiplier;
            }
        }
        #endregion

        #region Fast Leaning
        private void PatchFastLeaning()
        {
            harmony.Patch(AccessTools.Method(typeof(MovementContext), "method_15"),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "TiltAccelerationTranspiler")));

            harmony.Patch(AccessTools.Method(typeof(MovementContext), "WeightRelatedValuesUpdated"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "TiltInertiaPostfix")));
        }

        static IEnumerable<CodeInstruction> TiltAccelerationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            if (!EnableFastLeaning) return codes;

            for (int i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("TiltInertia"))
                {
                    for (int j = i + 1; j < Math.Min(i + 5, codes.Count); j++)
                    {
                        if (codes[j].opcode == OpCodes.Ldc_R4)
                        {
                            float originalValue = (float)codes[j].operand;
                            codes[j].operand = originalValue * 50f;
                            break;
                        }
                    }
                }

                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("Single_2"))
                {
                    for (int j = i + 1; j < Math.Min(i + 5, codes.Count); j++)
                    {
                        if (codes[j].opcode == OpCodes.Ldc_R4)
                        {
                            float originalValue = (float)codes[j].operand;
                            codes[j].operand = originalValue * 10f;
                            break;
                        }
                    }
                }
            }

            return codes;
        }

        static void TiltInertiaPostfix(MovementContext __instance)
        {
            if (!EnableFastLeaning) return;

            __instance.TiltInertia = 100f;
            __instance.MaxTiltStep = 50f;
            __instance.PlayerAnimatorTransitionSpeed = 1000f;
        }
        #endregion

        #region Fast Pose Transition
        private void PatchFastPoseTransition()
        {
            harmony.Patch(AccessTools.Method(typeof(MovementContext), "SmoothPoseLevel"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "PoseTransitionPrefix")));
        }

        static bool PoseTransitionPrefix(MovementContext __instance, float deltaTime)
        {
            if (!EnableFastPoseTransition) return true;

            float poseSpeed = 20f;

            float targetPose = __instance.PoseLevel;
            float poseDifference = targetPose - __instance.SmoothedPoseLevel;

            if (Mathf.Abs(poseDifference) > 0.001f)
            {
                float transition = poseDifference * deltaTime * poseSpeed;
                __instance.SmoothedPoseLevel += transition;
            }
            else
            {
                __instance.SmoothedPoseLevel = targetPose;
            }

            return false;
        }
        #endregion

        #region Double Jump
        private static bool hasUsedMidAirJump = false;
        private static float lastMidAirJumpTime = 0f;

        private void PatchDoubleJump()
        {
            PatchStateJump(typeof(IdleStateClass));
            PatchStateJump(typeof(RunStateClass));

            harmony.Patch(AccessTools.Method(typeof(MovementState), "Jump"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "MovementStateJumpPrefix")));

            harmony.Patch(AccessTools.Method(typeof(MovementContext), "UpdateGroundCollision"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "UpdateGroundCollisionPostfix")));

            harmony.Patch(AccessTools.Method(typeof(JumpStateClass), "method_1"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "JumpMethod1Prefix")));
        }

        private void PatchStateJump(Type stateType)
        {
            var jumpMethod = AccessTools.Method(stateType, "Jump");
            if (jumpMethod != null)
            {
                harmony.Patch(jumpMethod,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "StateJumpOverridePrefix")));
            }
        }

        static bool StateJumpOverridePrefix(MovementState __instance)
        {
            if (!EnableDoubleJump) return true;

            if (!__instance.MovementContext.IsGrounded)
            {
                bool allowJump = CheckDoubleJump(__instance.MovementContext);

                if (!allowJump) return false;

                hasUsedMidAirJump = true;
                lastMidAirJumpTime = Time.time;
                __instance.MovementContext.TryJump();
                return false;
            }

            return true;
        }

        static bool MovementStateJumpPrefix(MovementState __instance)
        {
            if (!EnableDoubleJump) return true;

            if (__instance is JumpStateClass)
            {
                bool allowJump = CheckDoubleJump(__instance.MovementContext);

                if (allowJump)
                {
                    var jumpState = __instance as JumpStateClass;

                    jumpState.Float_5 *= 1f;
                    jumpState.Vector3_1 *= 1f;
                    jumpState.Float_2 = 0f;
                    jumpState.EjumpState_0 = JumpStateClass.EJumpState.PushingFromTheGround;
                    jumpState.Float_3 = EFTHardSettings.Instance.JUMP_DELAY_BY_SPEED.Evaluate(jumpState.Float_1);
                    jumpState.Float_9 = jumpState.MovementContext.TransformPosition.y;
                    jumpState.Float_10 = 0f;

                    hasUsedMidAirJump = true;
                    lastMidAirJumpTime = Time.time;

                    __instance.MovementContext.method_2(1f);
                    __instance.MovementContext.PlayerAnimatorEnableJump(true);

                    return false;
                }

                return false;
            }
            else if (!__instance.MovementContext.IsGrounded)
            {
                bool allowJump = CheckDoubleJump(__instance.MovementContext);

                if (allowJump)
                {
                    hasUsedMidAirJump = true;
                    lastMidAirJumpTime = Time.time;
                    __instance.MovementContext.TryJump();
                    return false;
                }

                return false;
            }

            return true;
        }

        static bool CheckDoubleJump(MovementContext context)
        {
            if (!EnableDoubleJump) return true;

            if (context.IsGrounded)
            {
                hasUsedMidAirJump = false;
                return true;
            }

            return !hasUsedMidAirJump;
        }

        static bool JumpMethod1Prefix(JumpStateClass __instance, ref float __state)
        {
            if (!EnableDoubleJump) return true;

            __state = __instance.Float_5;
            return true;
        }

        static void UpdateGroundCollisionPostfix(MovementContext __instance, float deltaTime)
        {
            if (!EnableDoubleJump) return;

            if (__instance.IsGrounded && hasUsedMidAirJump && Time.time > lastMidAirJumpTime + 0.3f)
            {
                hasUsedMidAirJump = false;
            }
        }
        #endregion

        #region Bot Position Swapper
        private Player FindVisibleBot(Player localPlayer)
        {
            try
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld == null) return null;

                Vector3 eyePos = localPlayer.PlayerBones.Head.position;
                Vector3 lookDir = localPlayer.LookDirection;

                float maxDistance = SWAP_DISTANCE;
                LayerMask shootMask = LayerMaskClass.HighPolyWithTerrainMask | (1 << LayerMaskClass.PlayerLayer);

                Ray ray = new Ray(eyePos, lookDir);

                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, shootMask))
                {
                    Player hitPlayer = hit.transform.GetComponentInParent<Player>();
                    if (hitPlayer != null && hitPlayer.ProfileId != localPlayer.ProfileId)
                    {
                        float distance = Vector3.Distance(eyePos, hitPlayer.Position);
                        if (distance <= SWAP_DISTANCE)
                        {
                            return hitPlayer;
                        }
                    }
                }

                return null;
            }
            catch { return null; }
        }

        private void PerformSwap(Player player, Player bot)
        {
            try
            {
                Vector3 playerPos = player.Position;
                Vector3 botPos = bot.Position;

                BotOwner botOwner = bot.AIData?.BotOwner;

                if (botOwner?.Mover != null)
                {
                    botOwner.Mover.PrevSuccessLinkedFrom_1 = playerPos;
                    botOwner.Mover.PrevPosLinkedTime_1 = Time.time;
                    botOwner.Mover.LastGoodCastPoint = playerPos;
                    botOwner.Mover.LastGoodCastPointTime = Time.time;
                    botOwner.Mover.PositionOnWayInner = playerPos;
                    botOwner.Mover.LinkedToNavmeshInitially = true;
                    botOwner.Mover.Stop();
                }

                player.GetPlayer.Teleport(botPos, true);
                bot.GetPlayer.Teleport(playerPos, true);

                if (botOwner?.Mover != null)
                {
                    botOwner.Mover.SetPointOnWay(playerPos);
                    botOwner.Mover.PositionOnWayInner = playerPos;
                }

                NotificationManagerClass.DisplayMessageNotification(
                    $"Swapped with 与{bot.Profile.Nickname}挪移换位",
                    EFT.Communications.ENotificationDurationType.Default,
                    EFT.Communications.ENotificationIconType.Quest,
                    Color.green
                );
            }
            catch
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "Swap failed",
                    EFT.Communications.ENotificationDurationType.Default,
                    EFT.Communications.ENotificationIconType.Alert,
                    Color.red
                );
            }
        }
        #endregion

        #region Fast Weapon
        private void PatchFastWeapon()
        {
            harmony.Patch(AccessTools.Method(typeof(FirearmsAnimator), "SetSpeedParameters"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "WeaponPrefix")));
        }

        static bool WeaponPrefix(ref float reload, ref float draw)
        {
            if (!EnableFastWeapon) return true;
            draw = 5f;
            return true;
        }
        #endregion

        #region Wider Freelook
        private void PatchWiderFreelook()
        {
            harmony.Patch(AccessTools.Method(typeof(EFTHardSettings), "get_Instance"),
                postfix: new HarmonyMethod(typeof(NewMovement).GetMethod("FreelookSettingsPostfix")));
        }

        public static void FreelookSettingsPostfix(ref EFTHardSettings __result)
        {
            if (!EnableWiderFreelook) return;
            __result.MOUSE_LOOK_HORIZONTAL_LIMIT = new Vector2(-72f, 72f);
            __result.MOUSE_LOOK_VERTICAL_LIMIT = new Vector2(-36f, 36f);
            __result.MOUSE_LOOK_LIMIT_IN_AIMING_COEF = 1f;
        }
        #endregion
    }
}