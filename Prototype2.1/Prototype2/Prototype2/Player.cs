using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Prototype2
{
    public class Player
    {
        Texture2D playerIcon, flag;
        Point[] inventory = new Point[9];
        Point spawnPosition;
        Point position;

        bool[,] path = new bool[11, 9];
        int[,] flagArea = new int[11, 9];
        int mines = 4;//9
        int actionPoints = 0;
        int bitMine, bitFlag;

        public static bool Player1Turn = true;
        bool flagIsSet = false;

        public Player(Texture2D playerIcon, Texture2D flag, Point spawnPosition, int bitM, int bitF)
        {
            this.playerIcon = playerIcon;
            this.flag = flag;
            this.spawnPosition = spawnPosition;
            position = spawnPosition;
            bitMine = bitM;
            bitFlag = bitF;
            path[spawnPosition.X, spawnPosition.Y] = true;
        }

        public Point getSpawn()
        {
            return spawnPosition;
        }

        public Point getPosition()
        {
            return position;
        }

        public int getBitM()
        {
            return bitMine;
        }

        public int getBitF()
        {
            return bitFlag;
        }

        public int getMines()
        {
            return mines;
        }

        public int getActionPoints()
        {
            return actionPoints;
        }

        public bool getFlagIsSet()
        {
            return flagIsSet;
        }

        public Point[] getInventory()
        {
            return inventory;
        }

        public bool[,] getPath()
        {
            return path;
        }

        public int[,] getFlagArea()
        {
            return flagArea;
        }

        public void fillFlagArea(int x, int y)
        {
            flagArea[x, y]++;
        }

        public void resetActionPoints()
        {
            actionPoints = 5;
        }

        public void addActionPoints(int add)
        {
            actionPoints += add;
        }

        public void subActionPoints(int sub)
        {
            actionPoints -= sub;
        }

        public void setPosition(int x, int y)
        {
            position.X = x;
            position.Y = y;
        }

        public void setPosition(Point p)
        {
            position = p;
        }

        public void incMine()
        {
            mines++;
        }

        public void decMine()
        {
            mines--;
        }

        public void changeFlagIsSet()
        {
            flagIsSet = !flagIsSet;
        }

        public void appendInventory(Point coordinate)
        {
            inventory[mines] = coordinate;
        }

        public void appendFlag(Point coordinate)
        {
            inventory[0] = coordinate;
        }

        public void removeInventory()
        {
            inventory[mines] = Point.Zero;
        }

        public void removeFlagFromInventory()
        {
            inventory[0] = Point.Zero;
        }

        public void move(int x, int y)
        {
            path[x, y] = true;
        }
    }
}