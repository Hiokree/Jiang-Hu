using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace JiangHu.Server
{
    /// <summary>
    /// Server-side movement inertia modification for zero-inertia experience
    /// Modifies global inertia parameters in server configuration
    /// Client reads these values through its own BackendConfigSettingsClass system
    /// </summary>
    [Injectable]
    public class MovementServerSide
    {
        private readonly DatabaseServer _databaseServer;
        private readonly SaveServer _saveServer;

        public MovementServerSide(DatabaseServer databaseServer, SaveServer saveServer)
        {
            _databaseServer = databaseServer;
            _saveServer = saveServer;
        }

        /// <summary>
        /// Applies zero-inertia configuration to server global settings
        /// Path: DatabaseTables.Globals.Configuration.Inertia
        /// These settings provide the base configuration for movement physics
        /// </summary>
        public void ApplyZeroInertiaSettings()
        {
            var globalConfig = _databaseServer.GetTables().Globals;

            // Get inertia settings from server configuration
            var inertia = globalConfig.Configuration.Inertia;

            // INSTANT ACCELERATION - Zero ground inertia
            inertia.WalkInertia = new XYZ { X = 1000, Y = 1000, Z = 1000 };           // Instant acceleration
            inertia.SprintBrakeInertia = new XYZ { X = 1000, Y = 1000, Z = 1000 };    // Instant sprint stops

            // ZERO JUMP PENALTIES - Perfect momentum preservation
            inertia.SpeedInertiaAfterJump = new XYZ { X = 0, Y = 0, Z = 0 };          // No speed loss after jumps
            inertia.BaseJumpPenalty = 0;                                              // No jump penalty
            inertia.BaseJumpPenaltyDuration = 0;                                      // No penalty duration

            // INSTANT DIRECTION CHANGES - Zero directional inertia
            inertia.MinDirectionBlendTime = 0;                                        // No direction blending delay
            inertia.InertiaBackwardCoef = new XYZ { X = 1, Y = 1, Z = 1 };            // Full backward movement
            inertia.MoveTimeRange = new XYZ { X = 0, Y = 0, Z = 0 };                  // Instant movement response
            inertia.SideTime = new XYZ { X = 0, Y = 0, Z = 0 };                       // Instant side movement
            inertia.DiagonalTime = new XYZ { X = 0, Y = 0, Z = 0 };                   // Instant diagonal movement

            // INSTANT TRANSITIONS - Perfect momentum preservation
            inertia.SprintTransitionMotionPreservation = new XYZ { X = 1, Y = 1, Z = 1 }; // 100% momentum preservation

            // INSTANT ROTATION - Zero rotation inertia
            inertia.TiltAcceleration = new XYZ { X = 1000, Y = 1000, Z = 1000 };      // Instant tilt changes
            inertia.TiltMaxSideBackSpeed = new XYZ { X = 1000, Y = 1000, Z = 1000 };  // Max tilt speed

            // INSTANT SPRINT - Zero sprint inertia
            inertia.SprintSpeedInertiaCurveMin = new XYZ { X = 1, Y = 1, Z = 1 };     // Full sprint control
            inertia.SprintSpeedInertiaCurveMax = new XYZ { X = 1, Y = 1, Z = 1 };     // Full sprint control

            // INSTANT MOVEMENT - Zero movement inertia
            inertia.ProneDirectionAccelerationRange = new XYZ { X = 1000, Y = 1000, Z = 1000 }; // Instant prone direction
            inertia.ProneSpeedAccelerationRange = new XYZ { X = 1000, Y = 1000, Z = 1000 };     // Instant prone speed
            inertia.CrouchSpeedAccelerationRange = new XYZ { X = 1000, Y = 1000, Z = 1000 };    // Instant crouch speed

            // INSTANT INPUT RESPONSE
            inertia.MaxTimeWithoutInput = new XYZ { X = 0, Y = 0, Z = 0 };            // No input delay

            Console.WriteLine($"\x1b[93m🪽 [Jiang Hu] New movement settings applied \x1b[0m");
        }
    }
}
