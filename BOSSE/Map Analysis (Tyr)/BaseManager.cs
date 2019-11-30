/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 * Map analyzis based on Tyr bot
 */
namespace BOSSE.Tyr
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using System.IO;
    using System.Diagnostics;

    using static BOSSE.Tyr.UnitTypes;

    public class BaseManager
    {
        public List<Base> Bases { get; internal set; } = new List<Base>();
        public int AvailableGasses { get; internal set; }
        public Point2D NaturalDefensePos { get; private set; }
        public Point2D MainDefensePos { get; private set; }
        public Base Main { get; private set; }
        public Base Natural { get; private set; }
        public Base Pocket { get; private set; }

        public void OnStart()
        {
            int[,] distances = Tyr.MapAnalyzer.Distances(SC2Util.To2D(Tyr.MapAnalyzer.StartLocation));
            BaseLocation natural = null;
            int dist = 1000000000;
            foreach (BaseLocation loc in Tyr.MapAnalyzer.BaseLocations)
            {
                int distanceToMain = distances[(int)loc.Pos.X, (int)loc.Pos.Y];
                Base newBase = new Base() { BaseLocation = loc, DistanceToMain = distanceToMain };
                Bases.Add(newBase);

                if (distanceToMain <= 5)
                {
                    Main = newBase;
                }
                else if (Tyr.MapAnalyzer.MainAndPocketArea[loc.Pos])
                {
                    Pocket = newBase;
                    Log.Info("Found pocket base at: " + Pocket.BaseLocation.Pos);
                }
                else if (distanceToMain < dist)
                {
                    natural = loc;
                    dist = distanceToMain;
                    Natural = newBase;
                }

                Point2D mineralPos = new Point2D() { X = 0, Y = 0 };
                foreach (MineralField field in loc.MineralFields)
                {
                    mineralPos.X += field.Pos.X;
                    mineralPos.Y += field.Pos.Y;
                }
                mineralPos.X /= loc.MineralFields.Count;
                mineralPos.Y /= loc.MineralFields.Count;

                newBase.MineralLinePos = mineralPos;
                newBase.OppositeMineralLinePos = new Point2D() { X = 2 * loc.Pos.X - mineralPos.X, Y = 2 * loc.Pos.Y - mineralPos.Y };

                Point2D furthest = null;
                float mineralDist = -1;
                foreach (MineralField field in loc.MineralFields)
                {
                    float newDist = SC2Util.DistanceSq(mineralPos, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                foreach (Gas field in loc.Gasses)
                {
                    float newDist = SC2Util.DistanceSq(mineralPos, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                newBase.MineralSide1 = furthest;

                furthest = null;
                mineralDist = -1;
                foreach (MineralField field in loc.MineralFields)
                {
                    float newDist = SC2Util.DistanceSq(newBase.MineralSide1, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                foreach (Gas field in loc.Gasses)
                {
                    float newDist = SC2Util.DistanceSq(newBase.MineralSide1, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                newBase.MineralSide2 = furthest;

                newBase.MineralSide1 = new Point2D() { X = (newBase.MineralSide1.X + newBase.BaseLocation.Pos.X) / 2f, Y = (newBase.MineralSide1.Y + newBase.BaseLocation.Pos.Y) / 2f };
                newBase.MineralSide2 = new Point2D() { X = (newBase.MineralSide2.X + newBase.BaseLocation.Pos.X) / 2f, Y = (newBase.MineralSide2.Y + newBase.BaseLocation.Pos.Y) / 2f };
            }

            NaturalDefensePos = Tyr.MapAnalyzer.Walk(natural.Pos, Tyr.MapAnalyzer.EnemyDistances, 10);
            int distToEnemy = Tyr.MapAnalyzer.EnemyDistances[(int)NaturalDefensePos.X, (int)NaturalDefensePos.Y];
            int wallDist = Tyr.MapAnalyzer.WallDistances[(int)NaturalDefensePos.X, (int)NaturalDefensePos.Y];
            for (int x = (int)NaturalDefensePos.X - 5; x <= NaturalDefensePos.X + 5; x++)
                for (int y = (int)NaturalDefensePos.Y - 5; y <= NaturalDefensePos.Y + 5; y++)
                {
                    if (SC2Util.DistanceSq(SC2Util.Point(x, y), natural.Pos) <= 7 * 7
                        || SC2Util.DistanceSq(SC2Util.Point(x, y), natural.Pos) >= 10 * 10)
                        continue;
                    if (Tyr.MapAnalyzer.EnemyDistances[x, y] > distToEnemy)
                        continue;
                    int newDist = Tyr.MapAnalyzer.WallDistances[x, y];
                    if (newDist > wallDist)
                    {
                        wallDist = newDist;
                        NaturalDefensePos = SC2Util.Point(x, y);
                    }
                }
            MainDefensePos = Tyr.MapAnalyzer.Walk(SC2Util.To2D(Tyr.MapAnalyzer.StartLocation), Tyr.MapAnalyzer.EnemyDistances, 10);
        }
    }

    public class Base
    {
        public BaseLocation BaseLocation { get; set; }
        //public Agent ResourceCenter { get; set; }
        public int ResourceCenterFinishedFrame = -1;
        public int Owner { get; set; }
        public Dictionary<uint, int> BuildingCounts = new Dictionary<uint, int>();
        public Dictionary<uint, int> BuildingsCompleted = new Dictionary<uint, int>();
        public int DistanceToMain { get; set; }
        public bool UnderAttack;
        public bool Evacuate;
        public bool Blocked;
        public Point2D MineralLinePos;
        public Point2D OppositeMineralLinePos;
        public Point2D MineralSide1;
        public Point2D MineralSide2;
    }
}
