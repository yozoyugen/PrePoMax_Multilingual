﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    public class BoundingBoxVolumeComparer : IComparer<BoundingBox>
    {
        public int Compare(BoundingBox bb1, BoundingBox bb2)
        {
            // Use rounding to eliminate numerical error - different exploded view
            double v1 = Tools.RoundToSignificantDigits(bb1.GetVolume(), 6);
            double v2 = Tools.RoundToSignificantDigits(bb2.GetVolume(), 6);
            //
            if (v1 < v2) return 1;
            else if (v1 > v2) return -1;
            else return 0;
        }
    }
    public class BoundingBoxDistanceVolumeComparer : IComparer<BoundingBox>
    {
        public static double[] Center;
        public int Compare(BoundingBox bb1, BoundingBox bb2)
        {
            if (Center == null || Center.Length != 3)
                throw new NotSupportedException();
            double[] c1 = bb1.GetCenter();
            double[] c2 = bb2.GetCenter();
            double dist1 = Math.Pow(c1[0] - Center[0], 2) + Math.Pow(c1[1] - Center[1], 2) + Math.Pow(c1[2] - Center[2], 2);
            double dist2 = Math.Pow(c2[0] - Center[0], 2) + Math.Pow(c2[1] - Center[1], 2) + Math.Pow(c2[2] - Center[2], 2);
            // Use rounding to eliminate numerical error - different exploded view
            dist1 = Tools.RoundToSignificantDigits(dist1, 6);
            dist2 = Tools.RoundToSignificantDigits(dist2, 6);
            //
            if (dist1 > dist2) return 1;
            else if (dist1 < dist2) return -1;
            else
            {
                BoundingBoxVolumeComparer boundingBoxVolumeComparer = new BoundingBoxVolumeComparer();
                return boundingBoxVolumeComparer.Compare(bb1, bb2);
            }
        }
    }

    [Serializable]
    public class BoundingBox
    {
        // Variables                                                                                                                
        public double MinX;
        public double MinY;
        public double MinZ;
        //
        public double MaxX;
        public double MaxY;
        public double MaxZ;
        //
        public object Tag;


        // Constructors                                                                                                             
        public BoundingBox()
        {
            Reset();
        }
        public BoundingBox(BoundingBox box)
        {
            MinX = box.MinX;
            MinY = box.MinY;
            MinZ = box.MinZ;
            //
            MaxX = box.MaxX;
            MaxY = box.MaxY;
            MaxZ = box.MaxZ;
            //
            if (box.Tag != null) Tag = box.Tag.DeepClone();
        }


        // Static methods                                                                                                           
        public static double[] GetCenter(IEnumerable<BoundingBox> boxes)
        {
            BoundingBox bb = new BoundingBox();
            foreach (var box in boxes) bb.IncludeBox(box);
            return bb.GetCenter();
        }
        public static double[][] GetExplodedBBOffsets(int explodedDirection, double scaleFactor, BoundingBox[] boxes,
                                                      BoundingBox[] fixedBoxes = null)
        {
            //
            // https://stackoverflow.com/questions/3265986/an-algorithm-to-space-out-overlapping-rectangles
            //
            // Renumber and add scale
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].Tag = i;
                boxes[i].Scale(1.2);
            }
            // Sort by size
            Array.Sort(boxes, new BoundingBoxVolumeComparer());
            //
            int firstBoxId = 0;
            BoundingBox globalBox = new BoundingBox();
            double[][] offsets = new double[boxes.Length][];
            List<BoundingBox> nonIntersectingBBs = new List<BoundingBox>();
            // Add fixed boxes
            if (fixedBoxes != null && fixedBoxes.Length > 0)
            {
                nonIntersectingBBs.AddRange(fixedBoxes);
                foreach (var fixedBox in fixedBoxes) globalBox.IncludeBox(fixedBox);
            }
            else
            {
                // Add the largest box
                firstBoxId = 1;
                globalBox.IncludeBox(boxes[0]);
                nonIntersectingBBs.Add(boxes[0]);
                offsets[(int)boxes[0].Tag] = new double[] { 0, 0, 0 };
            }
            int count;
            Vec3D center;
            Vec3D offset;
            Vec3D direction;
            BoundingBox box;
            for (int i = firstBoxId; i < boxes.Length; i++)
            {
                box = boxes[i];
                center = new Vec3D(globalBox.GetCenter());
                offset = new Vec3D();
                direction = new Vec3D(box.GetCenter()) - center;
                // Set the offset direction
                if (explodedDirection == 1) direction.Y = direction.Z = 0;
                else if (explodedDirection == 2) direction.X = direction.Z = 0;
                else if (explodedDirection == 3) direction.X = direction.Y = 0;
                else if (explodedDirection == 4) direction.Z = 0;
                else if (explodedDirection == 5) direction.Y = 0;
                else if (explodedDirection == 6) direction.X = 0;
                // Fix the 0 length direction
                if (direction.Len2 < 1E-6 * globalBox.GetDiagonal()) direction.Coor = new double[] { 1, 1, 1 };
                // Set the offset direction
                if (explodedDirection == 1) direction.Y = direction.Z = 0;
                else if (explodedDirection == 2) direction.X = direction.Z = 0;
                else if (explodedDirection == 3) direction.X = direction.Y = 0;
                else if (explodedDirection == 4) direction.Z = 0;
                else if (explodedDirection == 5) direction.Y = 0;
                else if (explodedDirection == 6) direction.X = 0;
                //
                direction.Normalize();
                direction *= (0.01 * globalBox.GetDiagonal());
                //
                count = 0;
                while (box.Intersects(nonIntersectingBBs) && count++ < 10000)
                {
                    box.AddOffset(direction.Coor);
                    offset += direction;
                }
                nonIntersectingBBs.Add(box);
                globalBox.IncludeBox(box);
                //
                offsets[(int)box.Tag]= (offset * scaleFactor).Coor;
            }
            //
            return offsets;
        }
        public static double[][] GetExplodedBBbyCPOffsets(double[] centerPoint, int explodedDirection, double scaleFactor,
                                                          BoundingBox[] boxes)
        {
            BoundingBox assemblyBB = new BoundingBox();
            // Renumber and add scale
            for (int i = 0; i < boxes.Length; i++)
            {
                assemblyBB.IncludeBox(boxes[i]);
                //
                boxes[i].Tag = i;
                boxes[i].Scale(1.2);
            }
            // Sort by distance and size
            BoundingBoxDistanceVolumeComparer.Center = centerPoint.ToArray();
            Array.Sort(boxes, new BoundingBoxDistanceVolumeComparer());
            //
            double[][] offsets = new double[boxes.Length][];
            //
            Vec3D center;
            Vec3D offset;
            Vec3D direction;
            double distance;
            BoundingBox box;
            for (int i = 0; i < boxes.Length; i++)
            {
                box = boxes[i];
                center = new Vec3D(centerPoint);
                direction = new Vec3D(box.GetCenter()) - center;
                distance = direction.Len;
                // Set the offset type
                if (explodedDirection == 1) direction.Y = direction.Z = 0;
                else if (explodedDirection == 2) direction.X = direction.Z = 0;
                else if (explodedDirection == 3) direction.X = direction.Y = 0;
                else if (explodedDirection == 4) direction.Z = 0;
                else if (explodedDirection == 5) direction.Y = 0;
                else if (explodedDirection == 6) direction.X = 0;
                // Normalize after truncation
                direction.Normalize();
                // Compute offset
                offset = distance * direction * 2;
                offsets[(int)box.Tag] = (offset * scaleFactor).Coor;
            }
            //
            return offsets;
        }


        // Methods                                                                                                                  
        public void Reset()
        {
            MinX = double.MaxValue;
            MinY = double.MaxValue;
            MinZ = double.MaxValue;
            MaxX = -double.MaxValue;
            MaxY = -double.MaxValue;
            MaxZ = -double.MaxValue;
        }
        public void AddOffset(double[] offset)
        {
            MinX += offset[0];
            MaxX += offset[0];
            MinY += offset[1];
            MaxY += offset[1];
            MinZ += offset[2];
            MaxZ += offset[2];
        }
        public void RemoveOffset(double[] offset)
        {
            MinX -= offset[0];
            MaxX -= offset[0];
            MinY -= offset[1];
            MaxY -= offset[1];
            MinZ -= offset[2];
            MaxZ -= offset[2];
        }
        public void Inflate(double offset)
        {
            MinX -= offset;
            MaxX += offset;
            MinY -= offset;
            MaxY += offset;
            MinZ -= offset;
            MaxZ += offset;
        }
        public void InflateByDiagonal(double offsetFactor)
        {
            Inflate(GetDiagonal() * offsetFactor);
        }
        public void InflateIfThinn(double offsetFactor)
        {
            if (IsThinnInAnyDirection())
                Inflate(GetDiagonal() * offsetFactor);
        }
        public void Scale(double scaleFactor)
        {
            double delta = 0.5 * (MaxX - MinX) * (scaleFactor - 1);
            MinX -= delta;
            MaxX += delta;
            //
            delta = 0.5 * (MaxY - MinY) * (scaleFactor - 1);
            MinY -= delta;
            MaxY += delta;
            //
            delta = 0.5 * (MaxZ - MinZ) * (scaleFactor - 1);
            MinZ -= delta;
            MaxZ += delta;
        }
        
        //
        public void IncludeCoor(double[] coor)
        {
            if (coor[0] > MaxX) MaxX = coor[0];
            if (coor[0] < MinX) MinX = coor[0];
            //
            if (coor[1] > MaxY) MaxY = coor[1];
            if (coor[1] < MinY) MinY = coor[1];
            //
            if (coor[2] > MaxZ) MaxZ = coor[2];
            if (coor[2] < MinZ) MinZ = coor[2];
        }
        public void IncludeFirstCoor(double[] coor)
        {
            MaxX = coor[0];
            MinX = coor[0];
            //
            MaxY = coor[1];
            MinY = coor[1];
            //
            MaxZ = coor[2];
            MinZ = coor[2];
        }
        public void IncludeCoorFast(double[] coor)
        {
            if (coor[0] > MaxX) MaxX = coor[0];
            else if (coor[0] < MinX) MinX = coor[0];
            //
            if (coor[1] > MaxY) MaxY = coor[1];
            else if (coor[1] < MinY) MinY = coor[1];
            //
            if (coor[2] > MaxZ) MaxZ = coor[2];
            else if (coor[2] < MinZ) MinZ = coor[2];
        }
        public void IncludeCoors(double[][] coors)
        {
            if (coors.Length > 0) IncludeFirstCoor(coors[0]);
            for (int i = 0; i < coors.Length; i++) IncludeCoorFast(coors[i]);
        }
        public void IncludeBox(BoundingBox box)
        {
            if (box.MaxX > MaxX) MaxX = box.MaxX;
            if (box.MinX < MinX) MinX = box.MinX;
            //
            if (box.MaxY > MaxY ) MaxY = box.MaxY;
            if (box.MinY < MinY) MinY = box.MinY;
            //
            if (box.MaxZ > MaxZ) MaxZ = box.MaxZ;
            if (box.MinZ < MinZ) MinZ = box.MinZ;
        }
        public void IncludeNode(FeNode node)
        {
            if (node.X > MaxX) MaxX = node.X;
            if (node.X < MinX) MinX = node.X;
            //
            if (node.Y > MaxY) MaxY = node.Y;
            if (node.Y < MinY) MinY = node.Y;
            //
            if (node.Z > MaxZ) MaxZ = node.Z;
            if (node.Z < MinZ) MinZ = node.Z;
        }
        //
        public bool Intersects(BoundingBox box, double relativeOffset = 0)
        {
            double off1 = 0;
            double off2 = 0;
            if (relativeOffset > 0)
            {
                off1 = GetDiagonal() * relativeOffset;
                off2 = box.GetDiagonal() * relativeOffset;
            }
            //
            if (box.MaxX + off2 < MinX - off1) return false;
            if (box.MinX - off2 > MaxX + off1) return false;
            if (box.MaxY + off2 < MinY - off1) return false;
            if (box.MinY - off2 > MaxY + off1) return false;
            if (box.MaxZ + off2 < MinZ - off1) return false;
            if (box.MinZ - off2 > MaxZ + off1) return false;
            return true;
        }
        public bool Intersects(List<BoundingBox> boxes)
        {
            foreach (var box in boxes)
            {
                if (Intersects(box)) return true;
            }
            return false;
        }
        public BoundingBox GetIntersection(BoundingBox box)
        {
            BoundingBox intersection = new BoundingBox();
            //
            intersection.MinX = Math.Max(MinX, box.MinX);
            intersection.MaxX = Math.Min(MaxX, box.MaxX);
            //
            intersection.MinY = Math.Max(MinY, box.MinY);
            intersection.MaxY = Math.Min(MaxY, box.MaxY);
            //
            intersection.MinZ = Math.Max(MinZ, box.MinZ);
            intersection.MaxZ = Math.Min(MaxZ, box.MaxZ);
            //
            return intersection;
        }
        //
        public double MaxOutsideAxialDistance(double[] coor)
        {
            double distX = 0;
            double distY = 0;
            double distZ = 0;
            if (coor[0] < MinX) distX = MinX - coor[0];
            else if (coor[0] > MaxX) distX = coor[0] - MaxX;
            //
            if (coor[1] < MinY) distY = MinY - coor[1];
            else if (coor[1] > MaxY) distY = coor[1] - MaxY;
            //
            if (coor[2] < MinZ) distZ = MinZ - coor[2];
            else if (coor[2] > MaxZ) distZ = coor[2] - MaxZ;
            //
            return Math.Max(Math.Max(distX, distY), distZ);
        }
        public double MinOutsideDistance2(double[] coor)
        {
            double d;
            double dist = 0;
            //
            if (coor[0] < MinX)
            {
                d = MinX - coor[0];
                dist = d * d;
            }
            else if (coor[0] > MaxX)
            {
                d = coor[0] - MaxX;
                dist = d * d;
            }
            //
            if (coor[1] < MinY)
            {
                d = MinY - coor[1];
                dist += d * d;
            }
            else if (coor[1] > MaxY)
            {
                d = coor[1] - MaxY;
                dist += d * d;
            }
            //
            if (coor[2] < MinZ)
            {
                d = MinZ - coor[2];
                dist += d * d;
            }
            else if (coor[2] > MaxZ)
            {
                d = coor[2] - MaxZ;
                dist += d * d;
            }
            //
            return dist;
        }
        public double MaxOutsideDistance2(double[] coor)
        {
            double d;
            double dist = 0;
            //
            if (coor[0] < MinX)
            {
                d = MaxX - coor[0];
                dist = d * d;
            }
            else if (coor[0] > MaxX)
            {
                d = coor[0] - MinX;
                dist = d * d;
            }
            //
            if (coor[1] < MinY)
            {
                d = MaxY - coor[1];
                dist += d * d;
            }
            else if (coor[1] > MaxY)
            {
                d = coor[1] - MinY;
                dist += d * d;
            }
            //
            if (coor[2] < MinZ)
            {
                d = MaxZ - coor[2];
                dist += d * d;
            }
            else if (coor[2] > MaxZ)
            {
                d = coor[2] - MinZ;
                dist += d * d;
            }
            //
            return dist;
        }
        public bool IsMaxOutsideDistance2SmallerThan(double[] coor, double limit)
        {
            double d;
            double dist = 0;
            //
            if (coor[0] < MinX)
            {
                d = MinX - coor[0];
                dist = d * d;
                if (dist >= limit) return false;
            }
            else if (coor[0] > MaxX)
            {
                d = coor[0] - MaxX;
                dist = d * d;
                if (dist >= limit) return false;
            }
            //
            if (coor[1] < MinY)
            {
                d = MinY - coor[1];
                dist += d * d;
                if (dist >= limit) return false;
            }
            else if (coor[1] > MaxY)
            {
                d = coor[1] - MaxY;
                dist += d * d;
                if (dist >= limit) return false;
            }
            //
            if (coor[2] < MinZ)
            {
                d = MinZ - coor[2];
                dist += d * d;
                if (dist >= limit) return false;
            }
            else if (coor[2] > MaxZ)
            {
                d = coor[2] - MaxZ;
                dist += d * d;
                if (dist >= limit) return false;
            }
            //
            return true;
        }
        //
        public double[] GetCenter()
        {
            return new double[] { (MinX + MaxX) / 2, (MinY + MaxY) / 2, (MinZ + MaxZ) / 2 };
        }
        public double GetXSize()
        {
            return MaxX - MinX;
        }
        public double GetYSize()
        {
            return MaxY - MinY;
        }
        public double GetZSize()
        {
            return MaxZ - MinZ;
        }
        public double GetDiagonal()
        {
            return Math.Sqrt(Math.Pow(MaxX - MinX, 2) + Math.Pow(MaxY - MinY, 2) + Math.Pow(MaxZ - MinZ, 2));
        }
        public double GetVolume()
        {
            return (MaxX - MinX) * (MaxY - MinY) * (MaxZ - MinZ);
        }
        public bool Is2D()
        {
            double epsilon = GetDiagonal() * 1E-6;
            //
            if (Math.Abs(MinZ) < epsilon && Math.Abs(MaxZ) < epsilon) return true;
            else return false;
        }
        public bool IsThinnInAnyDirection()
        {
            double epsilon = GetDiagonal() * 1E-2;
            //
            if (Math.Abs(MaxX - MinX) < epsilon) return true;
            else if (Math.Abs(MaxY - MinY) < epsilon) return true;
            else if (Math.Abs(MaxZ - MinZ) < epsilon) return true;
            else return false;
        }
        //
        public bool IsEqualInSize(BoundingBox boundingBox)
        {
            double size = Math.Max(Math.Max(MaxX - MinX, MaxY - MinY), MaxZ - MinZ) * 1E-6;
            //
            if (Math.Abs(MinX - boundingBox.MinX) > size) return false;
            else if (Math.Abs(MaxX - boundingBox.MaxX) > size) return false;
            //
            else if (Math.Abs(MinY - boundingBox.MinY) > size) return false;
            else if (Math.Abs(MaxY - boundingBox.MaxY) > size) return false;
            //
            else if (Math.Abs(MinZ - boundingBox.MinZ) > size) return false;
            else if (Math.Abs(MaxZ - boundingBox.MaxZ) > size) return false;
            //
            else return true;
        }
        //
        public BoundingBox DeepCopy()
        {
            return new BoundingBox(this);
        }
    }
}
