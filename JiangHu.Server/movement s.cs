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
        private bool _Enable_Slide = false;

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
                if (config.TryGetValue("Enable_Slide", out var slideValue))
                    _Enable_Slide = slideValue.GetBoolean();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error loading movement config: {ex.Message} \x1b[0m");
            }
        }

        public void ApplyNewMovementSettings()
        {
            if (!_Enable_New_Movement)
            {
                return;
            }

            var globalConfig = _databaseServer.GetTables().Globals;
            var inertia = globalConfig.Configuration.Inertia;

            inertia.BaseJumpPenalty = 0.001f;                                              // No jump penalty
            inertia.BaseJumpPenaltyDuration = 0.001f;                                      // No penalty duration
            inertia.CrouchSpeedAccelerationRange = new XYZ { X = 0.001f, Y = 0.001f, Z = 0 };    // Instant crouch speed
            inertia.DiagonalTime = new XYZ { X = 0.001f, Y = 0.001f, Z = 0 };                   // Instant diagonal movement
            inertia.InertiaBackwardCoef = new XYZ { X = 1, Y = 1, Z = 0 };            // Full backward movement
            inertia.MaxTimeWithoutInput = new XYZ { X = 0.001f, Y = 0.001f, Z = 0 };            // No input delay
            inertia.MinDirectionBlendTime = 0.001f;                                        // No direction blending delay
            inertia.TiltAcceleration = new XYZ { X = 0, Y = 0, Z = 0 };      // Instant tilt changes
            inertia.TiltMaxSideBackSpeed = new XYZ { X = 0.001f, Y = 0.001f, Z = 0 };  // Max tilt speed
            inertia.WalkInertia = new XYZ { X = 0.001f, Y = 0.001f, Z = 0 };           // Instant acceleration

            if (_Enable_Slide)
            {
                inertia.SprintTransitionMotionPreservation = new XYZ { X = 1, Y = 1, Z = 0 }; // 100% momentum preservation
            }

            Console.WriteLine($"\x1b[93m🪽 [Jiang Hu] Floating Steps Over Ripples enabled 凌波微步心法\x1b[0m");
        }
    }
}
