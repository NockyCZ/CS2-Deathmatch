using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void RemoveEntities()
        {
            var bombSites = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("func_bomb_target");
            foreach (var site in bombSites)
            {
                if (site.IsValid)
                {
                    site.Remove();
                }
            }
            var buyZones = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("func_buyzone");
            foreach (var zone in buyZones)
            {
                if (zone.IsValid)
                {
                    zone.Remove();
                }
            }
        }
        public void RemoveBreakableEntities()
        {
            var entities = Utilities.FindAllEntitiesByDesignerName<CBreakable>("prop_dynamic")
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBreakable>("func_breakable"));
            foreach (var entity in entities)
            {
                if (entity.IsValid)
                {
                    entity.AcceptInput("Break");
                }
            }
        }
        public void RemoveBeams()
        {
            var beams = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("beam");
            foreach (var beam in beams)
            {
                if (beam.IsValid)
                {
                    beam.Remove();
                }
            }
        }
        public static CCSGameRules GameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }
    }
}