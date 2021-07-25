using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckGenerator
{
    private int roomSize;
    private int roomDistance;

    public int RoomsX, RoomsY;
    public int[,] Rooms;
    public int GeneratedRooms
    {
        get
        {
            int sum = 0;

            for (int roomX = 0; roomX < RoomsX; roomX++)
                for (int roomY = 0; roomY < RoomsY; roomY++)
                    sum += Rooms[roomX, roomY];

            return sum;
        }
    }

    private int corridorsX;
    private int corridorsY;
    private int innerCorridorsX;
    private int innerCorridorsY;

    private double corridorClosureProbability;

    public int Width;
    public int Height;
    public int[,] Matrix;

    private int corridorWidth;
    private int corridorHeight;
    private int longestPath;

    private readonly System.Random random = new System.Random();

    public DeckGenerator()
    {
        roomSize = 3;
        roomDistance = roomSize + 3;

        RoomsX = 3;
        RoomsY = 6;

        Rooms = new int[RoomsX, RoomsY];

        corridorsX = RoomsX + 1;
        corridorsY = RoomsY + 1;
        innerCorridorsX = corridorsX - 2;
        innerCorridorsY = corridorsY - 2;

        corridorClosureProbability = 0.1;

        Width = 3 + roomDistance * RoomsX;
        Height = 3 + roomDistance * RoomsY;
        Matrix = new int[Width, Height];

        corridorWidth = Width - 4;
        corridorHeight = Height - 4;
        longestPath = innerCorridorsX * corridorHeight + innerCorridorsY + corridorWidth - innerCorridorsX * innerCorridorsY;

        GenerateDeck();
    }

    public void GenerateDeck()
    {
        GenerateCorridors();
        GenerateRooms();
    }

    private void GenerateCorridors()
    {
        // generate vertical corridors
        for (int corridorX = 0; corridorX < corridorsX; corridorX++)
        {
            int x = 1 + corridorX * roomDistance;
            for (int y = 1; y < Height - 1; y++)
            {
                if (IsBorderCorridorCell(x, y) || y % 2 != 0 || random.NextDouble() >= corridorClosureProbability)
                    Matrix[x, y] = 1;
            }
        }

        // generate horizontal corridors
        for (int corridorY = 0; corridorY < corridorsY; corridorY++)
        {
            int y = 1 + corridorY * roomDistance;
            for (int x = 1; x < Width - 1; x++)
            {
                if (IsBorderCorridorCell(x, y) || x % 2 != 0 || random.NextDouble() >= corridorClosureProbability)
                    Matrix[x, y] = 1;
            }
        }

        // check vertical dead-ends
        for (int corridorX = 1; corridorX < corridorsX - 1; corridorX++)
        {
            int x = 1 + corridorX * roomDistance;
            for (int y = 1; y < Height - 1; y++)
            {
                if (CheckDeadEnds(x, y) == 0)
                    Matrix[x, y] = 0;
            }
        }

        // check horizontal dead-ends
        for (int corridorY = 1; corridorY < corridorsY - 1; corridorY++)
        {
            int y = 1 + corridorY * roomDistance;
            for (int x = 1; x < Width - 1; x++)
            {
                if (CheckDeadEnds(x, y) == 0)
                    Matrix[x, y] = 0;
            }
        }
    }

    private void GenerateRooms()
    {
        for (int roomX = 0; roomX < RoomsX; roomX++)
        {
            int startX = 3 + roomX * roomDistance;
            for (int roomY = 0; roomY < RoomsY; roomY++)
            {
                int startY = 3 + roomY * roomDistance;

                if (random.Next(4) != 0)
                {
                    // decide door coordinates
                    int doorX = -1;
                    int doorY = -1;

                    void setDoor(int x, int y)
                    {
                        doorX = x;
                        doorY = y;
                    }

                    int doorSide = random.Next(4);
                    int doorCorner = random.Next(2);
                    int doorRotation = 4 + doorSide * doorCorner;

                    switch (doorSide)
                    {
                        case 0:
                            if (doorCorner == 0) setDoor(startX + roomSize, startY);
                            else setDoor(startX + roomSize, startY + roomSize - 1);
                            break;
                        case 1:
                            if (doorCorner == 0) setDoor(startX, startY - 1);
                            else setDoor(startX + roomSize - 1, startY - 1);
                            break;
                        case 2:
                            if (doorCorner == 0) setDoor(startX - 1, startY);
                            else setDoor(startX - 1, startY + roomSize - 1);
                            break;
                        case 3:
                            if (doorCorner == 0) setDoor(startX, startY + roomSize);
                            else setDoor(startX + roomSize - 1, startY + roomSize);
                            break;
                    }

                    // decide if room should be generated
                    if (
                        Math.Abs(Matrix[doorX, doorY - 1]) == 1
                        || Math.Abs(Matrix[doorX, doorY + 1]) == 1
                        || Math.Abs(Matrix[doorX - 1, doorY]) == 1
                        || Math.Abs(Matrix[doorX + 1, doorY]) == 1
                    )
                    {
                        Rooms[roomX, roomY] = 1;
                        Matrix[doorX, doorY] = doorRotation;

                        // fill room
                        for (int x = 0; x < roomSize; x++)
                            for (int y = 0; y < roomSize; y++)
                                Matrix[startX + x, startY + y] = 2;
                    }
                }
            }
        }
    }

    private int CheckDeadEnds(int x, int y, int depth = 0, int previousX = -1, int previousY = -1)
    {
        if (IsBorderCorridorCell(x, y)) return 1;

        int cell = Matrix[x, y];
        if (cell == 0) return -1;

        if (depth > longestPath) return 0;

        int check;

        if (
            (y - 1 != previousY && CheckDeadEnds(x, y - 1, depth + 1, x, y) == 1)
            || (y + 1 != previousY && CheckDeadEnds(x, y + 1, depth + 1, x, y) == 1)
            || (x - 1 != previousX && CheckDeadEnds(x - 1, y, depth + 1, x, y) == 1)
            || (x + 1 != previousX && CheckDeadEnds(x + 1, y, depth + 1, x, y) == 1)
        ) check = 1;
        else check = 0;

        return check;
    }

    private bool IsBorderCorridorCell(int x, int y)
    {
        return x == 1 || y == 1 || x == Width - 2 || y == Height - 2;
    }
}
