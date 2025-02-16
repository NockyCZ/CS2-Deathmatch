using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void RemoveEntities()
        {
            var entities = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("func_bomb_target")
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBreakable>("func_buyzone"));
            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;
                entity.Remove();
            }
        }
        public void RemoveBreakableEntities()
        {
            var entities = Utilities.FindAllEntitiesByDesignerName<CBreakable>("prop_dynamic")
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBreakable>("func_breakable"));
            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;
                entity.Remove();
            }
        }
        public void RemoveBeams()
        {
            var beams = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("beam");
            foreach (var beam in beams)
            {
                if (beam == null)
                    continue;
                beam.Remove();
            }
        }
        public void RemoveSpawnModels()
        {
            foreach (var model in savedSpawnsModel)
            {
                if (model == null || !model.IsValid)
                    continue;
                model.AcceptInput("Kill");
            }
            savedSpawnsModel.Clear();
        }
        public void SetGameRules()
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
            GameRules = gameRules.GameRules;
        }
    }
}