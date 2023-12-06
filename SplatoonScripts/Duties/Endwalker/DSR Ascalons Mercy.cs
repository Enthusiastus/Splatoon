using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms.Design;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class DSR_Ascalons_Mercy : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 968 };
        public override Metadata? Metadata => new(1, "Enthusiastus");

        List<ConeData> Cones = new();
        List<Element> ConeElements = new();

        public class ConeData
        {
            public uint source;
            public uint target;
            public Element cone;
            public long DelTime = -1;
        }

        const uint ThordanDataId = 12604;
        bool positionDynamic = true;

        BattleNpc? Thordan => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == ThordanDataId) as BattleNpc;
        Vector2 Center = new(100, 100);

        public override void OnSetup()
        {
            var code = "{\"Name\":\"\",\"type\":5,\"refX\":103.03228,\"refY\":99.94743,\"radius\":20.0,\"coneAngleMin\":-15,\"coneAngleMax\":15,\"color\":3355506687,\"FillStep\":2.0,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"Filled\":true}";
            for (var i = 0; i < 8; i++)
            {
                var e = Controller.RegisterElementFromCode($"Cone{i}", code);
                e.Enabled = false;
                ConeElements.Add(e);
            }
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        public override void OnMessage(string Message)
        {
            if (Message.Contains("(3632>25544)"))
            {
                //DuoLog.Information($"Found Ascalon cast start");
                var players = FakeParty.Get();
                int num = 0;
                foreach (var p in players)
                {
                    //DuoLog.Information($"{p.Name} is @ {p.Position.X}/{p.Position.Z}/{p.Position.Y}");
                    var e = ConeElements[num];
                    e.color = C.Col1.ToUint();
                    Cones.Add(new() { source = Thordan.ObjectId, target = p.ObjectId, cone = e });
                    positionDynamic = true;
                    num++;
                }
            }
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            if (set.Action == null) return;
            if (set.Action.RowId == 25544)
            {
                //DuoLog.Information($"Position locked!");
                positionDynamic = false;
                for (var i = 0; i < Cones.Count; ++i)
                {
                    var c = Cones[i];
                    var e = ConeElements[i];
                    e.color = C.Col2.ToUint();
                    c.DelTime = Environment.TickCount64 + 2 * 1000;
                }
                //DuoLog.Information($"Thordan is @ {Thordan.Position.X}/{Thordan.Position.Z}/{Thordan.Position.Y}");
            }
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
            Cones.Clear();
            ConeElements.Each(x => x.Enabled = false);
        }

        public override void OnUpdate()
        {
            if (positionDynamic)
            {
                int num = 0;
                foreach (var x in Cones)
                {
                    if (x.source.TryGetObject(out var src) && src is BattleChara t && x.target.TryGetObject(out var tgt) && tgt is PlayerCharacter pc && Controller.TryGetElementByName($"Cone{num}", out var c))
                    {
                        c.Enabled = true;
                        c.AdditionalRotation = (180 + MathHelper.GetRelativeAngle(Thordan.Position, pc.Position)).DegreesToRadians();
                        c.SetRefPosition(t.Position);
                        //DuoLog.Information($"Found info to draw from {Thordan.Name} ({Thordan.ObjectId}) to {pc.Name} ({pc.ObjectId})");
                        num++;
                    }
                }
            }
            else
            {
                for (var i = Cones.Count - 1; i >= 0; --i)
                {
                    var c = Cones[i];
                    if (c.DelTime > 0 && c.DelTime < Environment.TickCount64)
                    {
                        c.cone.Enabled = false;
                        Cones.Remove(c);
                    }
                }
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                Off();
            }
        }

        Config C => Controller.GetConfig<Config>();
        public class Config : IEzConfig
        {
            public Vector4 Col1 = Vector4FromRGBA(0xFFFF00C8);
            public Vector4 Col2 = Vector4FromRGBA(0xFF0000C8);
        }

        public override void OnSettingsDraw()
        {
            ImGui.ColorEdit4("Color unlocked", ref C.Col1, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Color locked", ref C.Col2, ImGuiColorEditFlags.NoInputs);
        }

        public unsafe static Vector4 Vector4FromRGBA(uint col)
        {
            byte* bytes = (byte*)&col;
            return new Vector4((float)bytes[3] / 255f, (float)bytes[2] / 255f, (float)bytes[1] / 255f, (float)bytes[0] / 255f);
        }
    }
}
