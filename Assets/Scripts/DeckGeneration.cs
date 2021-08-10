using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckGeneration : MonoBehaviour
{
    public GameObject Ceiling_A;
    public GameObject Ceiling_B;
    public GameObject Ceiling_Thin;

    public GameObject Floor_A;
    public GameObject Floor_A_Thin;
    public GameObject Floor_B;
    public GameObject Floor_C;
    public GameObject Floor_D;
    public GameObject Floor_E;

    public GameObject Wall_A;
    public GameObject Wall_B;
    public GameObject Wall_Door;
    public GameObject Wall_Thin;

    public GameObject Room_A;
    public GameObject Room_B;

    public int WallWidth;
    public int WallHeight;

    public float EvenWallFactor;
    public float OddWallFactor;

    private DeckGenerator deckGenerator;

    void Start()
    {
        deckGenerator = new DeckGenerator();
        while (deckGenerator.GeneratedRooms < 3) deckGenerator.GenerateDeck();

        print($"Generated {deckGenerator.GeneratedRooms} rooms\n");

        float deckX = 0;
        float deckZ = 0;

        for (int y = 0; y < deckGenerator.Height; y++)
        {
            for (int x = 0; x < deckGenerator.Width; x++)
            {
                InstantiateCeiling(x, y, deckX, deckZ);
                InstantiateFloor(x, y, deckX, deckZ);
                InstantiateWalls(x, y, deckX, deckZ);
                InstantiateRoom(x, y, deckX, deckZ);

                deckX += WallWidth * (EvenWallFactor + OddWallFactor) / 2;
            }

            deckX = 0;
            deckZ += WallWidth * (EvenWallFactor + OddWallFactor) / 2;
        }
    }

    private void InstantiateCeiling(int x, int y, float deckX, float deckZ)
    {
        int cell = deckGenerator.Matrix[x, y];
        if (cell == 1)
        {
            if (x % 2 == 0) Instantiate(Ceiling_Thin, new Vector3(deckX, WallHeight, deckZ), Quaternion.Euler(0, 0, 0), transform);
            else if (y % 2 == 0) Instantiate(Ceiling_Thin, new Vector3(deckX, WallHeight, deckZ), Quaternion.Euler(0, 90, 0), transform);
            else
            {
                GameObject ceiling = ((x - 1) / 2 % 2 == 0 ^ (y - 1) / 2 % 2 == 0) ? Ceiling_B : Ceiling_A;
                Instantiate(ceiling, new Vector3(deckX, WallHeight, deckZ), Quaternion.Euler(0, 0, 0), transform);
            }
        }
    }

    private void InstantiateFloor(int x, int y, float deckX, float deckZ)
    {
        int cell = deckGenerator.Matrix[x, y];
        if (cell == 1)
        {
            int north = Mathf.Min(deckGenerator.Matrix[x, y + 1], 1);
            int east = Mathf.Min(deckGenerator.Matrix[x + 1, y], 1);
            int south = Mathf.Min(deckGenerator.Matrix[x, y - 1], 1);
            int west = Mathf.Min(deckGenerator.Matrix[x - 1, y], 1);
            int sum = north + east + south + west;

            if (x % 2 == 0) Instantiate(Floor_A_Thin, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
            else if (y % 2 == 0) Instantiate(Floor_A_Thin, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
            else
            {
                switch (sum)
                {
                    case 1:
                        if (north == 1) Instantiate(Floor_E, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        else if (east == 1) Instantiate(Floor_E, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        else if (south == 1) Instantiate(Floor_E, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        else if (west == 1) Instantiate(Floor_E, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        break;
                    case 2:
                        if (north == 1)
                        {
                            if (south == 1) Instantiate(Floor_A, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                            else if (east == 1) Instantiate(Floor_B, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                            else if (west == 1) Instantiate(Floor_B, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        }
                        else if (east == 1)
                        {
                            if (west == 1) Instantiate(Floor_A, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                            else Instantiate(Floor_B, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        }
                        else Instantiate(Floor_B, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        break;
                    case 3:
                        if (north == 0) Instantiate(Floor_C, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        else if (east == 0) Instantiate(Floor_C, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        else if (south == 0) Instantiate(Floor_C, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        else if (west == 0) Instantiate(Floor_C, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        break;
                    case 4:
                        Instantiate(Floor_D, new Vector3(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        break;
                }
            }
        }
    }

    private void InstantiateWalls(int x, int y, float deckX, float deckZ)
    {
        int cell = deckGenerator.Matrix[x, y];
        if (cell == 1)
        {
            if (x % 2 == 0)
            {
                Instantiate(Wall_Thin, new Vector3(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                Instantiate(Wall_Thin, new Vector3(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
            }
            else if (y % 2 == 0)
            {
                Instantiate(Wall_Thin, new Vector3(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                Instantiate(Wall_Thin, new Vector3(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);
            }
            else
            {
                int north = deckGenerator.Matrix[x, y + 1];
                int east = deckGenerator.Matrix[x + 1, y];
                int south = deckGenerator.Matrix[x, y - 1];
                int west = deckGenerator.Matrix[x - 1, y];

                GameObject wall = ((x - 1) / 2 % 2 == 0 ^ (y - 1) / 2 % 2 == 0) ? Wall_A : Wall_B;

                if (north == 0) Instantiate(wall, new Vector3(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                else if (north >= 4 && north < 12) Instantiate(Wall_Door, new Vector3(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);

                if (east == 0) Instantiate(wall, new Vector3(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                else if (east >= 4 && east < 12) Instantiate(Wall_Door, new Vector3(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);

                if (south == 0) Instantiate(wall, new Vector3(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
                else if (south >= 4 && south < 12) Instantiate(Wall_Door, new Vector3(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);

                if (west == 0) Instantiate(wall, new Vector3(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);
                else if (west >= 4 && west < 12) Instantiate(Wall_Door, new Vector3(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, -90, 0), transform);
            }
        }
    }

    private void InstantiateRoom(int x, int y, float deckX, float deckZ)
    {
        int cell = deckGenerator.Matrix[x, y];
        if (cell >= 12)
        {
            cell -= 12;
            int corner = cell / 4;
            int rotation = cell % 4;

            GameObject room = corner == 0 ? Room_A : Room_B;
            Instantiate(room, new Vector3(deckX + 3.75f, 0, deckZ + 3.75f), Quaternion.Euler(0, 90 * rotation, 0), transform);
        }
    }
}
