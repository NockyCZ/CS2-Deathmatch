using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Deathmatch;
public partial class Deathmatch
{
    public bool CanSeeSpawn(CCSPlayerPawn? pawn, Vector pos2)
    {
        var playerEyeAngles = pawn?.EyeAngles;
        var angleToPlayer = CalculateAngle(CreateNewVector(pawn?.AbsOrigin), CreateNewVector(pos2));
        if (angleToPlayer == null || playerEyeAngles == null)
        {
            Server.PrintToChatAll("pawn angleToPlayer je null");
            return false;
        }

        if (IsPlayerBehind(playerEyeAngles, angleToPlayer))
        {
            Server.PrintToChatAll($"{pawn!.OriginalController.Value!.PlayerName} is behind");
            return false;
        }

        var Position = TraceShape(pawn?.AbsOrigin!, angleToPlayer, 0x1C1003, true, true, 3f);
        if (Position != null)
        {
            Server.PrintToChatAll($"{pawn!.OriginalController.Value!.PlayerName} pawn nevidí spawn");
            return false;
        }

        Server.PrintToChatAll($"{pawn!.OriginalController.Value!.PlayerName} vidí spawn");
        return true;
    }

    public Vector? CreateNewVector(Vector? vector)
    {
        if (vector == null)
            return null;
        return new Vector(vector.X, vector.Y, vector.Z);
    }

    public void DrawLaserBetween(Vector startPos, Vector endPos, Color color, float life, float width)
    {
        if (startPos == null || endPos == null)
            return;

        var beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null)
            return;

        beam.Render = color;
        beam.Width = width;

        beam.Teleport(startPos, QAngle.Zero, Vector.Zero);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();

        if (life != -1) AddTimer(life, () => { if (beam != null && beam.IsValid) beam.Remove(); });
    }

    public bool IsPlayerBehind(QAngle player1EyeAngles, QAngle player2EyeAngles)
    {
        float yawDifference = Math.Abs(player1EyeAngles.Y - player2EyeAngles.Y);
        if (yawDifference > 180)
            yawDifference = 360 - yawDifference;

        return yawDifference > 52;
    }

    public QAngle? CalculateAngle(Vector? origin1, Vector? origin2)
    {
        if (origin1 == null || origin2 == null)
            return null;

        Vector direction = new Vector(
            origin2.X - origin1.X,
            origin2.Y - origin1.Y,
            origin2.Z - origin1.Z
        );

        direction = Normalize(direction);

        float yaw = (float)(Math.Atan2(direction.Y, direction.X) * (180.0 / Math.PI));

        float hypotenuse = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        float pitch = (float)(Math.Atan2(-direction.Z, hypotenuse) * (180.0 / Math.PI));

        return new QAngle(pitch, yaw, 0);
    }

    public Vector Normalize(Vector v)
    {
        float length = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return new Vector(v.X / length, v.Y / length, v.Z / length);
    }
}
