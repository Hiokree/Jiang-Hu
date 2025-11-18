using BepInEx.Configuration;
using EFT;
using EFT.Animations;
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
        private static bool EnableAll, EnableFastMovement, EnableFastLeaning, EnableFastPoseTransition, EnableJumpHigher, EnableFastWeapon, EnableWiderFreelook;
        private Harmony harmony;

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
            if (EnableJumpHigher) PatchJumpHigher();
            if (EnableFastWeapon) PatchFastWeapon();
            if (EnableWiderFreelook) PatchWiderFreelook();
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
            EnableJumpHigher = config["Enable_Jump_Higher"]?.Value<bool>() ?? true;
            EnableFastWeapon = config["Enable_Fast_Weapon_Switching"]?.Value<bool>() ?? true;
            EnableWiderFreelook = config["Enable_Wider_Freelook_Angle"]?.Value<bool>() ?? true;
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
            harmony.Patch(AccessTools.Method(typeof(MovementContext), "get_MaxSpeed"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "MaxSpeedPostfix")));

            harmony.Patch(AccessTools.Method(typeof(MovementContext), "get_SprintingSpeed"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "SprintingSpeedPostfix")));
        }

        static void MaxSpeedPostfix(ref float __result)
        {
            if (!EnableFastMovement) return;
            __result *= 5f;
        }

        static void SprintingSpeedPostfix(ref float __result)
        {
            if (!EnableFastMovement) return;
            __result *= 5f;
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

        #region Jump Higher
        private void PatchJumpHigher()
        {
            harmony.Patch(AccessTools.Method(typeof(JumpStateClass), "Enter"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(NewMovement), "JumpEnterPostfix")));
        }

        static void JumpEnterPostfix(JumpStateClass __instance)
        {
            if (!EnableJumpHigher) return;
            __instance.Float_5 *= 1.4f;
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
            draw = 7f; 
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