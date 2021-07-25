using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckGeneration : MonoBehaviour
{
    public GameObject Ceiling;
    public GameObject Floor_A;
    public GameObject Floor_B;
    public GameObject Floor_C;
    public GameObject Floor_D;
    public GameObject Floor_E;
    public GameObject DoorWall;
    public GameObject Wall;

    private int wallWidth;
    private int wallHeight;

    private DeckGenerator deckGenerator;

    void Start()
    {
        wallWidth = (int)(Wall.transform.localScale.x * 10);
        wallHeight = (int)(Wall.transform.localScale.z * 10);

        deckGenerator = new DeckGenerator();
        while (deckGenerator.GeneratedRooms < 3) deckGenerator.GenerateDeck();

        print($"Generated {deckGenerator.GeneratedRooms} rooms\n");

        for (int x = 0; x < deckGenerator.Width; x++)
        {
            for (int y = 0; y < deckGenerator.Height; y++)
            {
                InstantiateCeiling(x, y);
                InstantiateFloor(x, y);
                InstantiateWalls(x, y);
            }
        }
    }

    private void InstantiateCeiling(int x, int y)
    {
        Instantiate(Ceiling, new Vector3(x * wallWidth, wallHeight, y * wallWidth), Quaternion.identity, transform);
    }

        private void InstantiateFloor(int x, int y)
    {
        int cell = deckGenerator.Matrix[x, y];
        if (cell == 1)
        {
            int north = Mathf.Min(deckGenerator.Matrix[x, y + 1], 1);
            int east = Mathf.Min(deckGenerator.Matrix[x + 1, y], 1);
            int south = Mathf.Min(deckGenerator.Matrix[x, y - 1], 1);
            int west = Mathf.Min(deckGenerator.Matrix[x - 1, y], 1);
            int sum = north + east + south + west;

            switch (sum)
            {
                case 1:
                    if (north == 1) Instantiate(Floor_E, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 90, 0), transform);
                    else if (east == 1) Instantiate(Floor_E, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 180, 0), transform);
                    else if (south == 1) Instantiate(Floor_E, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, -90, 0), transform);
                    else if (west == 1) Instantiate(Floor_E, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 0, 0), transform);
                    break;
                case 2:
                    if (north == 1)
                    {
                        if (south == 1) Instantiate(Floor_A, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 90, 0), transform);
                        else if (east == 1) Instantiate(Floor_B, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 90, 0), transform);
                        else if (west == 1) Instantiate(Floor_B, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 0, 0), transform);
                    }
                    else if (east == 1)
                    {
                        if (west == 1) Instantiate(Floor_A, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 0, 0), transform);
                        else Instantiate(Floor_B, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 180, 0), transform);
                    }
                    else Instantiate(Floor_B, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, -90, 0), transform);
                    break;
                case 3:
                    if (north == 0) Instantiate(Floor_C, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 180, 0), transform);
                    else if (east == 0) Instantiate(Floor_C, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, -90, 0), transform);
                    else if (south == 0) Instantiate(Floor_C, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 0, 0), transform);
                    else if (west == 0) Instantiate(Floor_C, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.Euler(0, 90, 0), transform);
                    break;
                case 4:
                    Instantiate(Floor_D, new Vector3(x * wallWidth, 0, y * wallWidth), Quaternion.identity, transform);
                    break;
            }
        }
    }

    private void InstantiateWalls(int x, int y)
    {
        int cell = deckGenerator.Matrix[x, y];
        if (cell == 1)
        {
            int north = deckGenerator.Matrix[x, y + 1];
            int east = deckGenerator.Matrix[x + 1, y];
            int south = deckGenerator.Matrix[x, y - 1];
            int west = deckGenerator.Matrix[x - 1, y];

            if (north == 0) Instantiate(Wall, new Vector3(x * wallWidth, wallHeight / 2f, y * wallWidth + wallWidth / 2f), Quaternion.Euler(90, 0, 0), transform);
            else if (north >= 4) Instantiate(DoorWall, new Vector3(x * wallWidth, wallHeight / 2f, y * wallWidth + wallWidth / 2f), Quaternion.Euler(90, 0, 0), transform);

            if (east == 0) Instantiate(Wall, new Vector3(x * wallWidth + wallWidth / 2f, wallHeight / 2f, y * wallWidth), Quaternion.Euler(90, 90, 0), transform);
            else if (east >= 4) Instantiate(DoorWall, new Vector3(x * wallWidth + wallWidth / 2f, wallHeight / 2f, y * wallWidth), Quaternion.Euler(90, 90, 0), transform);

            if (south == 0) Instantiate(Wall, new Vector3(x * wallWidth, wallHeight / 2f, y * wallWidth - wallWidth / 2f), Quaternion.Euler(90, 180, 0), transform);
            else if (south >= 4) Instantiate(DoorWall, new Vector3(x * wallWidth, wallHeight / 2f, y * wallWidth - wallWidth / 2f), Quaternion.Euler(90, 180, 0), transform);

            if (west == 0) Instantiate(Wall, new Vector3(x * wallWidth - wallWidth / 2f, wallHeight / 2f, y * wallWidth), Quaternion.Euler(90, -90, 0), transform);
            else if (west >= 4) Instantiate(DoorWall, new Vector3(x * wallWidth - wallWidth / 2f, wallHeight / 2f, y * wallWidth), Quaternion.Euler(90, -90, 0), transform);
        }
    }
}
