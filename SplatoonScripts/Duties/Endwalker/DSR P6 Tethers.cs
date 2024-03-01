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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class DSR_P6_Tethers : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 968 };
        public override Metadata? Metadata => new(1, "Enthusiastus");


        Dictionary<string,int> tethers = new();

        Element? SeElement;
        Element? SwElement;
        Element? NoElement;

        bool active = false;

        const uint NidhoggDataId = 12612;
        const uint HraesvelgrDataId = 12613;
        bool positionDynamic = true;

        

        BattleNpc? Nidhogg => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == NidhoggDataId) as BattleNpc;
        BattleNpc? Hraesvelgr => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == NidhoggDataId) as BattleNpc;
        string TestOverride = "";

        PlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is PlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            var sespot = "{\"Name\":\"sespot\",\"Enabled\":false,\"refX\":105.5,\"refY\":117.5,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            var swspot = "{\"Name\":\"swspot\",\"Enabled\":false,\"refX\":94.5,\"refY\":117.5,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            var nospot = "{\"Name\":\"nospot\",\"Enabled\":false,\"refX\":100.0,\"refY\":107.5,\"radius\":0.5,\"color\":3372154880,\"thicc\":6.0,\"tether\":true}";
            SeElement = Controller.RegisterElementFromCode($"sespot", sespot);
            SwElement = Controller.RegisterElementFromCode($"swspot", swspot);
            NoElement = Controller.RegisterElementFromCode($"nospot", nospot);
        }

        private string findPosition(string job)
        {
            job = job.ToLower();
            if (job == Conf.NorthAnchorName.ToLower()) return "NOAnchor";
            if (job == Conf.NorthPartnerName.ToLower()) return "NOPartner";
            if (job == Conf.SWAnchorName.ToLower()) return "SWAnchor";
            if (job == Conf.SWPartnerName.ToLower()) return "SWPartner";
            if (job == Conf.SEAnchorName.ToLower()) return "SEAnchor";
            if (job == Conf.SEPartnerName.ToLower()) return "SEPartner";
            return "Unknown";
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
            //DuoLog.Information($"I am {PC.ClassJob.GameData.NameEnglish} on {findPosition(PC.ClassJob.GameData.NameEnglish)}");
        }

        private void enableSportByString(string pos)
        {
            pos = pos.ToLower();
            switch(pos)
            {
                case "no":
                    NoElement.Enabled = true;
                    break;
                case "se":
                    SeElement.Enabled = true;
                    break;
                case "sw":
                    SwElement.Enabled = true;
                    break;
                default:
                    DuoLog.Error($"DSR P6: Unknown spot {pos}");
                    break;

            }
            Task.Delay(9000).ContinueWith(_ =>
            {
                Off();
            });
        }

        public void maybeDrawFirstChainTarget()
        {
            if (tethers.Count == 6)
            {
                //DuoLog.Information($"All tethers seen, tell me where now");
                var me = findPosition(PC.ClassJob.GameData.NameEnglish);
                var initialPosition = me[0..2];
                var anchorString = me[2..];
                var tetherId = tethers[me];
                //DuoLog.Information($"Initial position is {initialPosition} and I am {anchorString} with tether {tetherId}");
                var isAnchor = false;
                if(anchorString == "Anchor")
                {
                    isAnchor = true;
                    enableSportByString(initialPosition);
                    //DuoLog.Information($"I am anchor... never move.");
                    return;
                }
                var partnerTetherId = tethers[initialPosition + "Anchor"];
                //DuoLog.Information($"I am partner with {tetherId} and my anchor has {partnerTetherId} should I move?");
                if(tetherId != partnerTetherId)
                {
                    enableSportByString(initialPosition);
                    //DuoLog.Information($"Tethers are different, stay");
                    return;
                }
                //DuoLog.Information($"Tethers are same, move");
                foreach (var p in new List<string>{ "NO", "SW", "SE" }) {
                    if (tethers[p+"Anchor"] == tethers[p+"Partner"])
                    {
                        if (initialPosition != p)
                        {
                            enableSportByString(p);
                            return;
                        }
                    }
                }
            }
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            // Ice Tether
            if (vfxPath == "vfx/channeling/eff/chn_ice_mouth01x.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is PlayerCharacter pvc)
                {
                    var pos = findPosition(pvc.ClassJob.GameData.NameEnglish);
                    //DuoLog.Information($"{pos} has ice");
                    tethers.Add(pos, 0);
                    maybeDrawFirstChainTarget();
                }
            // Fire Tether
            } else if(vfxPath == "vfx/channeling/eff/chn_fire_mouth01x.avfx")
            {
                if (target.TryGetObject(out var pv) && pv is PlayerCharacter pvc)
                {
                    var pos = findPosition(pvc.ClassJob.GameData.NameEnglish);
                    //DuoLog.Information($"{pos} has fire");
                    tethers.Add(pos, 1);
                    maybeDrawFirstChainTarget();
                }
            }
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
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            /*
            if (set.Action == null) return;
            if (set.Action.RowId == 27955 || set.Action.RowId == 27956 || set.Action.RowId == 2795)
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
            tethers.Clear();
            NoElement.Enabled = false;
            SwElement.Enabled = false;
            SeElement.Enabled = false;
        }

        public override void OnUpdate()
        {
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
            public string NorthAnchorName = "Samurai";
            public string NorthPartnerName = "Ninja";
            public string SWAnchorName = "Sage";
            public string SWPartnerName = "Dancer";
            public string SEAnchorName = "Summoner";
            public string SEPartnerName = "Astrologian";
        }

        public override void OnSettingsDraw()
        {
            if (Conf.NorthAnchorName == null)
                Conf.NorthAnchorName = "Samurai";
            ImGui.SetNextItemWidth(200f);
            ImGui.InputText("NorthAnchor", ref Conf.NorthAnchorName, 50);
            if (Conf.NorthPartnerName == null)
                Conf.NorthPartnerName = "Ninja";
            ImGui.SetNextItemWidth(200f);
            ImGui.InputText("NorthPartner", ref Conf.NorthPartnerName, 50);
            ImGui.Separator();

            if (Conf.SWAnchorName == null)
                Conf.SWAnchorName = "Sage";
            ImGui.SetNextItemWidth(200f);
            ImGui.InputText("SWAnchor", ref Conf.SWAnchorName, 50);
            if (Conf.SWPartnerName == null)
                Conf.SWPartnerName = "Dancer";
            ImGui.SetNextItemWidth(200f);
            ImGui.InputText("SWPartner", ref Conf.SWPartnerName, 50);
            ImGui.Separator();

            if (Conf.SEAnchorName == null)
                Conf.SEAnchorName = "Summoner";
            ImGui.SetNextItemWidth(200f);
            ImGui.InputText("SEAnchor", ref Conf.SEAnchorName, 50);
            if (Conf.SEPartnerName == null)
                Conf.SEPartnerName = "Astrologian";
            ImGui.SetNextItemWidth(200f);
            ImGui.InputText("SEPartner", ref Conf.SEPartnerName, 50);

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
