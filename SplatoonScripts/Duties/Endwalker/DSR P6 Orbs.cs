using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Microsoft.VisualBasic.ApplicationServices;
using PInvoke;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class DSR_P6_Orbs: SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 968 };
        public override Metadata? Metadata => new(3, "Enthusiastus");

        Element? EElement;
        Element? NWElement;
        Element? NEElement;
        Element? SWElement;
        Element? SEElement;
        Element? WElement;
        Element? NoElement;

        bool active = false;
        string safeSide = "w";
        string safeHeight = "n";


        const uint NidhoggDataId = 12612;
        const uint HraesvelgrDataId = 12613;
        const uint ScarletPriceDataId = 13238;

        BattleNpc? Nidhogg => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == NidhoggDataId) as BattleNpc;
        BattleNpc? Hraesvelgr => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == HraesvelgrDataId) as BattleNpc;
        string TestOverride = "";

        PlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is PlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            var espot = "{\"Name\":\"espot\",\"Enabled\":false,\"refX\":115.5,\"refY\":100.0,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            var nwspot = "{\"Name\":\"espot\",\"Enabled\":false,\"refX\":80.0,\"refY\":85.0,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            var nespot = "{\"Name\":\"espot\",\"Enabled\":false,\"refX\":120.0,\"refY\":85.0,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            var swspot = "{\"Name\":\"espot\",\"Enabled\":false,\"refX\":80.0,\"refY\":115.0,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            var sespot = "{\"Name\":\"espot\",\"Enabled\":false,\"refX\":120.0,\"refY\":115.0,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            var wspot = "{\"Name\":\"wspot\",\"Enabled\":false,\"refX\":85.0,\"refY\":100.0,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            EElement = Controller.RegisterElementFromCode($"espot", espot);
            NWElement = Controller.RegisterElementFromCode($"nwspot", nwspot);
            NEElement = Controller.RegisterElementFromCode($"nespot", nespot);
            SWElement = Controller.RegisterElementFromCode($"swspot", swspot);
            SEElement = Controller.RegisterElementFromCode($"sespot", sespot);
            WElement = Controller.RegisterElementFromCode($"wspot", wspot);
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            /*
            // Ice Tether
            if (vfxPath == "vfx/channeling/eff/chn_ice_mouth01x.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is PlayerCharacter pvc)
                {
                    var pos = findPosition(pvc.ClassJob.GameData.NameEnglish);
                    DuoLog.Information($"{pos} has ice");
                    tethers.Add(pos, 0);
                    maybeDrawFirstChainTarget();
                }
            // Fire Tether
            } else if(vfxPath == "vfx/channeling/eff/chn_fire_mouth01x.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is PlayerCharacter pvc)
                {
                    var pos = findPosition(pvc.ClassJob.GameData.NameEnglish);
                    DuoLog.Information($"{pos} has fire");
                    tethers.Add(pos, 1);
                    maybeDrawFirstChainTarget();
                }
            }
            */
        }

        /*
        public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
        {
            source.TryGetObject(out var src);
            DuoLog.Information($"Tether from {src.DataId}");
            if (src.DataId == NidhoggDataId)
            {
                DuoLog.Information($"{target} has nidhogg");
            }
            if (src.DataId == HraesvelgrDataId)
            {
                DuoLog.Information($"{target} has hraes");
            }
        }
        */

        public override void OnMessage(string Message)
        {
            if (Message.Contains("(3458>27974)"))
            {
                DuoLog.Information($"Enable search for orbs!");
                active = true;
            }
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            /*
            if (set.Action == null) return;
            if (set.Action.RowId == 27957)
            {
                //DuoLog.Information($"Position locked!");
                Off();
            }
            */
        }

        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
        }

        void Hide()
        {
        }

        void Off()
        {
            safeSide = "w";
            safeHeight = "n";
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        public override void OnUpdate()
        {
            if (!active) return;
            if (Svc.Objects.Count(x => x is BattleChara c && c.DataId == ScarletPriceDataId) > 3)
            {
                active = false;
                EElement.Enabled = false;
                WElement.Enabled = false;
                var prices = Svc.Objects.Where(x => x is BattleChara c && c.DataId == ScarletPriceDataId);
                DuoLog.Information($"Found {prices.Count()} ScarletPrices!");
                
                foreach(var p in prices)
                {
                    DuoLog.Information($"Price pos: X:{p.Position.X}Y:{p.Position.Y}Z:{p.Position.Z}!{p.GetPositionXZY()}");
                    if(p.Position.Z < 94)
                    {
                        safeHeight = "s";
                        break;
                    } else if(p.Position.Z > 106)
                    {
                        break;
                    }
                }
                DuoLog.Information($"Safespot is: {safeHeight+safeSide+"spot"}");
                var elem = Controller.GetElementByName(safeHeight + safeSide + "spot");
                elem.Enabled = true;
                Task.Delay(8500).ContinueWith(_ =>
                {
                    Off();
                });
            } else if(Svc.Objects.Count(x => x is BattleChara c && c.DataId == ScarletPriceDataId) < 4)
            {
                if (Hraesvelgr.Position.X < 95)
                {
                    safeSide = "e";
                    EElement.Enabled = true;
                    return;
                }
                WElement.Enabled = true;
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                Off();
            }
        }

        Config Conf => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
        }

        public override void OnSettingsDraw()
        {
            if (ImGui.CollapsingHeader("Debug"))
            {
                ImGui.SetNextItemWidth(200f);
                ImGui.InputText("TestOverride", ref TestOverride, 50);
                ImGuiEx.Text($"{PC}");
            }
        }

        public unsafe static Vector4 Vector4FromRGBA(uint col)
        {
            byte* bytes = (byte*)&col;
            return new Vector4((float)bytes[3] / 255f, (float)bytes[2] / 255f, (float)bytes[1] / 255f, (float)bytes[0] / 255f);
        }
    }
}
