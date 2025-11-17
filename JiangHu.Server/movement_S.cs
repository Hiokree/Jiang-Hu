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
            inertia.SprintTransitionMotionPreservation = new XYZ { X = 0.9f, Y = 0.9f, Z = 0f }; 
            inertia.TiltAcceleration = new XYZ { X = 0.1f, Y = 0.1f, Z = 0f };              
            inertia.TiltInertiaMaxSpeed = new XYZ { X = 0.8f, Y = 0.8f, Z = 0f };         
            inertia.TiltMaxSideBackSpeed = new XYZ { X = 1f, Y = 1f, Z = 0f };              
            inertia.TiltStartSideBackSpeed = new XYZ { X = 0.9f, Y = 0.9f, Z = 0f };      
            inertia.WalkInertia = new XYZ { X = 0.05f, Y = 0.1f, Z = 0f };                 
            inertia.WeaponFlipSpeed = new XYZ { X = 1f, Y = 1f, Z = 0f };                  


            if (_Enable_Slide)
            {
                inertia.SprintTransitionMotionPreservation = new XYZ { X = 1, Y = 1, Z = 0 };
            }

            Console.WriteLine($"\x1b[93m🪽 [Jiang Hu] Floating Steps Over Ripples enabled 凌波微步心法\x1b[0m");
        }
    }
}
