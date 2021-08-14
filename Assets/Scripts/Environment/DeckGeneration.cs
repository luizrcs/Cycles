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
    public GameObject Wall_Thin;
    public GameObject Wall_Door_A;
    public GameObject Wall_Door_B;

    public GameObject ExitDoor;

    public GameObject Room_A;
    public GameObject Room_B;

    public int WallWidth;
    public int WallHeight;

    public float EvenWallFactor;
    public float OddWallFactor;

    private DeckGenerator deckGenerator;
    public int[,] Matrix;

    private int oars, nails, compass;
    private int currentRoom = -1;

    public float minX = -1f, minZ = -1f, maxX = float.MinValue, maxZ = float.MinValue;

    void Start()
    {
        deckGenerator = new DeckGenerator();
        while (deckGenerator.GeneratedRooms < 3) deckGenerator.GenerateDeck();

        Matrix = deckGenerator.Matrix;

        DecideItemsRooms();

        float deckX = 0;
        float deckZ = 0;

        for (int y = 0; y < DeckGenerator.Height; y++)
        {
            for (int x = 0; x < DeckGenerator.Width; x++)
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

    private int RandomIntExceptOne(int max, int exception)
    {
        int value = deckGenerator.Random.Next(max - 1);
        return value < exception ? value : value + 1;
    }

    private int RandomIntExceptTwo(int max, int exception1, int exception2)
    {
        int value = deckGenerator.Random.Next(max - 2);
        return value < Mathf.Min(exception1, exception2) ? value : (value < Mathf.Max(exception1, exception2) - 1 ? value + 1 : value + 2);
    }

    private void DecideItemsRooms()
    {
        int generatedRooms = deckGenerator.GeneratedRooms;
        oars = deckGenerator.Random.Next(generatedRooms);
        nails = RandomIntExceptOne(generatedRooms, oars);
        compass = RandomIntExceptTwo(generatedRooms, oars, nails);
    }

    private void InstantiateCeiling(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell == 1)
        {
            if (x % 2 == 0) Instantiate(Ceiling_Thin, new(deckX, WallHeight, deckZ), Quaternion.Euler(0, 0, 0), transform);
            else if (y % 2 == 0) Instantiate(Ceiling_Thin, new(deckX, WallHeight, deckZ), Quaternion.Euler(0, 90, 0), transform);
            else
            {
                GameObject ceiling = ((x - 1) % 4 == 0 ^ (y - 1) % 4 == 0) ? Ceiling_B : Ceiling_A;
                Instantiate(ceiling, new(deckX, WallHeight, deckZ), Quaternion.Euler(0, 0, 0), transform);
            }
        }
    }

    private void InstantiateFloor(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell == 1)
        {
            if (minX == -1f) minX = deckX;
            if (minZ == -1f) minZ = deckZ;
            if (deckX > maxX) maxX = deckX;
            if (deckZ > maxZ) maxZ = deckZ;

            int north = Mathf.Min(Matrix[x, y + 1], 1);
            int east = Mathf.Min(Matrix[x + 1, y], 1);
            int south = Mathf.Min(Matrix[x, y - 1], 1);
            int west = Mathf.Min(Matrix[x - 1, y], 1);
            int sum = north + east + south + west;

            if (x % 2 == 0) Instantiate(Floor_A_Thin, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
            else if (y % 2 == 0) Instantiate(Floor_A_Thin, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
            else
            {
                switch (sum)
                {
                    case 1:
                        if (north == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        else if (east == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        else if (south == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        else if (west == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        break;
                    case 2:
                        if (north == 1)
                        {
                            if (south == 1) Instantiate(Floor_A, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                            else if (east == 1) Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                            else if (west == 1) Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        }
                        else if (east == 1)
                        {
                            if (west == 1) Instantiate(Floor_A, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                            else Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        }
                        else Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        break;
                    case 3:
                        if (north == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        else if (east == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        else if (south == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        else if (west == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        break;
                    case 4:
                        Instantiate(Floor_D, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        break;
                }
            }
        }
    }

    private void InstantiateWalls(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell == 1)
        {
            if (x % 2 == 0)
            {
                Instantiate(Wall_Thin, new(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                Instantiate(Wall_Thin, new(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
            }
            else if (y % 2 == 0)
            {
                Instantiate(Wall_Thin, new(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                Instantiate(Wall_Thin, new(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);
            }
            else
            {
                int north = Matrix[x, y + 1];
                int east = Matrix[x + 1, y];
                int south = Matrix[x, y - 1];
                int west = Matrix[x - 1, y];

                GameObject wall = ((x - 1) % 4 == 0 ^ (y - 1) % 4 == 0) ? Wall_A : Wall_B;

                if (north == 0)
                {
                    if (
                        y == DeckGenerator.Height - 2
                        && (x - 1) % deckGenerator.RoomDistance == 0
                        && x != 1
                        && x != DeckGenerator.Width - 2
                    ) wall = ExitDoor;

                    Instantiate(wall, new(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                }
                else if (north >= 4 && north < 12)
                {
                    int corner = north / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                }

                if (east == 0)
                {
                    if (
                        x == DeckGenerator.Width - 2
                        && (y - 1) % deckGenerator.RoomDistance == 0
                        && y != 1
                        && y != DeckGenerator.Height - 2
                    ) wall = ExitDoor;

                    Instantiate(wall, new(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                }
                else if (east >= 4 && east < 12)
                {
                    int corner = east / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                }

                if (south == 0)
                {
                    if (
                        y == 1
                        && (x - 1) % deckGenerator.RoomDistance == 0
                        && x != 1
                        && x != DeckGenerator.Width - 2
                    ) wall = ExitDoor;

                    Instantiate(wall, new(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
                }
                else if (south >= 4 && south < 12)
                {
                    int corner = south / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
                }

                if (west == 0)
                {
                    if (
                        x == 1
                        && (y - 1) % deckGenerator.RoomDistance == 0
                        && y != 1
                        && y != DeckGenerator.Height - 2
                    ) wall = ExitDoor;

                    GameObject theWall = Instantiate(wall, new(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);

                    if (deckX == 3.75f && deckZ == 71.25f)
                    {
                        GameObject enterDoor = null;
                        foreach(Transform transform in theWall.GetComponentsInChildren<Transform>())
                        {
                            if (transform.CompareTag("Exit"))
                            {
                                enterDoor = transform.gameObject;
                                break;
                            }
                        }

                        GetComponent<EnterDoorContainer>().EnterDoor = enterDoor;
                    }
                }
                else if (west >= 4 && west < 12)
                {
                    int corner = west / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);
                }
            }
        }
    }

    private void InstantiateRoom(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell >= 12)
        {
            currentRoom++;

            cell -= 12;
            int corner = cell / 4;
            int rotation = cell % 4;

            GameObject chosenRoom = corner == 0 ? Room_A : Room_B;
            GameObject room = Instantiate(chosenRoom, new(deckX + 3.75f, 0, deckZ + 3.75f), Quaternion.Euler(0, 90 * rotation, 0), transform);

            foreach (Transform transform in room.GetComponentsInChildren<Transform>())
            {
                if (transform.CompareTag("Collectible"))
                {
                    switch (transform.name)
                    {
                        case "Oars":
                            if (currentRoom != oars) transform.gameObject.SetActive(false);
                            break;
                        case "Nails":
                            if (currentRoom != nails) transform.gameObject.SetActive(false);
                            break;
                        case "Compass":
                            if (currentRoom != compass) transform.gameObject.SetActive(false);
                            break;
                    }
                }
                else if (transform.CompareTag("Globe") && currentRoom == oars) transform.gameObject.SetActive(false);
            }
        }
    }
}
