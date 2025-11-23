using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace JiangHu.Server
{
    [Injectable]
    public class MovementServerSide
    {
        private readonly DatabaseServer _databaseServer;
        private readonly SaveServer _saveServer;

        private bool _Enable_New_Movement = false;
        private bool _Enable_Fast_Movement = false;
        private bool _Enable_Fast_Leaning = false;
        private bool _Enable_Fast_Pose_Transition = false;
        private bool _Enable_Jump_Higher = false;
        private bool _Enable_Slide = false;
        private bool _Enable_Fast_Weapon_Switching = false;
        private bool _Enable_Minimal_Aimpunch = false;
        private bool _Enable_Fast_Aiming = false;
        private bool _Enable_Wider_Freelook_Angle = false;

        public MovementServerSide(DatabaseServer databaseServer, SaveServer saveServer)
        {
            _databaseServer = databaseServer;
            _saveServer = saveServer;
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = Path.Combine(modPath, "config", "config.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine("⚠️ [Jiang Hu] config.json not found!");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (config == null)
                    return;

                if (config.TryGetValue("Enable_New_Movement", out var movementValue))
                    _Enable_New_Movement = movementValue.GetBoolean();
                if (config.TryGetValue("Enable_Fast_Movement", out var fastMovementValue))
                    _Enable_Fast_Movement = fastMovementValue.GetBoolean();
                if (config.TryGetValue("Enable_Fast_Leaning", out var leaningValue))
                    _Enable_Fast_Leaning = leaningValue.GetBoolean();
                if (config.TryGetValue("Enable_Fast_Pose_Transition", out var poseValue))
                    _Enable_Fast_Pose_Transition = poseValue.GetBoolean();
                if (config.TryGetValue("Enable_Jump_Higher", out var jumpValue))
                    _Enable_Jump_Higher = jumpValue.GetBoolean();
                if (config.TryGetValue("Enable_Slide", out var slideValue))
                    _Enable_Slide = slideValue.GetBoolean();
                if (config.TryGetValue("Enable_Fast_Weapon_Switching", out var weaponSwitchValue))
                    _Enable_Fast_Weapon_Switching = weaponSwitchValue.GetBoolean();
                if (config.TryGetValue("Enable_Minimal_Aimpunch", out var aimpunchValue))
                    _Enable_Minimal_Aimpunch = aimpunchValue.GetBoolean();
                if (config.TryGetValue("Enable_Fast_Aiming", out var aimingValue))
                    _Enable_Fast_Aiming = aimingValue.GetBoolean();
                if (config.TryGetValue("Enable_Wider_Freelook_Angle", out var freelookValue))
                    _Enable_Wider_Freelook_Angle = freelookValue.GetBoolean();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error loading movement config: {ex.Message} \x1b[0m");
            }
        }

        public void ApplyAllSettings()
        {
            if (!_Enable_New_Movement)
            {
                Console.WriteLine("🔄 [Jiang Hu] New Movement disabled - skipping all modifications");
                return;
            }
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Floating Steps Over Ripples enabled    凌波微步\x1b[0m");

            ApplyMovementSettings();
            ApplyFastMovement();
            ApplyFastLeaning();
            ApplyFastPoseTransition();
            ApplyJumpHigher();
            ApplySlide();
            ApplyFastWeaponSwitching();
            ApplyFastAiming();
            ApplyMinimalAimpunch();
            ApplyWiderFreelookAngle();            
        }

        private void ApplyMovementSettings()
        {
            var globalConfig = _databaseServer.GetTables().Globals;
            var inertia = globalConfig.Configuration.Inertia;

            inertia.AverageRotationFrameSpan = 1;
            inertia.BaseJumpPenalty = 0.1f;
            inertia.BaseJumpPenaltyDuration = 0.1f;
            inertia.CrouchSpeedAccelerationRange = new XYZ { X = 0.1f, Y = 0.1f, Z = 0f };
            inertia.DiagonalTime = new XYZ { X = 0.2f, Y = 0.2f, Z = 0f };
            inertia.DurationPower = 0.5f;
            inertia.ExitMovementStateSpeedThreshold = new XYZ { X = 0.05f, Y = 0.05f, Z = 0f };
            inertia.FallThreshold = 0.05f;
            inertia.InertiaBackwardCoef = new XYZ { X = 0.8f, Y = 0.8f, Z = 0f };
            inertia.InertiaLimits = new XYZ { X = 0.1f, Y = 10f, Z = 0.1f };
            inertia.InertiaLimitsStep = 0.01f;
            inertia.InertiaTiltCurveMax = new XYZ { X = 1f, Y = 0.1f, Z = 0f };
            inertia.InertiaTiltCurveMin = new XYZ { X = 0.1f, Y = 1f, Z = 0f };
            inertia.MaxMovementAccelerationRangeRight = new XYZ { X = 0.1f, Y = 1f, Z = 0f };
            inertia.MaxTimeWithoutInput = new XYZ { X = 0.05f, Y = 0.1f, Z = 0f };
            inertia.MinDirectionBlendTime = 0.05f;
            inertia.MinMovementAccelerationRangeRight = new XYZ { X = 0.1f, Y = 1f, Z = 0f };
            inertia.MoveTimeRange = new XYZ { X = 0.05f, Y = 0.1f, Z = 0f };
            inertia.PenaltyPower = 0.5f;
            inertia.PreSprintAccelerationLimits = new XYZ { X = 0.5f, Y = 1f, Z = 0f };
            inertia.ProneDirectionAccelerationRange = new XYZ { X = 0.5f, Y = 0.5f, Z = 0f };
            inertia.ProneSpeedAccelerationRange = new XYZ { X = 0.5f, Y = 0.5f, Z = 0f };
            inertia.SideTime = new XYZ { X = 0.2f, Y = 0.2f, Z = 0f };
            inertia.SpeedInertiaAfterJump = new XYZ { X = 0.5f, Y = 0.5f, Z = 0f };
            inertia.SpeedLimitAfterFallMax = new XYZ { X = 2f, Y = 1f, Z = 0f };
            inertia.SpeedLimitAfterFallMin = new XYZ { X = 0.8f, Y = 1f, Z = 0f };
            inertia.SpeedLimitDurationMax = new XYZ { X = 0.5f, Y = 0.5f, Z = 0f };
            inertia.SpeedLimitDurationMin = new XYZ { X = 0.1f, Y = 0.3f, Z = 0f };
            inertia.SprintAccelerationLimits = new XYZ { X = 0.5f, Y = 0.8f, Z = 0f };
            inertia.SprintBrakeInertia = new XYZ { X = 0.5f, Y = 10f, Z = 0f };
            inertia.SprintSpeedInertiaCurveMax = new XYZ { X = 1f, Y = 0.5f, Z = 0f };
            inertia.SprintSpeedInertiaCurveMin = new XYZ { X = 0.5f, Y = 1f, Z = 0f };
            inertia.TiltAcceleration = new XYZ { X = 0.1f, Y = 0.1f, Z = 0f };
            inertia.TiltInertiaMaxSpeed = new XYZ { X = 0.8f, Y = 0.8f, Z = 0f };
            inertia.TiltMaxSideBackSpeed = new XYZ { X = 1f, Y = 1f, Z = 0f };
            inertia.TiltStartSideBackSpeed = new XYZ { X = 0.9f, Y = 0.9f, Z = 0f };
            inertia.WalkInertia = new XYZ { X = 0.05f, Y = 0.1f, Z = 0f };
            inertia.WeaponFlipSpeed = new XYZ { X = 1f, Y = 1f, Z = 0f };
        }

        private void ApplySlide()
        {
            if (!_Enable_Slide) return;

            var globalConfig = _databaseServer.GetTables().Globals;
            var inertia = globalConfig.Configuration.Inertia;
            inertia.SprintTransitionMotionPreservation = new XYZ { X = 1, Y = 1, Z = 0 };
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Slide enabled    滑铲\x1b[0m");
        }

        private void ApplyMinimalAimpunch()
        {
            if (!_Enable_Minimal_Aimpunch) return;

            var globalConfig = _databaseServer.GetTables().Globals;
            globalConfig.Configuration.AimPunchMagnitude = 0.001f;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] MinimalAimpunch enabled    瞄准无被击干扰\x1b[0m");
        }

        private void ApplyFastAiming()
        {
            if (!_Enable_Fast_Aiming) return;

            var globalConfig = _databaseServer.GetTables().Globals;
            var aiming = globalConfig.Configuration.Aiming;

            aiming.LightWeight = 0.1f;
            aiming.HeavyWeight = 5f;
            aiming.MinTimeLight = 0.1f;
            aiming.MaxTimeLight = 0.5f;
            aiming.MinTimeHeavy = 0.3f;
            aiming.MaxTimeHeavy = 1.0f;
            aiming.AimProceduralIntensity = 0.3f;
            aiming.CameraSnapGlobalMult = 2f;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Fast Aiming enabled    快速瞄准\x1b[0m");
        }

        private void ApplyFastMovement()
        {
            if (!_Enable_Fast_Movement) return;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Fast Movement enabled    快速移动\x1b[0m");
        }

        private void ApplyFastLeaning()
        {
            if (!_Enable_Fast_Leaning) return;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Fast Leaning enabled    快速侧身\x1b[0m");
        }

        private void ApplyFastPoseTransition()
        {
            if (!_Enable_Fast_Pose_Transition) return;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Fast Pose Transition enabled    快速姿态切换\x1b[0m");
        }

        private void ApplyJumpHigher()
        {
            if (!_Enable_Jump_Higher) return;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Jump Higher enabled    跳得更高\x1b[0m");
        }

        private void ApplyFastWeaponSwitching()
        {
            if (!_Enable_Fast_Weapon_Switching) return;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Fast Weapon Switching enabled    快速切枪\x1b[0m");
        }

        private void ApplyWiderFreelookAngle()
        {
            if (!_Enable_Wider_Freelook_Angle) return;
            Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Wider Freelook Angle enabled    自由视角更广\x1b[0m");
        }

    }
}
