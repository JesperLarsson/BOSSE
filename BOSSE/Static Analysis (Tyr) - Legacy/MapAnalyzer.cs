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
    using System.IO;
    using System.Diagnostics;

    using DebugGui;
    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static global::BOSSE.Tyr.UnitTypes;


    public static class Tyr
    {
        public static bool Debug = true;
        public static bool OldMapData = false;
        public static uint PlayerId = 0;

        public static MapAnalyzer MapAnalyzer = new MapAnalyzer();
        public static BaseManager BaseManager = new BaseManager();
        public static TargetManager TargetManager = new TargetManager();

        public static ResponseGameInfo GameInfo;
        public static ResponseObservation Observation;

        public static void Initialize()
        {
            Tyr.Debug = Globals.IsSinglePlayer;
            Tyr.PlayerId = Globals.PlayerId;
            Tyr.GameInfo = CurrentGameState.GameInformation;
            Tyr.Observation = CurrentGameState.ObservationState;
            Tyr.MapAnalyzer.Analyze();
            Tyr.TargetManager.OnStart();
            Tyr.BaseManager.OnStart();
            Tyr.MapAnalyzer.AddToGui();
        }
    }

    public class MapAnalyzer
    {
        public List<BaseLocation> BaseLocations { get; private set; } = new List<BaseLocation>();
        public Point StartLocation { get; private set; }
        public BoolGrid Placement;
        public BoolGrid StartArea;
        public BoolGrid MainAndPocketArea;
        private int[,] enemyDistances;
        private int[,] MainDistancesStore;
        public int[,] WallDistances;

        // Positions for wallin, needs better place.
        public Point2D building1 = null;
        public Point2D building2 = null;
        public Point2D building3 = null;

        public BoolGrid Ramp;
        public BoolGrid Pathable;
        //public BoolGrid UnPathable;
        //private Point2D EnemyRamp = null;

        public void AddToGui()
        {
            List<KeyValuePair<Point2D, string>> newPoints = new List<KeyValuePair<Point2D, string>>();

            Point2D sRamp = this.GetMainRamp();
            newPoints.Add(new KeyValuePair<Point2D, string>(sRamp, "Ramp"));

            Point2D eRamp = this.GetEnemyRamp();
            newPoints.Add(new KeyValuePair<Point2D, string>(eRamp, "EnemyRamp"));

            if (Tyr.BaseManager.Natural != null)
            {
                Point2D point = Tyr.BaseManager.Natural.BaseLocation.Pos;
                newPoints.Add(new KeyValuePair<Point2D, string>(point, "Natural"));
            }

            if (Tyr.BaseManager.Main != null)
            {
                Point2D point = Tyr.BaseManager.Main.BaseLocation.Pos;
                newPoints.Add(new KeyValuePair<Point2D, string>(point, "Main"));
            }

            if (Tyr.BaseManager.MainDefensePos != null)
            {
                Point2D point = Tyr.BaseManager.MainDefensePos;
                newPoints.Add(new KeyValuePair<Point2D, string>(point, "MainDefense"));
            }

            if (Tyr.BaseManager.NaturalDefensePos != null)
            {
                Point2D point = Tyr.BaseManager.NaturalDefensePos;
                newPoints.Add(new KeyValuePair<Point2D, string>(point, "NaturalDefense"));
            }

            for (int i = 0; i < Tyr.TargetManager.PotentialEnemyStartLocations.Count; i++)
            {
                Point2D point = Tyr.TargetManager.PotentialEnemyStartLocations[i];
                newPoints.Add(new KeyValuePair<Point2D, string>(point, "PossibleEnemyStart" + i));
            }

            TerrainDebugMap.MarkedPoints = newPoints;
        }

        public void Analyze()
        {
            // Determine the start location.
            foreach (Unit unit in Tyr.Observation.Observation.RawData.Units)
                if (unit.Owner == Tyr.PlayerId && UnitTypes.ResourceCenters.Contains(unit.UnitType))
                    StartLocation = unit.Pos;

            List<MineralField> mineralFields = new List<MineralField>();

            foreach (Unit mineralField in Tyr.Observation.Observation.RawData.Units)
                if (UnitTypes.MineralFields.Contains(mineralField.UnitType))
                    mineralFields.Add(new MineralField() { Pos = mineralField.Pos, Tag = mineralField.Tag });

            // The Units provided in our observation are not guaranteed to be in the same order every game.
            // To ensure the base finding algorithm finds the same base location every time we sort the mineral fields by position.
            mineralFields.Sort((a, b) => (int)(2 * (a.Pos.X + a.Pos.Y * 10000 - b.Pos.X - b.Pos.Y * 10000)));

            Dictionary<ulong, int> mineralSetIds = new Dictionary<ulong, int>();
            List<List<MineralField>> mineralSets = new List<List<MineralField>>();
            int currentSet = 0;
            foreach (MineralField mineralField in mineralFields)
            {
                if (mineralSetIds.ContainsKey(mineralField.Tag))
                    continue;
                BaseLocation baseLocation = new BaseLocation();
                BaseLocations.Add(baseLocation);
                mineralSetIds.Add(mineralField.Tag, currentSet);
                baseLocation.MineralFields.Add(mineralField);

                for (int i = 0; i < baseLocation.MineralFields.Count; i++)
                {
                    MineralField mineralFieldA = baseLocation.MineralFields[i];
                    foreach (MineralField closeMineralField in mineralFields)
                    {
                        if (mineralSetIds.ContainsKey(closeMineralField.Tag))
                            continue;

                        if (SC2Util.DistanceSq(mineralFieldA.Pos, closeMineralField.Pos) <= 4 * 4)
                        {
                            mineralSetIds.Add(closeMineralField.Tag, currentSet);
                            baseLocation.MineralFields.Add(closeMineralField);
                        }
                    }
                }
                currentSet++;
            }

            List<Gas> gasses = new List<Gas>();
            foreach (Unit unit in Tyr.Observation.Observation.RawData.Units)
                if (UnitTypes.GasGeysers.Contains(unit.UnitType))
                    gasses.Add(new Gas() { Pos = unit.Pos, Tag = unit.Tag });

            // The Units provided in our observation are not guaranteed to be in the same order every game.
            // To ensure the base finding algorithm finds the same base location every time we sort the gasses by position.
            gasses.Sort((a, b) => (int)(2 * (a.Pos.X + a.Pos.Y * 10000 - b.Pos.X - b.Pos.Y * 10000)));

            foreach (BaseLocation loc in BaseLocations)
                DetermineFinalLocation(loc, gasses);

            if (Tyr.GameInfo.MapName.Contains("Blueshift"))
            {
                foreach (BaseLocation loc in BaseLocations)
                {
                    if (SC2Util.DistanceSq(loc.Pos, SC2Util.Point(141.5f, 112.5f)) <= 5 * 5 && (loc.Pos.X != 141.5 || loc.Pos.Y != 112.5))
                    {
                        Log.Bulk("Incorrect base location, fixing: " + loc.Pos);
                        loc.Pos = SC2Util.Point(141.5f, 112.5f);
                    }
                    else if (SC2Util.DistanceSq(loc.Pos, SC2Util.Point(34.5f, 63.5f)) <= 5 * 5 && (loc.Pos.X != 34.5 || loc.Pos.Y != 63.5))
                    {
                        Log.Bulk("Incorrect base location, fixing: " + loc.Pos);
                        loc.Pos = SC2Util.Point(34.5f, 63.5f);
                    }
                }

            }

            Stopwatch stopWatch = Stopwatch.StartNew();

            int width = Tyr.GameInfo.StartRaw.MapSize.X;
            int height = Tyr.GameInfo.StartRaw.MapSize.Y;

            Placement = new ImageBoolGrid(Tyr.GameInfo.StartRaw.PlacementGrid);
            StartArea = Placement.GetConnected(SC2Util.To2D(StartLocation));

            ArrayBoolGrid startLocations = new ArrayBoolGrid(Placement.Width(), Placement.Height());
            foreach (Point2D startLoc in Tyr.GameInfo.StartRaw.StartLocations)
                for (int x = -2; x <= 2; x++)
                    for (int y = -2; y <= 2; y++)
                        startLocations[(int)startLoc.X + x, (int)startLoc.Y + y] = true;
            for (int x = -2; x <= 2; x++)
                for (int y = -2; y <= 2; y++)
                    startLocations[(int)StartLocation.X + x, (int)StartLocation.Y + y] = true;

            BoolGrid unPathable;
            if (Tyr.OldMapData)
            {
                unPathable = new ImageBoolGrid(Tyr.GameInfo.StartRaw.PathingGrid).GetAnd(startLocations.Invert());
                Pathable = unPathable.Invert();
            }
            else
            {
                Pathable = new ImageBoolGrid(Tyr.GameInfo.StartRaw.PathingGrid).GetOr(startLocations);
                unPathable = Pathable.Invert();
            }

            BoolGrid chokes = Placement.Invert().GetAnd(Pathable);
            BoolGrid mainExits = chokes.GetAdjacent(StartArea);

            enemyDistances = EnemyDistances;

            int dist = 1000;
            Point2D mainRamp = null;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (mainExits[x, y])
                    {
                        int newDist = enemyDistances[x, y];
                        if (newDist < dist)
                        {
                            dist = newDist;
                            mainRamp = SC2Util.Point(x, y);
                        }
                    }
                }

            Ramp = chokes.GetConnected(mainRamp);

            BoolGrid pathingWithoutRamp = Pathable.GetAnd(Ramp.Invert());
            MainAndPocketArea = pathingWithoutRamp.GetConnected(SC2Util.To2D(StartLocation));

            if (BotConstants.SpawnAsRace == Race.Protoss)
                DetermineWall(Ramp, unPathable);

            WallDistances = Distances(unPathable);

            stopWatch.Stop();
            Log.Bulk("Total time to find wall: " + stopWatch.ElapsedMilliseconds);
        }

        public Point2D GetMainRamp()
        {
            float totalPoints = 0;
            float totalX = 0;
            float totalY = 0;
            for (int x = 0; x < Ramp.Width(); x++)
                for (int y = 0; y < Ramp.Height(); y++)
                {
                    if (Ramp[x, y])
                    {
                        totalX += x;
                        totalY += y;
                        totalPoints++;
                    }
                }
            return SC2Util.Point((int)(totalX / totalPoints) + 1f, (int)(totalY / totalPoints) + 1f);
        }

        private void DetermineFinalLocation(BaseLocation loc, List<Gas> gasses)
        {
            for (int i = 0; i < gasses.Count; i++)
            {
                foreach (MineralField field in loc.MineralFields)
                {
                    if (SC2Util.DistanceSq(field.Pos, gasses[i].Pos) <= 8 * 8)
                    {
                        loc.Gasses.Add(gasses[i]);
                        gasses[i] = gasses[gasses.Count - 1];
                        gasses.RemoveAt(gasses.Count - 1);
                        i--;
                        break;
                    }
                }
            }

            if (loc.Gasses.Count == 1)
            {
                for (int i = 0; i < gasses.Count; i++)
                    if (SC2Util.DistanceSq(loc.Gasses[0].Pos, gasses[i].Pos) <= 8 * 8)
                    {
                        loc.Gasses.Add(gasses[i]);
                        gasses[i] = gasses[gasses.Count - 1];
                        gasses.RemoveAt(gasses.Count - 1);
                        i--;
                        break;
                    }
            }

            float x = 0;
            float y = 0;
            foreach (MineralField field in loc.MineralFields)
            {
                x += (int)field.Pos.X;
                y += (int)field.Pos.Y;
            }
            x /= loc.MineralFields.Count;
            y /= loc.MineralFields.Count;

            // Round to nearest half position. Nexii are 5x5 and therefore always centered in the middle of a tile.
            x = (int)(x) + 0.5f;
            y = (int)(y) + 0.5f;

            // Temporary position, we still need a proper position.
            loc.Pos = SC2Util.Point(x, y);


            MineralField closest = null;
            float distance = 10000;
            foreach (MineralField field in loc.MineralFields)
                if (SC2Util.DistanceGrid(field.Pos, loc.Pos) < distance)
                {
                    distance = SC2Util.DistanceGrid(field.Pos, loc.Pos);
                    closest = field;
                }

            // Move the estimated base position slightly away from the closest mineral.
            // This ensures that the base location will not end up on the far side of the minerals.
            if (closest.Pos.X < loc.Pos.X)
                loc.Pos.X += 2;
            else if (closest.Pos.X > loc.Pos.X)
                loc.Pos.X -= 2;
            if (closest.Pos.Y < loc.Pos.Y)
                loc.Pos.Y += 2;
            else if (closest.Pos.Y > loc.Pos.Y)
                loc.Pos.Y -= 2;

            bool test = SC2Util.DistanceSq(loc.Pos, new Point2D() { X = 127.5f, Y = 77.5f }) <= 10 * 10;

            float closestDist = 1000000;
            Point2D approxPos = loc.Pos;
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j == 0 || j < i; j++)
                {
                    float maxDist;
                    Point2D newPos;
                    newPos = SC2Util.Point(approxPos.X + i - j, approxPos.Y + j);
                    maxDist = checkPosition(newPos, loc);
                    if (maxDist < closestDist)
                    {
                        loc.Pos = newPos;
                        closestDist = maxDist;
                    }

                    newPos = SC2Util.Point(approxPos.X + i - j, approxPos.Y - j);
                    maxDist = checkPosition(newPos, loc);
                    if (maxDist < closestDist)
                    {
                        loc.Pos = newPos;
                        closestDist = maxDist;
                    }

                    newPos = SC2Util.Point(approxPos.X - i + j, approxPos.Y + j);
                    maxDist = checkPosition(newPos, loc);
                    if (maxDist < closestDist)
                    {
                        loc.Pos = newPos;
                        closestDist = maxDist;
                    }

                    newPos = SC2Util.Point(approxPos.X - i + j, approxPos.Y - j);
                    maxDist = checkPosition(newPos, loc);
                    if (maxDist < closestDist)
                    {
                        loc.Pos = newPos;
                        closestDist = maxDist;
                    }
                }
            }

            if (loc.Gasses.Count != 2)
                Log.Bulk("Wrong number of gasses, found: " + loc.Gasses.Count);
            if (closestDist >= 999999)
                Log.Bulk("Unable to find proper base placement: " + loc.Pos);

        }

        private void DrawPathing(BoolGrid pathable, BoolGrid placememt)
        {
            if (!Tyr.Debug)
                return;

            int width = Tyr.GameInfo.StartRaw.PathingGrid.Size.X;
            int height = Tyr.GameInfo.StartRaw.PathingGrid.Size.Y;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (pathable[x, y] && placememt[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Purple);
                    else if (pathable[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Blue);
                    else if (placememt[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Red);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                }

            bmp.Save(Directory.GetCurrentDirectory() + "/data/Pathing.png");
        }

        private void DrawDistances(int[,] distances)
        {
            if (!Tyr.Debug)
                return;

            int width = distances.GetLength(0);
            int height = distances.GetLength(1);
            int max = 1;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (distances[x, y] < 1000000000)
                        max = Math.Max(max, distances[x, y]);

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (distances[x, y] >= 1000000000)
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Red);
                    else
                    {
                        int val = Math.Min(255, distances[x, y] * 255 / max);
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.FromArgb(val, val, val));
                    }
                }

            bmp.Save(Directory.GetCurrentDirectory() + "/data/Distances.png");
        }

        private void DrawRamp(BoolGrid startArea, BoolGrid chokes, BoolGrid ramps)
        {
            if (!Tyr.Debug)
                return;

            int width = Tyr.GameInfo.StartRaw.PathingGrid.Size.X;
            int height = Tyr.GameInfo.StartRaw.PathingGrid.Size.Y;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (ramps[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Blue);
                    else if (chokes[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Orange);
                    else if (startArea[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);

                }

            bmp.Save(Directory.GetCurrentDirectory() + "/data/Ramp.png");
        }

        private void DrawPathingGrid()
        {
            if (!Tyr.Debug)
                return;

            int width = Tyr.GameInfo.StartRaw.PathingGrid.Size.X;
            int height = Tyr.GameInfo.StartRaw.PathingGrid.Size.Y;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int val = SC2Util.GetDataValue(Tyr.GameInfo.StartRaw.PathingGrid, x, y);
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.FromArgb(val, val, val));
                }
            bmp.Save(Directory.GetCurrentDirectory() + "/data/PathingGrid.png");
        }
        private void DrawPathable()
        {
            if (!Tyr.Debug)
                return;

            int width = Tyr.GameInfo.StartRaw.PathingGrid.Size.X;
            int height = Tyr.GameInfo.StartRaw.PathingGrid.Size.Y;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (SC2Util.GetTilePlacable(x, y))
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);
                }

            foreach (Unit unit in Tyr.Observation.Observation.RawData.Units)
            {
                if (UnitTypes.GasGeysers.Contains(unit.UnitType))
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            bmp.SetPixel((int)unit.Pos.X + dx, height - 1 - (int)unit.Pos.Y - dy, System.Drawing.Color.Cyan);
                if (UnitTypes.MineralFields.Contains(unit.UnitType))
                    for (int dx = 0; dx <= 1; dx++)
                        bmp.SetPixel((int)(unit.Pos.X - 0.5f) + dx, height - 1 - (int)(unit.Pos.Y - 0.5f), System.Drawing.Color.Cyan);
            }
            bmp.Save(Directory.GetCurrentDirectory() + "/data/Pathable.png");
        }

        private void DrawBases(int width, int height)
        {
            if (!Tyr.Debug)
                return;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (Pathable[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                }
            foreach (BaseLocation loc in BaseLocations)
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                        bmp.SetPixel((int)loc.Pos.X + dx, height - 1 - (int)loc.Pos.Y - dy, System.Drawing.Color.Blue);

            foreach (Unit unit in Tyr.Observation.Observation.RawData.Units)
            {
                if (UnitTypes.GasGeysers.Contains(unit.UnitType))
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            bmp.SetPixel((int)unit.Pos.X + dx, height - 1 - (int)unit.Pos.Y - dy, System.Drawing.Color.Cyan);
                if (UnitTypes.MineralFields.Contains(unit.UnitType))
                    for (int dx = 0; dx <= 1; dx++)
                        bmp.SetPixel((int)(unit.Pos.X - 0.5f) + dx, height - 1 - (int)(unit.Pos.Y - 0.5f), System.Drawing.Color.Cyan);
            }
            bmp.Save(Directory.GetCurrentDirectory() + "/data/Bases.png");

        }

        public int MapHeight(int x, int y)
        {
            return SC2Util.GetDataValue(Tyr.GameInfo.StartRaw.TerrainHeight, x, y);
        }

        private void DetermineWall(BoolGrid ramp, BoolGrid unPathable)
        {
            BoolGrid rampAdjacent = unPathable.GetAdjacent(ramp);
            BoolGrid rampSides = unPathable.GetConnected(rampAdjacent, 5);
            List<BoolGrid> sides = rampSides.GetGroups();

            BoolGrid shrunkenStart = StartArea.Shrink();

            List<Point2D> building1Positions = Placable(sides[0], shrunkenStart).ToList();
            List<Point2D> building2Positions = Placable(sides[1], shrunkenStart).ToList();

            float wallScore = 1000;


            foreach (Point2D p1 in building1Positions)
                foreach (Point2D p2 in building2Positions)
                {
                    if (System.Math.Abs(p1.X - p2.X) < 3 && System.Math.Abs(p1.Y - p2.Y) < 3)
                        continue;

                    float newScore = SC2Util.DistanceGrid(p1, p2);
                    if (newScore >= wallScore)
                        continue;

                    for (float i = -2.5f; i < 3; i++)
                    {
                        if (CheckPylon(SC2Util.Point(p1.X + 2.5f, p1.Y + i), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X + 2.5f, p1.Y + i);
                        }
                        if (CheckPylon(SC2Util.Point(p1.X - 2.5f, p1.Y + i), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X - 2.5f, p1.Y + i);
                        }
                        if (CheckPylon(SC2Util.Point(p1.X + i, p1.Y + 2.5f), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X + i, p1.Y + 2.5f);
                        }
                        if (CheckPylon(SC2Util.Point(p1.X + i, p1.Y - 2.5f), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X + i, p1.Y - 2.5f);
                        }
                    }
                }

        }

        private bool CheckPylon(Point2D pylon, Point2D p1, Point2D p2)
        {
            if (!StartArea[SC2Util.Point(pylon.X, pylon.Y)])
                return false;
            if (!StartArea[SC2Util.Point(pylon.X + 0.6f, pylon.Y)])
                return false;
            if (!StartArea[SC2Util.Point(pylon.X, pylon.Y + 0.6f)])
                return false;
            if (!StartArea[SC2Util.Point(pylon.X + 0.6f, pylon.Y + 0.6f)])
                return false;

            float dist = System.Math.Max(System.Math.Abs(pylon.X - p2.X), Math.Abs(pylon.Y - p2.Y));
            return dist > 2.4 && dist < 2.6;
        }

        private BoolGrid Placable(BoolGrid around, BoolGrid shrunkenStart)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(around.Width(), around.Height());
            for (int x = 0; x < around.Width(); x++)
                for (int y = 0; y < around.Height(); y++)
                {
                    if (around[x, y])
                    {
                        for (int i = -2; i <= 2; i++)
                        {
                            if (shrunkenStart[x + i, y - 2])
                                result[x + i, y - 2] = true;
                            if (shrunkenStart[x + i, y + 2])
                                result[x + i, y + 2] = true;
                            if (shrunkenStart[x + 2, y + i])
                                result[x + 2, y + i] = true;
                            if (shrunkenStart[x - 2, y + i])
                                result[x - 2, y + i] = true;
                        }
                    }
                }
            return result;
        }

        private float checkPosition(Point2D pos, BaseLocation loc)
        {
            foreach (MineralField mineralField in loc.MineralFields)
                if (SC2Util.DistanceGrid(mineralField.Pos, pos) <= 10
                    && System.Math.Abs(mineralField.Pos.X - pos.X) <= 5.5
                    && System.Math.Abs(mineralField.Pos.Y - pos.Y) <= 5.5)
                {
                    return 100000000;
                }
            foreach (Gas gas in loc.Gasses)
            {
                if (SC2Util.DistanceGrid(gas.Pos, pos) <= 11
                    && System.Math.Abs(gas.Pos.X - pos.X) <= 6.1
                    && System.Math.Abs(gas.Pos.Y - pos.Y) <= 6.1)
                {
                    return 100000000;
                }
                if (SC2Util.DistanceSq(gas.Pos, pos) >= 11 * 11)
                    return 100000000;
            }

            // Check if a resource center can actually be built here.
            for (float x = -2.5f; x < 2.5f + 0.1f; x++)
                for (float y = -2.5f; y < 2.5f + 0.1f; y++)
                    if (!SC2Util.GetTilePlacable((int)System.Math.Round(pos.X + x), (int)System.Math.Round(pos.Y + y)))
                        return 100000000;

            float maxDist = 0;
            foreach (MineralField mineralField in loc.MineralFields)
                maxDist += SC2Util.DistanceSq(mineralField.Pos, pos);

            foreach (Gas gas in loc.Gasses)
                maxDist += SC2Util.DistanceSq(gas.Pos, pos);
            return maxDist;
        }

        public Point2D CrossSpawn()
        {
            int dist = 0;
            Point2D crossSpawn = null;
            foreach (Point2D enemy in Tyr.GameInfo.StartRaw.StartLocations)
            {
                int enemyDist = (int)SC2Util.DistanceSq(enemy, StartLocation);
                if (enemyDist > dist)
                {
                    crossSpawn = enemy;
                    dist = enemyDist;
                }
            }
            return crossSpawn;
        }

        public int[,] EnemyDistances
        {
            get
            {
                if (enemyDistances == null)
                    enemyDistances = Distances(CrossSpawn());
                return enemyDistances;
            }
        }

        public int[,] MainDistances
        {
            get
            {
                if (MainDistancesStore == null)
                    MainDistancesStore = Distances(SC2Util.To2D(StartLocation));
                return MainDistancesStore;
            }
        }

        public int[,] Distances(Point2D pos)
        {
            int width = Tyr.GameInfo.StartRaw.MapSize.X;
            int height = Tyr.GameInfo.StartRaw.MapSize.Y;
            int[,] distances = new int[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    distances[x, y] = 1000000000;
            distances[(int)pos.X, (int)pos.Y] = 0;

            Queue<Point2D> q = new Queue<Point2D>();
            q.Enqueue(pos);

            while (q.Count > 0)
            {
                Point2D cur = q.Dequeue();
                check(Pathable, distances, q, SC2Util.Point(cur.X + 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(Pathable, distances, q, SC2Util.Point(cur.X - 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y + 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y - 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
            }

            return distances;
        }

        public int[,] Distances(BoolGrid start)
        {
            int width = Tyr.GameInfo.StartRaw.MapSize.X;
            int height = Tyr.GameInfo.StartRaw.MapSize.Y;
            int[,] distances = new int[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    distances[x, y] = 1000000000;


            Queue<Point2D> q = new Queue<Point2D>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (start[x, y])
                    {
                        distances[x, y] = 0;
                        q.Enqueue(SC2Util.Point(x, y));
                    }
                }

            while (q.Count > 0)
            {
                Point2D cur = q.Dequeue();
                check(Pathable, distances, q, SC2Util.Point(cur.X + 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(Pathable, distances, q, SC2Util.Point(cur.X - 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y + 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y - 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
            }

            return distances;
        }

        private void check(BoolGrid pathingData, int[,] distances, Queue<Point2D> q, Point2D pos, int width, int height, int newVal)
        {
            if (check(pathingData, pos, width, height) && distances[(int)pos.X, (int)pos.Y] == 1000000000)
            {
                q.Enqueue(pos);
                distances[(int)pos.X, (int)pos.Y] = newVal;
            }
        }

        private bool check(BoolGrid pathingData, Point2D pos, int width, int height)
        {
            if (pos.X < 0 || pos.X >= width || pos.Y < 0 || pos.Y >= height)
                return false;
            if (pathingData[pos])
                return true;

            foreach (Point2D p in Tyr.GameInfo.StartRaw.StartLocations)
                if (SC2Util.DistanceGrid(pos, p) <= 3)
                    return true;
            if (SC2Util.DistanceGrid(pos, StartLocation) <= 3)
                return true;
            return false;
        }

        private Point2D EnemyRamp = null;
        public Point2D GetEnemyRamp()
        {
            if (EnemyRamp != null)
                return EnemyRamp;
            if (Tyr.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return null;

            int width = Tyr.GameInfo.StartRaw.MapSize.X;
            int height = Tyr.GameInfo.StartRaw.MapSize.Y;

            Point2D start = Tyr.TargetManager.PotentialEnemyStartLocations[0];
            BoolGrid enemyStartArea = Placement.GetConnected(start);


            BoolGrid chokes = Placement.Invert().GetAnd(Pathable);
            BoolGrid mainExits = chokes.GetAdjacent(enemyStartArea);

            int[,] startDistances = Distances(SC2Util.To2D(StartLocation));

            int dist = 1000;
            Point2D mainRamp = null;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (mainExits[x, y])
                    {
                        int newDist = startDistances[x, y];
                        Log.Bulk("Ramp distance: " + newDist);
                        if (newDist < dist)
                        {
                            dist = newDist;
                            mainRamp = SC2Util.Point(x, y);
                        }
                    }
                }

            BoolGrid enemyRamp = chokes.GetConnected(mainRamp);

            float totalX = 0;
            float totalY = 0;
            float count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (enemyRamp[x, y])
                    {
                        totalX += x;
                        totalY += y;
                        count++;
                    }
                }

            EnemyRamp = new Point2D() { X = totalX / count, Y = totalY / count };
            return EnemyRamp;
        }

        public Point2D Walk(Point2D start, int[,] distances, int steps)
        {
            Point2D cur = start;
            int dx = 0;
            int dy = 0;
            for (int i = 0; i <= steps; i++)
            {
                List<Point2D> newDirections = new List<Point2D>();
                newDirections.Add(SC2Util.Point(cur.X + 1, cur.Y));
                newDirections.Add(SC2Util.Point(cur.X - 1, cur.Y));
                newDirections.Add(SC2Util.Point(cur.X, cur.Y + 1));
                newDirections.Add(SC2Util.Point(cur.X, cur.Y - 1));

                for (int j = newDirections.Count - 1; j >= 0; j--)
                {
                    Point2D next = newDirections[j];
                    if (distances[(int)cur.X, (int)cur.Y] <= distances[(int)next.X, (int)next.Y])
                        newDirections.RemoveAt(j);
                }

                if (newDirections.Count == 0)
                    break;

                Point2D goTo;
                if (newDirections.Count == 1 || newDirections[0].X - cur.X != dx || newDirections[0].Y - cur.Y != dy)
                    goTo = newDirections[0];
                else
                    goTo = newDirections[1];

                dx = (int)(goTo.X - cur.X);
                dy = (int)(goTo.Y - cur.Y);
                cur = goTo;

                if (distances[(int)cur.X, (int)cur.Y] == 0)
                    break;
            }
            return cur;
        }

        public BaseLocation GetEnemyNatural()
        {
            if (Tyr.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return null;
            int[,] distances = Distances(Tyr.TargetManager.PotentialEnemyStartLocations[0]);
            int dist = 1000000000;
            BaseLocation enemyNatural = null;
            foreach (BaseLocation loc in Tyr.MapAnalyzer.BaseLocations)
            {
                int distanceToMain = distances[(int)loc.Pos.X, (int)loc.Pos.Y];

                if (distanceToMain <= 5)
                    continue;

                if (distanceToMain < dist)
                {
                    dist = distanceToMain;
                    enemyNatural = loc;
                }
            }
            return enemyNatural;
        }

        public BaseLocation GetEnemyThird()
        {
            if (Tyr.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return null;
            float dist = 1000000000;
            BaseLocation enemyNatural = GetEnemyNatural();
            BaseLocation enemyThird = null;
            foreach (BaseLocation loc in Tyr.MapAnalyzer.BaseLocations)
            {
                float distanceToMain = SC2Util.DistanceSq(Tyr.TargetManager.PotentialEnemyStartLocations[0], loc.Pos);

                if (distanceToMain <= 4)
                    continue;

                if (SC2Util.DistanceSq(enemyNatural.Pos, loc.Pos) <= 2 * 2)
                    continue;

                if (distanceToMain < dist)
                {
                    dist = distanceToMain;
                    enemyThird = loc;
                }
            }
            return enemyThird;
        }
    }

    public class MineralField
    {
        public Point Pos { get; set; }
        public ulong Tag { get; set; }
    }

    public class ArrayBoolGrid : BoolGrid
    {
        private bool[,] data;
        public ArrayBoolGrid(int width, int height)
        {
            data = new bool[width, height];
        }

        public override BoolGrid Clone()
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y];
            return result;
        }

        internal override bool GetInternal(Point2D pos)
        {
            return data[(int)pos.X, (int)pos.Y];
        }

        internal void Set(Point2D pos, bool val)
        {
            data[(int)pos.X, (int)pos.Y] = val;
        }

        public new bool this[Point2D pos]
        {
            get { return Get(pos); }
            set { data[(int)pos.X, (int)pos.Y] = value; }
        }

        public new bool this[int x, int y]
        {
            get { return Get(SC2Util.Point(x, y)); }
            set { data[x, y] = value; }
        }

        public override int Width()
        {
            return data.GetLength(0);
        }

        public override int Height()
        {
            return data.GetLength(1);
        }
    }

    public class BaseLocation
    {
        public List<MineralField> MineralFields { get; internal set; } = new List<MineralField>();
        public List<Gas> Gasses { get; internal set; } = new List<Gas>();
        public Point2D Pos { get; set; }
    }

    public abstract class BoolGrid
    {
        private bool inverted = false;
        internal abstract bool GetInternal(Point2D pos);
        public bool Get(Point2D pos)
        {
            if (pos.X < 0 || pos.Y < 0 || pos.X >= Width() || pos.Y >= Height())
                return false;
            return GetInternal(pos) == (!inverted);
        }

        public BoolGrid Invert()
        {
            BoolGrid result = Clone();
            result.inverted = true;
            return result;
        }

        public abstract BoolGrid Clone();
        public abstract int Width();
        public abstract int Height();

        public bool this[Point2D pos]
        {
            get { return Get(pos); }
        }

        public bool this[int x, int y]
        {
            get { return Get(SC2Util.Point(x, y)); }
        }

        public BoolGrid GetConnected(Point2D point)
        {
            return GetConnected(point, new ArrayBoolGrid(Width(), Height()));
        }

        public BoolGrid GetConnected(Point2D point, ArrayBoolGrid encountered)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            Queue<Point2D> q = new Queue<Point2D>();
            q.Enqueue(point);
            while (q.Count > 0)
            {
                Point2D cur = q.Dequeue();
                if (cur.X < 0 || cur.Y < 0 || cur.X >= Width() || cur.Y >= Height())
                    continue;
                if (Get(cur) && !encountered[cur])
                {
                    result[cur] = true;
                    encountered[cur] = true;
                    q.Enqueue(SC2Util.Point(cur.X + 1, cur.Y));
                    q.Enqueue(SC2Util.Point(cur.X - 1, cur.Y));
                    q.Enqueue(SC2Util.Point(cur.X, cur.Y + 1));
                    q.Enqueue(SC2Util.Point(cur.X, cur.Y - 1));
                }
            }
            return result;
        }

        public BoolGrid GetAdjacent(BoolGrid other)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y] && (other[x + 1, y] || other[x - 1, y] || other[x, y + 1] || other[x, y - 1]);

            return result;
        }

        public BoolGrid GetAnd(BoolGrid other)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y] && other[x, y];

            return result;
        }

        public BoolGrid GetOr(BoolGrid other)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y] || other[x, y];

            return result;
        }

        public int Count()
        {
            int result = 0;
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    if (this[x, y])
                        result++;
            return result;
        }

        public BoolGrid GetConnected(BoolGrid connectedTo, int steps)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            Queue<Point2D> q1 = new Queue<Point2D>();
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    if (connectedTo[x, y])
                        q1.Enqueue(SC2Util.Point(x, y));

            Queue<Point2D> q2 = new Queue<Point2D>();
            for (int i = 0; i < steps; i++)
            {
                while (q1.Count > 0)
                {
                    Point2D cur = q1.Dequeue();
                    if (cur.X < 0 || cur.Y < 0 || cur.X >= Width() || cur.Y >= Height())
                        continue;
                    if (Get(cur) && !result[cur])
                    {
                        result[cur] = true;
                        q2.Enqueue(SC2Util.Point(cur.X + 1, cur.Y));
                        q2.Enqueue(SC2Util.Point(cur.X - 1, cur.Y));
                        q2.Enqueue(SC2Util.Point(cur.X, cur.Y + 1));
                        q2.Enqueue(SC2Util.Point(cur.X, cur.Y - 1));
                    }
                }
                q1 = q2;
                q2 = new Queue<Point2D>();
            }
            return result;
        }

        public List<BoolGrid> GetGroups()
        {
            List<BoolGrid> groups = new List<BoolGrid>();
            ArrayBoolGrid encountered = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    if (this[x, y] && !encountered[x, y])
                        groups.Add(GetConnected(SC2Util.Point(x, y), encountered));

            return groups;
        }

        public BoolGrid Shrink()
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 1; x < Width() - 1; x++)
                for (int y = 1; y < Height() - 1; y++)
                {
                    bool success = true;
                    for (int dx = -1; dx <= 1 && success; dx++)
                        for (int dy = -1; dy <= 1 && success; dy++)
                            success = this[x + dx, y + dy];
                    if (success)
                        result[x, y] = true;
                }

            return result;
        }

        public List<Point2D> ToList()
        {
            List<Point2D> result = new List<Point2D>();

            for (int x = 1; x < Width() - 1; x++)
                for (int y = 1; y < Height() - 1; y++)
                    if (this[x, y])
                        result.Add(SC2Util.Point(x, y));

            return result;
        }

        public BoolGrid Crop(int startX, int startY, int endX, int endY)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                {
                    if (x < startX || x >= endX || y < startY || y >= endY)
                        result[x, y] = false;
                    else result[x, y] = this[x, y];
                }
            return result;
        }
    }

    public class Gas
    {
        public Point Pos { get; set; }
        public ulong Tag { get; set; }
        public bool Available { get; set; }
        public bool CanBeGathered { get; set; }
        public Unit Unit { get; set; }
    }

    public class ImageBoolGrid : BoolGrid
    {
        private ImageData data;
        private int trueValue = Tyr.OldMapData ? 255 : 1;

        public ImageBoolGrid(ImageData data)
        {
            this.data = data;
        }

        public ImageBoolGrid(ImageData data, int trueValue)
        {
            this.data = data;
            this.trueValue = trueValue;
        }

        public override BoolGrid Clone()
        {
            return new ImageBoolGrid(data);
        }

        internal override bool GetInternal(Point2D pos)
        {
            return SC2Util.GetDataValue(data, (int)pos.X, (int)pos.Y) == trueValue;
        }

        public override int Width()
        {
            return data.Size.X;
        }

        public override int Height()
        {
            return data.Size.Y;
        }
    }

    public class WallBuilding
    {
        public uint Type;
        public Point2D Pos;

        public Point2D Size
        {
            get
            {
                return BuildingType.LookUp[Type].Size;
            }
            private set { }
        }
    }

    public class WallInCreator
    {
        public List<WallBuilding> Wall = new List<WallBuilding>();
        public void Create(List<uint> types)
        {
            foreach (uint type in types)
                Wall.Add(new WallBuilding() { Type = type });


            BoolGrid pathable = Tyr.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            BoolGrid ramp = Tyr.MapAnalyzer.Ramp;

            BoolGrid rampAdjacent = unPathable.GetAdjacent(ramp);
            BoolGrid rampSides = unPathable.GetConnected(rampAdjacent, 5);
            List<BoolGrid> sides = rampSides.GetGroups();

            List<Point2D> building1Positions = Placable(sides[0], Tyr.MapAnalyzer.StartArea, BuildingType.LookUp[types[0]].Size, true);
            List<Point2D> building2Positions = Placable(sides[1], Tyr.MapAnalyzer.StartArea, BuildingType.LookUp[types[2]].Size, true);

            float wallScore = 1000;
            foreach (Point2D p1 in building1Positions)
                foreach (Point2D p2 in building2Positions)
                {
                    if (System.Math.Abs(p1.X - p2.X) + 0.1f < (BuildingType.LookUp[types[0]].Size.X + BuildingType.LookUp[types[2]].Size.X) / 2f
                        && System.Math.Abs(p1.Y - p2.Y) + 0.1f < (BuildingType.LookUp[types[0]].Size.Y + BuildingType.LookUp[types[2]].Size.Y) / 2f)
                        continue;

                    float newScore = SC2Util.DistanceGrid(p1, p2);
                    if (newScore >= wallScore)
                        continue;

                    Wall[0].Pos = p1;
                    Wall[2].Pos = p2;
                    wallScore = newScore;
                }

            HashSet<Point2D> around1 = new HashSet<Point2D>();
            GetPlacableAround(Tyr.MapAnalyzer.StartArea, Wall[0].Pos, Wall[0].Size, Wall[1].Size, around1, true);
            HashSet<Point2D> around2 = new HashSet<Point2D>();
            GetPlacableAround(Tyr.MapAnalyzer.StartArea, Wall[2].Pos, Wall[2].Size, Wall[1].Size, around2, true);
            around1.IntersectWith(around2);

            foreach (Point2D pos in around1)
            {
                Wall[1].Pos = new Point2D() { X = pos.X + 0.5f, Y = pos.Y + 0.5f };
                break;
            }
            //DrawResult(unPathable, null, building1Positions, building2Positions);
        }

        public void CreateNatural(List<uint> types)
        {
            BoolGrid pathable = Tyr.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            Point2D naturalExit = Tyr.BaseManager.NaturalDefensePos;
            BoolGrid naturalWalls = unPathable.GetConnected(new Point2D() { X = 0, Y = 0 }).Crop((int)naturalExit.X - 12, (int)naturalExit.Y - 12, (int)naturalExit.X + 12, (int)naturalExit.Y + 12);
            List<BoolGrid> sides = naturalWalls.GetGroups();
            Dictionary<BoolGrid, int> counts = new Dictionary<BoolGrid, int>();
            foreach (BoolGrid side in sides)
                counts.Add(side, side.Count());
            sides.Sort((BoolGrid a, BoolGrid b) => counts[b] - counts[a]);

            List<Point2D> building1Positions = Placable(sides[0], Tyr.MapAnalyzer.Placement, BuildingType.LookUp[types[0]].Size, false);
            List<Point2D> building2Positions = Placable(sides[1], Tyr.MapAnalyzer.Placement, BuildingType.LookUp[types[3]].Size, false);
            int naturalHeight = Tyr.MapAnalyzer.MapHeight((int)Tyr.BaseManager.Natural.BaseLocation.Pos.X, (int)Tyr.BaseManager.Natural.BaseLocation.Pos.Y);
            building1Positions = building1Positions.FindAll((p) => Tyr.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);
            building2Positions = building2Positions.FindAll((p) => Tyr.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);


            FindWall(types, building1Positions, building2Positions, Tyr.MapAnalyzer.Placement, false);
            //DrawResult(unPathable, naturalWalls, building1Positions, building2Positions);
        }

        public void CreateFullNatural(List<uint> types)
        {
            BoolGrid pathable = Tyr.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            Point2D naturalExit = Tyr.BaseManager.NaturalDefensePos;
            BoolGrid naturalWalls = unPathable.GetConnected(new Point2D() { X = 0, Y = 0 }).Crop((int)naturalExit.X - 12, (int)naturalExit.Y - 12, (int)naturalExit.X + 12, (int)naturalExit.Y + 12);
            List<BoolGrid> sides = naturalWalls.GetGroups();
            Dictionary<BoolGrid, int> counts = new Dictionary<BoolGrid, int>();
            foreach (BoolGrid side in sides)
                counts.Add(side, side.Count());
            sides.Sort((BoolGrid a, BoolGrid b) => counts[b] - counts[a]);

            List<Point2D> building1Positions = Placable(sides[0], Tyr.MapAnalyzer.Placement, BuildingType.LookUp[types[0]].Size, false);
            List<Point2D> building2Positions = Placable(sides[1], Tyr.MapAnalyzer.Placement, BuildingType.LookUp[types[3]].Size, false);
            int naturalHeight = Tyr.MapAnalyzer.MapHeight((int)Tyr.BaseManager.Natural.BaseLocation.Pos.X, (int)Tyr.BaseManager.Natural.BaseLocation.Pos.Y);
            building1Positions = building1Positions.FindAll((p) => Tyr.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);
            building2Positions = building2Positions.FindAll((p) => Tyr.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);


            FindWall(types, building1Positions, building2Positions, Tyr.MapAnalyzer.Placement, true);
            //DrawResult(unPathable, naturalWalls, building1Positions, building2Positions);
        }

        private void FindWall(List<uint> types, List<Point2D> startPositions, List<Point2D> endPositions, BoolGrid placable, bool full)
        {
            foreach (Point2D start in startPositions)
                foreach (Point2D end in endPositions)
                {
                    if (Math.Abs(start.X - end.X) > 7
                        || Math.Abs(start.Y - end.Y) > 7)
                        continue;
                    for (int i = -1; i <= 1; i++)
                        if (CheckPlacement(types, start, end, placable, i, full))
                            return;
                }
            foreach (Point2D start in startPositions)
                foreach (Point2D end in endPositions)
                {
                    if (Math.Abs(start.X - end.X) > 7
                        || Math.Abs(start.Y - end.Y) > 7)
                        continue;
                    if (CheckPlacement(types, start, end, placable, -2, full))
                        return;
                    if (CheckPlacement(types, start, end, placable, 2, full))
                        return;
                }
        }

        private bool CheckPlacement(List<uint> types, Point2D start, Point2D end, BoolGrid placable, int i, bool full)
        {
            int spaceBetween = full ? 5 : 4;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X + i, end.Y + spaceBetween), placable, full))
                return true;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X + i, end.Y - spaceBetween), placable, full))
                return true;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X + spaceBetween, end.Y + i), placable, full))
                return true;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X - spaceBetween, end.Y + i), placable, full))
                return true;
            return false;
        }

        private bool CheckPlacement(List<uint> types, Point2D start, Point2D end, Point2D middle, BoolGrid placable, bool full)
        {
            if (full)
                return CheckPlacementFull(types, start, end, middle, placable);
            else
                return CheckPlacement(types, start, end, middle, placable);
        }

        private bool CheckPlacement(List<uint> types, Point2D start, Point2D end, Point2D middle, BoolGrid placable)
        {
            if (!CheckRect(placable, middle.X - 1, middle.Y - 1, middle.X + 1, middle.Y + 1))
                return false;

            if (Math.Abs(start.X - middle.X) == 3)
            {
                if (Math.Abs(start.Y - middle.Y) >= 3)
                    return false;
            }
            else if (Math.Abs(start.Y - middle.Y) == 3)
            {
                if (Math.Abs(start.X - middle.X) >= 3)
                    return false;
            }
            else return false;

            Point2D zealotPos = SC2Util.Point(end.X, end.Y);
            zealotPos = SC2Util.TowardCardinal(zealotPos, middle, 2);
            zealotPos = SC2Util.TowardCardinal(zealotPos, Tyr.BaseManager.Natural.BaseLocation.Pos, 0.5f);
            /*
            if (end.X - middle.X == 4)
                zealotPos.X -= 2;
            else if (end.X - middle.X == -4)
                zealotPos.X += 2;
            else if (end.Y - middle.Y == 4)
                zealotPos.Y -= 2;
            else
                zealotPos.Y += 2;
                */
            Point2D[] pylonPositions = new Point2D[2];
            Point2D natural = Tyr.BaseManager.Natural.BaseLocation.Pos;
            if (Math.Abs(natural.X - middle.X) >= Math.Abs(natural.Y - middle.Y))
            {
                if (natural.X > middle.X)
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 2.5f, middle.Y + 0.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X + 2.5f, middle.Y - 0.5f);
                }
                else
                {
                    pylonPositions[0] = SC2Util.Point(middle.X - 2.5f, middle.Y + 0.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 2.5f, middle.Y - 0.5f);
                }
                /*
                if (natural.Y > middle.Y)
                {
                    pylonPositions[2] = SC2Util.Point(middle.X + 0.5f, middle.Y + 2.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X - 0.5f, middle.Y + 2.5f);
                }
                else
                {
                    pylonPositions[2] = SC2Util.Point(middle.X + 0.5f, middle.Y - 2.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X - 0.5f, middle.Y - 2.5f);
                }
                */
            }
            else
            {
                if (natural.Y > middle.Y)
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 0.5f, middle.Y + 2.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 0.5f, middle.Y + 2.5f);
                }
                else
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 0.5f, middle.Y - 2.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 0.5f, middle.Y - 2.5f);
                }
                /*
                if (natural.X > middle.X)
                {
                    pylonPositions[2] = SC2Util.Point(middle.X + 2.5f, middle.Y + 0.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X + 2.5f, middle.Y - 0.5f);
                }
                else
                {
                    pylonPositions[2] = SC2Util.Point(middle.X - 2.5f, middle.Y + 0.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X - 2.5f, middle.Y - 0.5f);
                }
                */
            }

            Point2D pylonPos = null;
            //foreach (Point2D pos in pylonPositions)
            //{
            //    if (ProtossBuildingPlacement.IsBuildingInPowerField(start, SC2Util.Point(3, 3), pos)
            //        && ProtossBuildingPlacement.IsBuildingInPowerField(end, SC2Util.Point(3, 3), pos)
            //        && Tyr.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, start, types[0])
            //        && Tyr.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, end, types[3]))
            //        pylonPos = pos;
            //}
            if (pylonPos == null)
                return false;

            Wall = new List<WallBuilding>();
            Wall.Add(new WallBuilding() { Pos = start, Type = types[0] });
            Wall.Add(new WallBuilding() { Pos = middle, Type = types[1] });
            Wall.Add(new WallBuilding() { Pos = zealotPos, Type = types[2] });
            Wall.Add(new WallBuilding() { Pos = end, Type = types[3] });
            Wall.Add(new WallBuilding() { Pos = pylonPos, Type = UnitTypes.PYLON });

            return true;
        }

        private bool CheckPlacementFull(List<uint> types, Point2D start, Point2D end, Point2D middle, BoolGrid placable)
        {
            if (!CheckRect(placable, middle.X - 1, middle.Y - 1, middle.X + 1, middle.Y + 1))
                return false;

            if (Math.Abs(start.X - middle.X) == 3)
            {
                if (Math.Abs(start.Y - middle.Y) >= 3)
                    return false;
            }
            else if (Math.Abs(start.Y - middle.Y) == 3)
            {
                if (Math.Abs(start.X - middle.X) >= 3)
                    return false;
            }
            else return false;

            Point2D pylonPos = SC2Util.Point(end.X, end.Y);
            pylonPos = SC2Util.TowardCardinal(pylonPos, middle, 2.5f);

            Wall = new List<WallBuilding>();
            Wall.Add(new WallBuilding() { Pos = start, Type = types[0] });
            Wall.Add(new WallBuilding() { Pos = middle, Type = types[1] });
            Wall.Add(new WallBuilding() { Pos = pylonPos, Type = types[2] });
            Wall.Add(new WallBuilding() { Pos = end, Type = types[3] });

            return true;
        }

        //public void ReserveSpace()
        //{
        //    foreach (WallBuilding building in Wall)
        //        Tyr.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = building.Type, Pos = building.Pos });
        //}

        /*
    private void DrawResult(BoolGrid unPathable, BoolGrid naturalWalls, List<Point2D> building1Positions, List<Point2D> building2Positions)
    {
        if (!Tyr.Debug)
            return;

        int width = Tyr.GameInfo.StartRaw.MapSize.X;
        int height = Tyr.GameInfo.StartRaw.MapSize.Y;
        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (naturalWalls != null && naturalWalls[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Red);
                else if (unPathable[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                else if (!Tyr.MapAnalyzer.Placement[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Gray);
                else
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);
            }


        //DrawBuildings(bmp);
        for (float x = Wall[0].Pos.X - BuildingType.LookUp[Wall[0].Type].Size.X / 2f + 0.5f; x < Wall[0].Pos.X + BuildingType.LookUp[Wall[0].Type].Size.X / 2f - 0.5f + 0.1f; x++)
            for (float y = Wall[0].Pos.Y - BuildingType.LookUp[Wall[0].Type].Size.Y / 2f + 0.5f; y < Wall[0].Pos.Y + BuildingType.LookUp[Wall[0].Type].Size.Y / 2f - 0.5f + 0.1f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Green);
        for (float x = Wall[3].Pos.X - BuildingType.LookUp[Wall[3].Type].Size.X / 2f + 0.5f; x < Wall[3].Pos.X + BuildingType.LookUp[Wall[3].Type].Size.X / 2f + 0.1f - 0.5f; x++)
            for (float y = Wall[3].Pos.Y - BuildingType.LookUp[Wall[3].Type].Size.Y / 2f + 0.5f; y < Wall[3].Pos.Y + BuildingType.LookUp[Wall[3].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Yellow);
        for (float x = Wall[1].Pos.X - BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.5f; x < Wall[1].Pos.X + BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.1f - 0.5f; x++)
            for (float y = Wall[1].Pos.Y - BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.5f; y < Wall[1].Pos.Y + BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Purple);

        for (float x = Wall[4].Pos.X - BuildingType.LookUp[Wall[4].Type].Size.X / 2f + 0.5f; x < Wall[4].Pos.X + BuildingType.LookUp[Wall[4].Type].Size.X / 2f + 0.1f - 0.5f; x++)
            for (float y = Wall[4].Pos.Y - BuildingType.LookUp[Wall[4].Type].Size.Y / 2f + 0.5f; y < Wall[4].Pos.Y + BuildingType.LookUp[Wall[4].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Cyan);
        bmp.SetPixel((int)Wall[2].Pos.X, height - 1 - (int)Wall[2].Pos.Y, System.Drawing.Color.Blue);

        foreach (Point2D pos in building1Positions)
            bmp.SetPixel((int)pos.X, height - 1 - (int)pos.Y, System.Drawing.Color.Blue);
        foreach (Point2D pos in building2Positions)
            bmp.SetPixel((int)pos.X, height - 1 - (int)pos.Y, System.Drawing.Color.Green);

        int width = Tyr.GameInfo.StartRaw.MapSize.X;
        int height = Tyr.GameInfo.StartRaw.MapSize.Y;
        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (Tyr.MapAnalyzer.StartArea[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Green);
                else if (Tyr.MapAnalyzer.Ramp[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Blue);
                else
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
            }

        for (float x = Wall[0].Pos.X - BuildingType.LookUp[Wall[0].Type].Size.X / 2f + 0.5f; x < Wall[0].Pos.X + BuildingType.LookUp[Wall[0].Type].Size.X / 2f - 0.5f + 0.1f; x++)
            for (float y = Wall[0].Pos.Y - BuildingType.LookUp[Wall[0].Type].Size.Y / 2f + 0.5f; y < Wall[0].Pos.Y + BuildingType.LookUp[Wall[0].Type].Size.Y / 2f - 0.5f + 0.1f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Red);
        for (float x = Wall[2].Pos.X - BuildingType.LookUp[Wall[2].Type].Size.X / 2f + 0.5f; x < Wall[2].Pos.X + BuildingType.LookUp[Wall[2].Type].Size.X / 2f + 0.1f - 0.5f; x++)
            for (float y = Wall[2].Pos.Y - BuildingType.LookUp[Wall[2].Type].Size.Y / 2f + 0.5f; y < Wall[2].Pos.Y + BuildingType.LookUp[Wall[2].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Yellow);
        for (float x = Wall[1].Pos.X - BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.5f; x < Wall[1].Pos.X + BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.1f - 0.5f; x++)
            for (float y = Wall[1].Pos.Y - BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.5f; y < Wall[1].Pos.Y + BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Purple);

        bmp.Save(@"C:\Users\Simon\Desktop\WallPlacement.png");
    }
*/

        /*
        private void DrawBuildings(System.Drawing.Bitmap bmp)
        {
            int i = 0;
            System.Drawing.Color[] colors = new System.Drawing.Color[] { System.Drawing.Color.Blue, System.Drawing.Color.Green, System.Drawing.Color.Yellow, System.Drawing.Color.Cyan, System.Drawing.Color.Magenta };
            foreach (WallBuilding building in Wall)
            {
                DrawBuilding(bmp, building, colors[i % colors.Length]);
                i++;
            }
        }

        private void DrawBuilding(System.Drawing.Bitmap bmp, WallBuilding building, System.Drawing.Color color)
        {
            if (!BuildingType.LookUp.ContainsKey(building.Type))
            {
                bmp.SetPixel((int)building.Pos.X, bmp.Height - 1 - (int)building.Pos.Y, color);
                return;
            }
            BuildingType type = BuildingType.LookUp[building.Type];
            for (float x = building.Pos.X - type.Size.X / 2f + 0.5f; x < building.Pos.X + type.Size.X / 2f - 0.5f + 0.1f; x++)
                for (float y = building.Pos.Y - type.Size.Y / 2f + 0.5f; y < building.Pos.Y + type.Size.Y / 2f - 0.5f + 0.1f; y++)
                    bmp.SetPixel((int)x, bmp.Height - 1 - (int)y, color);

        }
        */

        private List<Point2D> Placable(BoolGrid around, BoolGrid startArea, Point2D size, bool allowCorners)
        {
            Point2D size1x1 = new Point2D() { X = 1, Y = 1 };
            List<Point2D> result = new List<Point2D>();

            for (int x = 0; x < around.Width(); x++)
                for (int y = 0; y < around.Height(); y++)
                    if (around[x, y])
                        GetPlacableAround(startArea, new Point2D() { X = x, Y = y }, size1x1, size, result, allowCorners);

            return result;
        }

        private void GetPlacableAround(BoolGrid startArea, Point2D pos, Point2D size1, Point2D size2, ICollection<Point2D> result, bool allowCorners)
        {
            float xOffset = (size1.X + size2.X) / 2f;
            float yOffset = (size1.Y + size2.Y) / 2f;
            for (float i = -xOffset + (allowCorners ? 0 : 1); i < 0.1f + xOffset - (allowCorners ? 0 : 1); i++)
            {
                Point2D checkPos = new Point2D() { X = pos.X + i, Y = pos.Y - yOffset };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
                checkPos = new Point2D() { X = pos.X + i, Y = pos.Y + yOffset };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
            }
            for (float i = -yOffset + (allowCorners ? 0 : 1); i < 0.1f + yOffset - (allowCorners ? 0 : 1); i++)
            {
                Point2D checkPos = new Point2D() { X = pos.X + xOffset, Y = pos.Y + i };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
                checkPos = new Point2D() { X = pos.X - xOffset, Y = pos.Y + i };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
            }
        }

        private bool CheckRect(BoolGrid grid, Point2D pos, Point2D size)
        {
            return CheckRect(grid,
                pos.X - size.X / 2f + 0.5f,
                pos.Y - size.Y / 2f + 0.5f,
                pos.X + size.X / 2f - 0.5f,
                pos.Y + size.Y / 2f - 0.5f);
        }

        private bool CheckRect(BoolGrid grid, float minX, float minY, float maxX, float maxY)
        {
            for (float x = minX; x < maxX + 0.1f; x++)
                for (float y = minY; y < maxY + 0.1f; y++)
                    if (!grid[(int)x, (int)y])
                        return false;
            return true;
        }
    }

    public abstract class SC2Util
    {
        public static int GetDataValue(ImageData data, int x, int y)
        {
            if (Tyr.OldMapData)
                return GetDataValueOld(data, x, y);

            if (data.BitsPerPixel == 1)
                return GetDataValueBit(data, x, y);

            return GetDataValueByte(data, x, y);
        }
        public static int GetDataValueBit(ImageData data, int x, int y)
        {
            int pixelID = x + y * data.Size.X;
            int byteLocation = pixelID / 8;
            int bitLocation = pixelID % 8;
            return ((data.Data[byteLocation] & 1 << (7 - bitLocation)) == 0) ? 0 : 1;
        }
        public static int GetDataValueByte(ImageData data, int x, int y)
        {
            int pixelID = x + y * data.Size.X;
            return data.Data[pixelID];
        }
        public static int GetDataValueOld(ImageData data, int x, int y)
        {
            int pixelID = x + (data.Size.Y - 1 - y) * data.Size.X;
            return data.Data[pixelID];
        }

        public static bool GetTilePlacable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Tyr.GameInfo.StartRaw.PlacementGrid.Size.X || y >= Tyr.GameInfo.StartRaw.PlacementGrid.Size.Y)
                return false;
            return SC2Util.GetDataValue(Tyr.GameInfo.StartRaw.PlacementGrid, x, y) != 0;
        }

        public static Point2D Point(float x, float y)
        {
            Point2D result = new Point2D
            {
                X = x,
                Y = y
            };
            return result;
        }

        public static Point Point(float x, float y, float z)
        {
            Point result = new Point
            {
                X = x,
                Y = y,
                Z = z
            };
            return result;
        }

        public static float DistanceSq(Point pos1, Point2D pos2)
        {
            return DistanceSq(To2D(pos1), pos2);
        }

        public static float DistanceSq(Point pos1, Point pos2)
        {
            return DistanceSq(To2D(pos1), To2D(pos2));
        }

        public static float DistanceSq(Point2D pos1, Point pos2)
        {
            return DistanceSq(pos1, To2D(pos2));
        }

        public static float DistanceSq(Point2D pos1, Point2D pos2)
        {
            return (pos1.X - pos2.X) * (pos1.X - pos2.X) + (pos1.Y - pos2.Y) * (pos1.Y - pos2.Y);
        }

        public static float DistanceGrid(Point pos1, Point pos2)
        {
            return DistanceGrid(To2D(pos1), To2D(pos2));
        }

        public static float DistanceGrid(Point pos1, Point2D pos2)
        {
            return DistanceGrid(To2D(pos1), pos2);
        }

        public static float DistanceGrid(Point2D pos1, Point pos2)
        {
            return DistanceGrid(pos1, To2D(pos2));
        }

        public static float DistanceGrid(Point2D pos1, Point2D pos2)
        {
            return Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);
        }

        public static Point2D To2D(Point pos)
        {
            return Point(pos.X, pos.Y);
        }

        public static Point To3D(Point2D pos)
        {
            return Point(pos.X, pos.Y, Tyr.MapAnalyzer.MapHeight((int)pos.X, (int)pos.Y));
        }

        public static Point2D Normalize(Point2D point)
        {
            float length = (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
            return Point(point.X / length, point.Y / length);
        }

        public static Point2D TowardCardinal(Point2D pos1, Point2D pos2, float distance)
        {
            if (Math.Abs(pos2.X - pos1.X) >= Math.Abs(pos2.Y - pos1.Y))
            {
                if (pos2.X > pos1.X)
                    return Point(pos1.X + distance, pos1.Y);
                else
                    return Point(pos1.X - distance, pos1.Y);
            }
            else
            {
                if (pos2.Y > pos1.Y)
                    return Point(pos1.X, pos1.Y + distance);
                else
                    return Point(pos1.X, pos1.Y - distance);
            }
        }

        public static bool IsVersionBefore(string version)
        {
            return false;
        }
    }

    class UnitTypes
    {
        public static Dictionary<uint, UnitTypeData> LookUp = new Dictionary<uint, UnitTypeData>();
        public static uint COLOSUS = 4;
        public static uint TECH_LAB = 5;
        public static uint REACTOR = 6;
        public static uint INFESTOR_TERRAN = 7;
        public static uint BANELING_COCOON = 8;
        public static uint BANELING = 9;
        public static uint MOTHERSHIP = 10;
        public static uint POINT_DEFENSE_DRONE = 11;
        public static uint CHANGELING = 12;
        public static uint CHANGELING_ZEALOT = 13;
        public static uint CHANGELING_MARINE_SHIELD = 14;
        public static uint CHANGELING_MARINE = 15;
        public static uint CHANGELING_ZERGLING_WINGS = 16;
        public static uint CHANGELING_ZERGLING = 17;
        public static uint COMMAND_CENTER = 18;
        public static uint SUPPLY_DEPOT = 19;
        public static uint REFINERY = 20;
        public static uint BARRACKS = 21;
        public static uint ENGINEERING_BAY = 22;
        public static uint MISSILE_TURRET = 23;
        public static uint BUNKER = 24;
        public static uint SENSOR_TOWER = 25;
        public static uint GHOST_ACADEMY = 26;
        public static uint FACTORY = 27;
        public static uint STARPORT = 28;
        public static uint ARMORY = 29;
        public static uint FUSION_CORE = 30;
        public static uint AUTO_TURRET = 31;
        public static uint SIEGE_TANK_SIEGED = 32;
        public static uint SIEGE_TANK = 33;
        public static uint VIKING_ASSUALT = 34;
        public static uint VIKING_FIGHTER = 35;
        public static uint COMMAND_CENTER_FLYING = 36;
        public static uint BARRACKS_TECH_LAB = 37;
        public static uint BARRACKS_REACTOR = 38;
        public static uint FACTORY_TECH_LAB = 39;
        public static uint FACTORY_REACTOR = 40;
        public static uint STARPORT_TECH_LAB = 41;
        public static uint STARPORT_REACTOR = 42;
        public static uint FACTORY_FLYING = 43;
        public static uint STARPORT_FLYING = 44;
        public static uint SCV = 45;
        public static uint BARRACKS_FLYING = 46;
        public static uint SUPPLY_DEPOT_LOWERED = 47;
        public static uint MARINE = 48;
        public static uint REAPER = 49;
        public static uint GHOST = 50;
        public static uint MARAUDER = 51;
        public static uint THOR = 52;
        public static uint HELLION = 53;
        public static uint MEDIVAC = 54;
        public static uint BANSHEE = 55;
        public static uint RAVEN = 56;
        public static uint BATTLECRUISER = 57;
        public static uint NUKE = 58;
        public static uint NEXUS = 59;
        public static uint PYLON = 60;
        public static uint ASSIMILATOR = 61;
        public static uint GATEWAY = 62;
        public static uint FORGE = 63;
        public static uint FLEET_BEACON = 64;
        public static uint TWILIGHT_COUNSEL = 65;
        public static uint PHOTON_CANNON = 66;
        public static uint STARGATE = 67;
        public static uint TEMPLAR_ARCHIVE = 68;
        public static uint DARK_SHRINE = 69;
        public static uint ROBOTICS_BAY = 70;
        public static uint ROBOTICS_FACILITY = 71;
        public static uint CYBERNETICS_CORE = 72;
        public static uint ZEALOT = 73;
        public static uint STALKER = 74;
        public static uint HIGH_TEMPLAR = 75;
        public static uint DARK_TEMPLAR = 76;
        public static uint SENTRY = 77;
        public static uint PHOENIX = 78;
        public static uint CARRIER = 79;
        public static uint VOID_RAY = 80;
        public static uint WARP_PRISM = 81;
        public static uint OBSERVER = 82;
        public static uint IMMORTAL = 83;
        public static uint PROBE = 84;
        public static uint INTERCEPTOR = 85;
        public static uint HATCHERY = 86;
        public static uint CREEP_TUMOR = 87;
        public static uint EXTRACTOR = 88;
        public static uint SPAWNING_POOL = 89;
        public static uint EVOLUTION_CHAMBER = 90;
        public static uint HYDRALISK_DEN = 91;
        public static uint SPIRE = 92;
        public static uint ULTRALISK_CAVERN = 93;
        public static uint INFESTATION_PIT = 94;
        public static uint NYDUS_NETWORK = 95;
        public static uint BANELING_NEST = 96;
        public static uint ROACH_WARREN = 97;
        public static uint SPINE_CRAWLER = 98;
        public static uint SPORE_CRAWLER = 99;
        public static uint LAIR = 100;
        public static uint HIVE = 101;
        public static uint GREATER_SPIRE = 102;
        public static uint EGG = 103;
        public static uint DRONE = 104;
        public static uint ZERGLING = 105;
        public static uint OVERLORD = 106;
        public static uint HYDRALISK = 107;
        public static uint MUTALISK = 108;
        public static uint ULTRALISK = 109;
        public static uint ROACH = 110;
        public static uint INFESTOR = 111;
        public static uint CORRUPTOR = 112;
        public static uint BROOD_LORD_COCOON = 113;
        public static uint BROOD_LORD = 114;
        public static uint BANELING_BURROWED = 115;
        public static uint DRONE_BURROWED = 116;
        public static uint HYDRALISK_BURROWED = 117;
        public static uint ROACH_BURROWED = 118;
        public static uint ZERGLING_BURROWED = 119;
        public static uint INFESTOR_TERRAN_BURROWED = 120;
        public static uint QUEEN_BURROWED = 125;
        public static uint QUEEN = 126;
        public static uint INFESTOR_BURROWED = 127;
        public static uint OVERLORD_COCOON = 128;
        public static uint OVERSEER = 129;
        public static uint PLANETARY_FORTRESS = 130;
        public static uint ULTRALISK_BURROWED = 131;
        public static uint ORBITAL_COMMAND = 132;
        public static uint WARP_GATE = 133;
        public static uint ORBITAL_COMMAND_FLYING = 134;
        public static uint FORCE_FIELD = 135;
        public static uint WARP_PRISM_PHASING = 136;
        public static uint CREEP_TUMOR_BURROWED = 137;
        public static uint CREEP_TUMOR_QUEEN = 138;
        public static uint SPINE_CRAWLER_UPROOTED = 139;
        public static uint SPORE_CRAWLER_UPROOTED = 140;
        public static uint ARCHON = 141;
        public static uint NYDUS_CANAL = 142;
        public static uint BROODLING_ESCORT = 143;
        public static uint RICH_MINERAL_FIELD = 146;
        public static uint RICH_MINERAL_FIELD_750 = 147;
        public static uint URSADON = 148;
        public static uint XEL_NAGA_TOWER = 149;
        public static uint INFESTED_TERRANS_EGG = 150;
        public static uint LARVA = 151;
        public static uint BROODLING = 289;
        public static uint ADEPT = 311;
        public static uint MINERAL_FIELD = 341;
        public static uint VESPENE_GEYSER = 342;
        public static uint SPACE_PLATFORM_GEYSER = 343;
        public static uint RICH_VESPENE_GEYSER = 344;
        public static uint MINERAL_FIELD_750 = 483;
        public static uint HELLBAT = 484;
        public static uint SWARM_HOST = 494;
        public static uint ORACLE = 495;
        public static uint TEMPEST = 496;
        public static uint WIDOW_MINE = 498;
        public static uint VIPER = 499;
        public static uint WIDOW_MINE_BURROWED = 500;
        public static uint LURKER = 502;
        public static uint LURKER_BURROWED = 503;
        public static uint PROTOSS_VESPENE_GEYSER = 608;
        public static uint DESTRUCTIBLE_ROCKS6X6 = 639;
        public static uint LAB_MINERAL_FIELD = 665;
        public static uint LAB_MINERAL_FIELD_750 = 666;
        public static uint RAVAGER = 688;
        public static uint LIBERATOR = 689;
        public static uint RAVAGER_BURROWED = 690;
        public static uint THOR_SINGLE_TARGET = 691;
        public static uint CYCLONE = 692;
        public static uint LOCUST_FLYING = 693;
        public static uint DISRUPTOR = 694;
        public static uint DISRUPTOR_PHASED = 733;
        public static uint LIBERATOR_AG = 734;
        public static uint PURIFIER_RICH_MINERAL_FIELD = 796;
        public static uint PURIFIER_RICH_MINERAL_FIELD_750 = 797;
        public static uint ADEPT_PHASE_SHIFT = 801;
        public static uint KD8_CHARGE = 830;
        public static uint PURIFIER_VESPENE_GEYSER = 880;
        public static uint SHAKURAS_VESPENE_GEYSER = 881;
        public static uint PURIFIER_MINERAL_FIELD = 884;
        public static uint PURIFIER_MINERAL_FIELD_750 = 885;
        public static uint BATTLE_STATION_MINERAL_FIELD = 886;
        public static uint BATTLE_STATION_MINERAL_FIELD_750 = 887;
        public static uint LURKER_DEN = 504;
        public static uint SHIELD_BATTERY = 1910;

        public static void LoadData(ResponseData data)
        {
            foreach (UnitTypeData unitType in data.Units)
            {
                LookUp.Add(unitType.UnitId, unitType);
                if (unitType.AbilityId != 0)
                    Abilities.Creates.Add(unitType.AbilityId, unitType.UnitId);
            }
        }

        public static HashSet<uint> BuildingTypes = new HashSet<uint>
            {
                ARMORY,
                ASSIMILATOR,
                BANELING_NEST,
                BARRACKS,
                BARRACKS_FLYING,
                BARRACKS_REACTOR,
                BARRACKS_TECH_LAB,
                BUNKER,
                COMMAND_CENTER,
                COMMAND_CENTER_FLYING,
                CYBERNETICS_CORE,
                DARK_SHRINE,
                ENGINEERING_BAY,
                EVOLUTION_CHAMBER,
                EXTRACTOR,
                FACTORY,
                FACTORY_FLYING,
                FACTORY_REACTOR,
                FACTORY_TECH_LAB,
                FLEET_BEACON,
                FORGE,
                FUSION_CORE,
                GATEWAY,
                GHOST_ACADEMY,
                GREATER_SPIRE,
                HATCHERY,
                HIVE,
                HYDRALISK_DEN,
                INFESTATION_PIT,
                LAIR,
                MISSILE_TURRET,
                NEXUS,
                NYDUS_NETWORK,
                ORBITAL_COMMAND,
                ORBITAL_COMMAND_FLYING,
                PHOTON_CANNON,
                PLANETARY_FORTRESS,
                PYLON,
                REACTOR,
                REFINERY,
                ROACH_WARREN,
                ROBOTICS_BAY,
                ROBOTICS_FACILITY,
                SENSOR_TOWER,
                SPAWNING_POOL,
                SPINE_CRAWLER,
                SPINE_CRAWLER_UPROOTED,
                SPIRE,
                SPORE_CRAWLER,
                SPORE_CRAWLER_UPROOTED,
                STARPORT,
                STARGATE,
                STARPORT_FLYING,
                STARPORT_REACTOR,
                STARPORT_TECH_LAB,
                SUPPLY_DEPOT,
                SUPPLY_DEPOT_LOWERED,
                TECH_LAB,
                TEMPLAR_ARCHIVE,
                TWILIGHT_COUNSEL,
                ULTRALISK_CAVERN,
                WARP_GATE,
                SHIELD_BATTERY,
                LURKER_DEN
            };
        public static HashSet<uint> ProductionStructures = new HashSet<uint>
            {
                ARMORY,
                BANELING_NEST,
                BARRACKS,
                BARRACKS_TECH_LAB,
                COMMAND_CENTER,
                CYBERNETICS_CORE,
                ENGINEERING_BAY,
                EVOLUTION_CHAMBER,
                FACTORY,
                FACTORY_TECH_LAB,
                FLEET_BEACON,
                FORGE,
                FUSION_CORE,
                GATEWAY,
                GHOST_ACADEMY,
                GREATER_SPIRE,
                HATCHERY,
                HIVE,
                HYDRALISK_DEN,
                INFESTATION_PIT,
                LAIR,
                NEXUS,
                NYDUS_NETWORK,
                ORBITAL_COMMAND,
                PLANETARY_FORTRESS,
                ROACH_WARREN,
                ROBOTICS_BAY,
                ROBOTICS_FACILITY,
                SPAWNING_POOL,
                SPIRE,
                STARPORT,
                STARGATE,
                STARPORT_TECH_LAB,
                TECH_LAB,
                TEMPLAR_ARCHIVE,
                TWILIGHT_COUNSEL,
                ULTRALISK_CAVERN,
                WARP_GATE,
                LURKER_DEN
            };
        public static HashSet<uint> CombatUnitTypes = new HashSet<uint>
            {
                ARCHON,
                AUTO_TURRET,
                BANELING,
                BANELING_BURROWED,
                BANELING_COCOON,
                BANSHEE,
                BATTLECRUISER,
                BROOD_LORD,
                BROOD_LORD_COCOON,
                CARRIER,
                COLOSUS,
                CORRUPTOR,
                DARK_TEMPLAR,
                GHOST,
                HELLION,
                HIGH_TEMPLAR,
                HYDRALISK,
                HYDRALISK_BURROWED,
                IMMORTAL,
                INFESTOR,
                INFESTED_TERRANS_EGG,
                INFESTOR_BURROWED,
                INFESTOR_TERRAN,
                INFESTOR_TERRAN_BURROWED,
                MARAUDER,
                MARINE,
                MEDIVAC,
                MOTHERSHIP,
                MUTALISK,
                PHOENIX,
                QUEEN,
                QUEEN_BURROWED,
                RAVEN,
                REAPER,
                ROACH,
                ROACH_BURROWED,
                SENTRY,
                SIEGE_TANK,
                SIEGE_TANK_SIEGED,
                STALKER,
                THOR,
                THOR_SINGLE_TARGET,
                ULTRALISK,
                URSADON,
                VIKING_ASSUALT,
                VIKING_FIGHTER,
                VOID_RAY,
                ZEALOT,
                ZERGLING,
                ZERGLING_BURROWED,
                ORACLE,
                TEMPEST,
                ADEPT,
                RAVAGER,
                RAVAGER_BURROWED,
                LURKER,
                LURKER_BURROWED,
                HELLBAT,
                LIBERATOR,
                LIBERATOR_AG,
                CYCLONE,
                WIDOW_MINE,
                WIDOW_MINE_BURROWED,
                SWARM_HOST,
                DISRUPTOR
            };
        public static HashSet<uint> AirAttackTypes = new HashSet<uint>
            {
                ARCHON,
                AUTO_TURRET,
                BATTLECRUISER,
                CARRIER,
                CORRUPTOR,
                GHOST,
                HIGH_TEMPLAR,
                HYDRALISK,
                INFESTOR,
                INFESTOR_TERRAN,
                MARINE,
                MOTHERSHIP,
                MUTALISK,
                PHOENIX,
                QUEEN,
                SENTRY,
                STALKER,
                THOR,
                THOR_SINGLE_TARGET,
                VIKING_FIGHTER,
                VOID_RAY,
                PHOTON_CANNON,
                MISSILE_TURRET,
                SPORE_CRAWLER,
                BUNKER,
                LIBERATOR,
                TEMPEST,
                WIDOW_MINE,
                WIDOW_MINE_BURROWED
            };
        public static HashSet<uint> RangedTypes = new HashSet<uint>
        {
                ARCHON,
                AUTO_TURRET,
                BANSHEE,
                BATTLECRUISER,
                BROOD_LORD,
                CARRIER,
                COLOSUS,
                CORRUPTOR,
                GHOST,
                HELLION,
                HIGH_TEMPLAR,
                IMMORTAL,
                INFESTOR_TERRAN,
                MARAUDER,
                MARINE,
                MEDIVAC,
                MOTHERSHIP,
                MUTALISK,
                PHOENIX,
                QUEEN,
                REAPER,
                ROACH,
                SENTRY,
                SIEGE_TANK,
                SIEGE_TANK_SIEGED,
                STALKER,
                THOR,
                THOR_SINGLE_TARGET,
                VIKING_ASSUALT,
                VIKING_FIGHTER,
                VOID_RAY,
                ORACLE,
                ADEPT,
                RAVAGER,
                LURKER_BURROWED,
                HELLBAT,
                LIBERATOR,
                LIBERATOR_AG,
                HYDRALISK,
                TEMPEST,
                CYCLONE,
                WIDOW_MINE,
                WIDOW_MINE_BURROWED,
                DISRUPTOR
        };

        public static HashSet<uint> ResourceCenters = new HashSet<uint>
            {
                COMMAND_CENTER,
                COMMAND_CENTER_FLYING,
                HATCHERY,
                LAIR,
                HIVE,
                NEXUS,
                ORBITAL_COMMAND,
                ORBITAL_COMMAND_FLYING,
                PLANETARY_FORTRESS
        };
        public static HashSet<uint> MineralFields = new HashSet<uint>
            {
                RICH_MINERAL_FIELD,
                RICH_MINERAL_FIELD_750,
                MINERAL_FIELD,
                MINERAL_FIELD_750,
                LAB_MINERAL_FIELD,
                LAB_MINERAL_FIELD_750,
                PURIFIER_RICH_MINERAL_FIELD,
                PURIFIER_RICH_MINERAL_FIELD_750,
                PURIFIER_MINERAL_FIELD,
                PURIFIER_MINERAL_FIELD_750,
                BATTLE_STATION_MINERAL_FIELD,
                BATTLE_STATION_MINERAL_FIELD_750
        };
        public static HashSet<uint> GasGeysers = new HashSet<uint>
            {
                VESPENE_GEYSER,
                SPACE_PLATFORM_GEYSER,
                RICH_VESPENE_GEYSER,
                PROTOSS_VESPENE_GEYSER,
                PURIFIER_VESPENE_GEYSER,
                SHAKURAS_VESPENE_GEYSER,
                EXTRACTOR,
                ASSIMILATOR,
                REFINERY

        };
        public static HashSet<uint> WorkerTypes = new HashSet<uint>
            {
                SCV,
                PROBE,
                DRONE
        };
        public static HashSet<uint> ChangelingTypes = new HashSet<uint>
            {
                CHANGELING,
                CHANGELING_MARINE,
                CHANGELING_MARINE_SHIELD,
                CHANGELING_ZEALOT,
                CHANGELING_ZERGLING,
                CHANGELING_ZERGLING_WINGS
        };
        public static HashSet<uint> DefensiveBuildingsTypes = new HashSet<uint>
            {
                MISSILE_TURRET,
                BUNKER,
                PLANETARY_FORTRESS,
                PHOTON_CANNON,
                SHIELD_BATTERY,
                SPORE_CRAWLER,
                SPORE_CRAWLER_UPROOTED,
                SPINE_CRAWLER,
                SPINE_CRAWLER_UPROOTED
        };

        public static Dictionary<uint, List<uint>> EquivalentTypes = new Dictionary<uint, List<uint>>() {
            { LURKER_BURROWED, new List<uint>() { LURKER } },
            { GREATER_SPIRE, new List<uint>() { SPIRE }},
            { HIVE, new List<uint>() { LAIR, HATCHERY}},
            { LAIR, new List<uint>() { HATCHERY }},
            { SUPPLY_DEPOT_LOWERED, new List<uint>() { SUPPLY_DEPOT }},
            { ORBITAL_COMMAND, new List<uint>() { COMMAND_CENTER }},
            { PLANETARY_FORTRESS, new List<uint>() { COMMAND_CENTER }},
            { LIBERATOR_AG, new List<uint>() { LIBERATOR }},
            { SIEGE_TANK_SIEGED, new List<uint>() { SIEGE_TANK }},
            { WIDOW_MINE_BURROWED, new List<uint>() { WIDOW_MINE }},
            { THOR_SINGLE_TARGET, new List<uint>() { THOR }},
            { WARP_GATE, new List<uint>() { GATEWAY }}
        };

        public static bool CanAttackGround(uint type)
        {
            if (type == LIBERATOR
                || type == CARRIER
                || type == WIDOW_MINE
                || type == WIDOW_MINE_BURROWED
                || type == SWARM_HOST
                || type == SPINE_CRAWLER
                || type == BATTLECRUISER
                || type == INFESTOR
                || type == DISRUPTOR
                || type == ORACLE
                || type == PHOENIX)
                return true;
            foreach (Weapon weapon in LookUp[type].Weapons)
                if (weapon.Type == Weapon.Types.TargetType.Any
                    || (weapon.Type == Weapon.Types.TargetType.Ground))
                    return true;
            return false;
        }

        public static bool CanAttackAir(uint type)
        {
            if (type == CARRIER
                || type == WIDOW_MINE
                || type == WIDOW_MINE_BURROWED
                || type == CYCLONE
                || type == INFESTOR
                || type == BATTLECRUISER)
                return true;
            foreach (Weapon weapon in LookUp[type].Weapons)
                if (weapon.Type == Weapon.Types.TargetType.Any
                    || (weapon.Type == Weapon.Types.TargetType.Air))
                    return true;
            return false;
        }
    }

    public class BuildingType
    {
        public uint Type { get; private set; }
        public int Ability { get; private set; }
        public Point2D Size { get; private set; }
        public string Name { get; private set; }
        public int Minerals { get; private set; }
        public int Gas { get; private set; }

        public static Dictionary<uint, BuildingType> LookUp = createLookUp();
        private static Dictionary<uint, BuildingType> createLookUp()
        {
            Dictionary<uint, BuildingType> lookUp = new Dictionary<uint, BuildingType>();

            lookUp.Add(5, new BuildingType() { Type = 5, Ability = 0, Size = SC2Util.Point(2, 2), Name = "TechLab", Minerals = 50, Gas = 25 });
            lookUp.Add(6, new BuildingType() { Type = 6, Ability = 0, Size = SC2Util.Point(2, 2), Name = "Reactor", Minerals = 50, Gas = 50 });
            lookUp.Add(18, new BuildingType() { Type = 18, Ability = 318, Size = SC2Util.Point(5, 5), Name = "CommandCenter", Minerals = 400 });
            lookUp.Add(19, new BuildingType() { Type = 19, Ability = 319, Size = SC2Util.Point(2, 2), Name = "SupplyDepot", Minerals = 100 });
            lookUp.Add(20, new BuildingType() { Type = 20, Ability = 320, Size = SC2Util.Point(3, 3), Name = "Refinery", Minerals = 75 });
            lookUp.Add(21, new BuildingType() { Type = 21, Ability = 321, Size = SC2Util.Point(3, 3), Name = "Barracks", Minerals = 150 });
            lookUp.Add(22, new BuildingType() { Type = 22, Ability = 322, Size = SC2Util.Point(3, 3), Name = "EngineeringBay", Minerals = 125 });
            lookUp.Add(23, new BuildingType() { Type = 23, Ability = 323, Size = SC2Util.Point(2, 2), Name = "MissileTurret", Minerals = 100 });
            lookUp.Add(24, new BuildingType() { Type = 24, Ability = 324, Size = SC2Util.Point(3, 3), Name = "Bunker", Minerals = 100 });
            lookUp.Add(25, new BuildingType() { Type = 25, Ability = 326, Size = SC2Util.Point(2, 2), Name = "SensorTower", Minerals = 125, Gas = 100 });
            lookUp.Add(27, new BuildingType() { Type = 27, Ability = 328, Size = SC2Util.Point(3, 3), Name = "Factory", Minerals = 150, Gas = 100 });
            lookUp.Add(28, new BuildingType() { Type = 28, Ability = 329, Size = SC2Util.Point(3, 3), Name = "Starport", Minerals = 150, Gas = 100 });
            lookUp.Add(29, new BuildingType() { Type = 29, Ability = 331, Size = SC2Util.Point(3, 3), Name = "Armory", Minerals = 150, Gas = 100 });
            lookUp.Add(30, new BuildingType() { Type = 30, Ability = 333, Size = SC2Util.Point(3, 3), Name = "FusionCore", Minerals = 150, Gas = 150 });
            lookUp.Add(59, new BuildingType() { Type = 59, Ability = 880, Size = SC2Util.Point(5, 5), Name = "Nexus", Minerals = 400 });
            lookUp.Add(60, new BuildingType() { Type = 60, Ability = 881, Size = SC2Util.Point(2, 2), Name = "Pylon", Minerals = 100 });
            lookUp.Add(61, new BuildingType() { Type = 61, Ability = 882, Size = SC2Util.Point(3, 3), Name = "Assimilator", Minerals = 75 });
            lookUp.Add(62, new BuildingType() { Type = 62, Ability = 883, Size = SC2Util.Point(3, 3), Name = "Gateway", Minerals = 150 });
            lookUp.Add(63, new BuildingType() { Type = 63, Ability = 884, Size = SC2Util.Point(3, 3), Name = "Forge", Minerals = 150 });
            lookUp.Add(64, new BuildingType() { Type = 64, Ability = 885, Size = SC2Util.Point(3, 3), Name = "FleetBeacon", Minerals = 300, Gas = 200 });
            lookUp.Add(65, new BuildingType() { Type = 65, Ability = 886, Size = SC2Util.Point(3, 3), Name = "TwilightCounsel", Minerals = 150, Gas = 100 });
            lookUp.Add(66, new BuildingType() { Type = 66, Ability = 887, Size = SC2Util.Point(2, 2), Name = "PhotonCannon", Minerals = 150 });
            lookUp.Add(67, new BuildingType() { Type = 67, Ability = 889, Size = SC2Util.Point(3, 3), Name = "Stargate", Minerals = 150, Gas = 150 });
            lookUp.Add(68, new BuildingType() { Type = 68, Ability = 890, Size = SC2Util.Point(3, 3), Name = "TemplarArchives", Minerals = 150, Gas = 200 });
            lookUp.Add(69, new BuildingType() { Type = 69, Ability = 891, Size = SC2Util.Point(2, 2), Name = "DarkShrine", Minerals = 150, Gas = 150 });
            lookUp.Add(70, new BuildingType() { Type = 70, Ability = 892, Size = SC2Util.Point(3, 3), Name = "RoboticsBay", Minerals = 200, Gas = 200 });
            lookUp.Add(71, new BuildingType() { Type = 71, Ability = 893, Size = SC2Util.Point(3, 3), Name = "RoboticsFacility", Minerals = 150, Gas = 100 });
            lookUp.Add(72, new BuildingType() { Type = 72, Ability = 894, Size = SC2Util.Point(3, 3), Name = "CyberneticsCore", Minerals = 150 });
            lookUp.Add(86, new BuildingType() { Type = 86, Ability = 1152, Size = SC2Util.Point(5, 5), Name = "Hatchery", Minerals = 300 });
            lookUp.Add(87, new BuildingType() { Type = 87, Ability = 1694, Size = SC2Util.Point(1, 1), Name = "CreepTumor" });
            lookUp.Add(88, new BuildingType() { Type = 88, Ability = 1154, Size = SC2Util.Point(3, 3), Name = "Extractor", Minerals = 25 });
            lookUp.Add(89, new BuildingType() { Type = 89, Ability = 1155, Size = SC2Util.Point(3, 3), Name = "SpawningPool", Minerals = 200 });
            lookUp.Add(90, new BuildingType() { Type = 90, Ability = 1156, Size = SC2Util.Point(3, 3), Name = "EvolutionChamber", Minerals = 75 });
            lookUp.Add(91, new BuildingType() { Type = 91, Ability = 1157, Size = SC2Util.Point(3, 3), Name = "HydraliskDen", Minerals = 100, Gas = 100 });
            lookUp.Add(92, new BuildingType() { Type = 92, Ability = 1158, Size = SC2Util.Point(3, 3), Name = "Spire", Minerals = 200, Gas = 200 });
            lookUp.Add(93, new BuildingType() { Type = 93, Ability = 1159, Size = SC2Util.Point(3, 3), Name = "UltraliskCavern", Minerals = 150, Gas = 200 });
            lookUp.Add(94, new BuildingType() { Type = 94, Ability = 1160, Size = SC2Util.Point(3, 3), Name = "InfestationPit", Minerals = 100, Gas = 100 });
            lookUp.Add(97, new BuildingType() { Type = 97, Ability = 1165, Size = SC2Util.Point(3, 3), Name = "RoachWarren", Minerals = 150 });
            lookUp.Add(98, new BuildingType() { Type = 98, Ability = 1166, Size = SC2Util.Point(2, 2), Name = "SpineCrawler", Minerals = 100 });
            lookUp.Add(99, new BuildingType() { Type = 99, Ability = 1167, Size = SC2Util.Point(2, 2), Name = "SporeCrawler", Minerals = 75 });
            lookUp.Add(130, new BuildingType() { Type = 130, Ability = 1450, Size = SC2Util.Point(5, 5), Name = "PlanetaryFortress", Minerals = 150, Gas = 150 });
            lookUp.Add(132, new BuildingType() { Type = 132, Ability = 1516, Size = SC2Util.Point(5, 5), Name = "OrbitalCommand", Minerals = 150 });
            lookUp.Add(133, new BuildingType() { Type = 133, Ability = 0, Size = SC2Util.Point(3, 3), Name = "WarpGate" });
            lookUp.Add(504, new BuildingType() { Type = 504, Ability = 1163, Size = SC2Util.Point(3, 3), Name = "LurkerDen", Minerals = 100, Gas = 150 });
            lookUp.Add(1910, new BuildingType() { Type = 1910, Ability = 895, Size = SC2Util.Point(2, 2), Name = "ShieldBattery", Minerals = 100 });
            lookUp.Add(639, new BuildingType() { Type = 639, Size = SC2Util.Point(6, 6), Name = "DestructibleRocks" });

            return lookUp;
        }
        public static HashSet<int> BuildingAbilities = CreateBuildingAbilities();

        private static HashSet<int> CreateBuildingAbilities()
        {
            HashSet<int> result = new HashSet<int>();

            foreach (BuildingType building in LookUp.Values)
                if (building.Ability != 0)
                    result.Add(building.Ability);

            return result;
        }

    }

    public class Abilities
    {
        public static Dictionary<uint, uint> Creates = new Dictionary<uint, uint>();
        public static int MOVE = 1;
        public static int ATTACK = 23;
        public static int CORRUPTION = 34;
        public static int CANCEL_CORRUPTION = 35;
        public static int HOLD_FIRE_GHOST = 36;
        public static int CANCEL_HOLD_FIRE_GHOST = 38;
        public static int MORPH_TO_INFESTED_TERRAN = 40;
        public static int EXPLODE = 42;
        public static int RESEARCH_INTERCEPTOR_LAUNCH_SPEED_UPGRADE = 44;
        public static int RESEARCH_PHOENIX_ANION_PULSE_CRYSTALS = 46;
        public static int TEMPEST_RANGE_UPGRADE = 47;
        public static int FUNGAL_GROWTH = 74;
        public static int GUARDIAN_SHIELD = 76;
        public static int MULE_REPAIR = 78;
        public static int MORPH_ZERGLING_TO_BANELING = 80;
        public static int NEXUS_TRAIN_MOTHERSHIP = 110;
        public static int FEEDBACK = 140;
        public static int MASS_RECAL = 142;
        public static int PLACE_POINT_DEFENSE_DRONE = 144;
        public static int HALLUCINATION_ARCHON = 146;
        public static int HALLUCINATION_COLLOSUS = 148;
        public static int HALLUCINATION_HIGH_TEMPLAR = 150;
        public static int HALLUCINATION_IMMORTAL = 152;
        public static int HALLUCINATION_PHOENIX = 154;
        public static int HALLUCINATION_PROBE = 156;
        public static int HALLUCINATION_STALKER = 158;
        public static int HALLUCINATION_VOID_RAY = 160;
        public static int HALLUCINATION_WARP_PRISM = 162;
        public static int HALLUCINATION_ZEALOT = 164;
        public static int MULE_GATHER = 166;
        public static int SEEKER_MISSILE = 169;
        public static int CALLDOWN_MULE = 171;
        public static int GRAVITON_BEAM = 173;
        public static int SIPHON = 177;
        public static int CANCEL_SIPHON = 178;
        public static int LEECH = 179;
        public static int SPAWN_CHANGELING = 181;
        public static int PHASE_SHIFT = 193;
        public static int RALLY = 195;
        public static int RESEARCH_GLIAL_REGENERATION = 216;
        public static int RESEARCH_TUNNELING_CLAWS = 217;
        public static int INFESTED_TERRANS = 247;
        public static int NEURAL_PARASITE = 249;
        public static int CANCEL_NEURAL_PARASITE = 250;
        public static int SPAWN_LARVA = 251;
        public static int STIMPACK_MARAUDER = 253;
        public static int SUPPLY_DROP = 255;
        public static int STRIKE_CANNON = 257;
        public static int CANCEL_STRIKE_CANNON = 258;
        public static int CANCEL = 314;
        public static int REPAIR = 316;
        public static int SIEGE = 388;
        public static int UNSIEGE = 390;
        public static int MORPH_DRONE = 1342;
        public static int MORPH_ZERGLING = 1343;
        public static int MORPH_OVERLORD = 1344;
        public static int MORPH_HYDRA = 1345;
        public static int MORPH_MUTALISK = 1346;
        public static int MORPH_ULTRALISK = 1348;
        public static int MORPH_ROACH = 1351;
        public static int MORPH_INFESTOR = 1352;
        public static int MORPH_CORRUPTOR = 1353;
        public static int MORPH_SWARM_HOST = 1356;
        public static int MORPH_BROODLORD = 1372;
        public static int BLINK = 1442;
        public static int MORPH_OVERSEER = 1448;
        public static int TRANSFUSE = 1664;
        public static int MORPH_RAVAGER = 2330;
        public static int MORPH_LURKER = 2332;
        public static int BURROW_DOWN = 2108;
        public static int BURROW_UP = 2110;
        public static int CORROSIVE_BILE = 2338;
        public static int WIDOW_MINE_BURROW = 2095;
        public static int WIDOW_MINE_UNBURROW = 2097;
    }

}
