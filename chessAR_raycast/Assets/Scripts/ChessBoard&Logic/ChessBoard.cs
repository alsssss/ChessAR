using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Xml.XPath;
using UnityEngine.UIElements;
using ExitGames.Client.Photon;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

public class ChessBoard : MonoBehaviourPunCallbacks,IOnEventCallback
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 0.02f;
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.5f;
    [SerializeField] private float deathSpacing = 0.05f;
    [SerializeField] private float dragOffset = 0.5f;
    [SerializeField] public GameObject victoryScreen;
    [SerializeField] public GameObject victoryScreenClient;
    [SerializeField] public GameObject Warning; 

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials; 

    //LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private int startX;
    private int startY;
    private int endX;
    private int endY;
    private bool updatable = false;
    private float yAngle;
    private Vector3 place;
    private GameObject Winner1;
    private GameObject Winner2;


    void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        if (PhotonNetwork.IsMasterClient) {
            isWhiteTurn = true;
        }
        else
        {
            isWhiteTurn = false;
            RotateBoard();
        }
        PositionAllPieces();

    }

    void Update()
    {
        if (!GameManager.IsPaused) 
        {
            Debug.Log("check");
            if (!currentCamera)
            {
                currentCamera = Camera.main;
                return;
            }

            if (updatable)
            {
                UpdateBoard();
                updatable = false;
            }


            RaycastHit info;
            Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out info, 1000, LayerMask.GetMask("Tile", "Hover", "Highlight")))
            {
                //Get the indexes of the tile i've hit
                Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                //If we're hovering a tile after not havering any tile before
                if (currentHover == -Vector2Int.one && Input.anyKeyDown)
                {
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }

                // If we were already hovering a tile
                if (currentHover != hitPosition && Input.anyKeyDown)
                {

                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }

                //If we press down on the mouse
                if (Input.GetMouseButtonDown(0))
                {
                    if (chessPieces[hitPosition.x, hitPosition.y] != null)
                    {
                        if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && IsYourPiece(chessPieces[hitPosition.x, hitPosition.y])) ||
                           (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && IsYourPiece(chessPieces[hitPosition.x, hitPosition.y])))
                        {
                            currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                            availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                            specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                            PreventCheck();
                            HiglightTiles();
                            byte EventCode = 1;
                            SignalEvent(hitPosition.x, hitPosition.y, EventCode);
//                            photonView.RPC("MovePieceFROM_RPC", RpcTarget.Others, hitPosition.x, hitPosition.y);*/
                        }
                    }

                }

                //If we are releasing the mouse button
                if (currentlyDragging != null && Input.GetMouseButtonUp(0))
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    if (!validMove)
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                    RemoveHiglightTiles();
                }

            }
            else
            {
                if (currentHover != -Vector2Int.one)
                {
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                    currentHover = -Vector2Int.one; 
                }

                if (currentlyDragging && Input.GetMouseButtonUp(0))
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                    currentlyDragging = null;
                    RemoveHiglightTiles();
                }
            }

            //If we're dragging a piece
            if (currentlyDragging)
            {
                Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                float distance = 0.0f;
                if (horizontalPlane.Raycast(ray, out distance))
                    currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset * tileSize);
            }

        }

        if (GameManager.IsRestarted && PhotonNetwork.IsMasterClient)
        {
            OnResetButton();
        }

    }
    private void UpdateBoard()
    {
        Debug.Log("The picked piece is: " + chessPieces[startX, startY].type + " moved from (" + startX + "," + startY + ") to (" + endX + "," + endY + ")");
        if (chessPieces[startX, startY] != null)
        { 
            MoveToModified(chessPieces[startX, startY], endX, endY);
        }
    }


    private bool IsYourPiece(ChessPiece cp) {
        //RIC:  masterClient ha i bianhchi, l'altro ha i neri
        int whiteTeam = 0, blackTeam = 1;
        if (PhotonNetwork.IsMasterClient && cp.GetComponent<MeshRenderer>().material.color == teamMaterials[whiteTeam].color)
        {
            return true;
        }
        else if (!PhotonNetwork.IsMasterClient && cp.GetComponent<MeshRenderer>().material.color == teamMaterials[blackTeam].color)
        {
            return true;

        }
        else
            return false;
    }
    



    //Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        //  Vector3 boardCenter = GameObject.Find("ARRaycastPlace").GetComponent<ARRaycastPlace>().getposition();
        
        Vector3 truePos = GameObject.Find("ARRaycastPlace").GetComponent<ARRaycastPlace>().p;
        float yp = truePos.y;
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 direction = cameraPosition - truePos;
        Vector3 targetRotationEuler = Quaternion.LookRotation(direction).eulerAngles;
        Vector3 scaledEuler = Vector3.Scale(targetRotationEuler, Vector3.up);
        float rot = (Mathf.PI) - Mathf.Deg2Rad * scaledEuler.y;
        yAngle = rot;
        place=truePos;
        Vector3 newCentre = (new Vector3(boardCenter.x * Mathf.Cos(rot) - boardCenter.z * Mathf.Sin(rot), 0, boardCenter.x * Mathf.Sin(rot) + boardCenter.z * Mathf.Cos(rot)) * tileSize);
        //Vector3 tester = new Vector3((t * Mathf.Cos(rot) - g * Mathf.Sin(rot)) * tileSize, 0, (t * Mathf.Sin(rot) + g * Mathf.Cos(rot)) * tileSize);
        yOffset = (yOffset)*tileSize+yp;
        bounds = new Vector3(((tileCountX / 2)* Mathf.Cos(rot) - (tileCountX / 2) * Mathf.Sin(rot)) * tileSize,0, ((tileCountX / 2) * Mathf.Sin(rot) + (tileCountX / 2) * Mathf.Cos(rot)) * tileSize) + newCentre;

        tiles = new GameObject[tileCountX, tileCountY];

        for(int x = 0; x < tileCountX; x++)
            for(int y = 0; y < tileCountY; y++)
                tiles[x,y] = GenerateSingleTile(tileSize, x, y);
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x,y));
        Vector3 truePos = GameObject.Find("ARRaycastPlace").GetComponent<ARRaycastPlace>().p;
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 direction = cameraPosition - truePos;
        Vector3 targetRotationEuler = Quaternion.LookRotation(direction).eulerAngles;
        Vector3 scaledEuler = Vector3.Scale(targetRotationEuler, Vector3.up);
        float rot = (Mathf.PI)-Mathf.Deg2Rad*scaledEuler.y;
        tileObject.transform.parent = transform;
   

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

 
        Vector3[] vertices = new Vector3[4];

        vertices[0] = new Vector3(truePos.x + (x * Mathf.Cos(rot) - y * Mathf.Sin(rot)) * tileSize, yOffset, truePos.z + (x * Mathf.Sin(rot) + y * Mathf.Cos(rot)) * tileSize) - bounds;

        vertices[1] = new Vector3(truePos.x + (x * Mathf.Cos(rot) - (y + 1) * Mathf.Sin(rot)) * tileSize, yOffset, truePos.z + ((x) * Mathf.Sin(rot) + (y + 1) * Mathf.Cos(rot)) * tileSize)- bounds;

        vertices[2] = new Vector3(truePos.x + ((x + 1) * Mathf.Cos(rot) - y * Mathf.Sin(rot)) * tileSize, yOffset, truePos.z + ((x + 1) * Mathf.Sin(rot) + y * Mathf.Cos(rot)) * tileSize)- bounds;

        vertices[3] = new Vector3(truePos.x + ((x + 1) * Mathf.Cos(rot) - (y + 1) * Mathf.Sin(rot)) * tileSize, yOffset, truePos.z + ((x + 1) * Mathf.Sin(rot) + (y + 1) * Mathf.Cos(rot)) * tileSize)- bounds;


        int[] tris = new int[] {0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }


    //Spawing of the pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        //White team;
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        //Black team;
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);


    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type -1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        if (cp.team == 0)
        {
            cp.transform.Rotate(Vector3.up, 180f);
        }
        
        return cp;
    }

    //Modify the board for the connecter player
    private void RotateBoard()
    {
        int whiteTeam = 0, blackTeam = 1;

        ChessPiece dummy1 = chessPieces[3, 0];
        ChessPiece dummy2 = chessPieces[3, 7];
        chessPieces[3, 0] = chessPieces[4, 0];
        chessPieces[3, 7] = chessPieces[4, 7];
        chessPieces[4, 0] = dummy1;
        chessPieces[4, 7] = dummy2;
        chessPieces[0, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        chessPieces[1, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        chessPieces[2, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        chessPieces[3, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        chessPieces[4, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        chessPieces[5, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        chessPieces[6, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        chessPieces[7, 0].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1].GetComponent<MeshRenderer>().material = teamMaterials[blackTeam];

        //Black team;
        chessPieces[0, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];
        chessPieces[1, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam]; 
        chessPieces[2, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];
        chessPieces[3, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];
        chessPieces[4, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];
        chessPieces[5, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];
        chessPieces[6, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];
        chessPieces[7, 7].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 6].GetComponent<MeshRenderer>().material = teamMaterials[whiteTeam];

    }

    //Positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x,y] != null)
                    PositionSinglePiece(x,y, true);
    }
    
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x,y].currentX = x;
        chessPieces[x,y].currentY = y;
        chessPieces[x,y].SetPosition(GetTileCenter(x,y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {

        return new Vector3(place.x + (x * Mathf.Cos(yAngle) - y * Mathf.Sin(yAngle)) * tileSize, yOffset, place.z + (x * Mathf.Sin(yAngle) + y * Mathf.Cos(yAngle)) * tileSize);
    }

    // Highlight Tiles
    private void HiglightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y]. layer = LayerMask.NameToLayer("Highlight");
    }
    private void RemoveHiglightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y]. layer = LayerMask.NameToLayer("Tile");
        availableMoves.Clear();
    }

    //Checkmate
    public void CheckMate(int team)
    {
        DisplayVictory(team);
    }

    public void DisplayVictory(int winningTeam)
    {
        GameObject canvas = GameObject.FindGameObjectWithTag("Ending");
        if (PhotonNetwork.IsMasterClient)
        {
            byte EventCode = 4;
            Winner1 = Instantiate(victoryScreen, canvas.transform);
            
            if(winningTeam == 0) { Winner1.transform.GetChild(0).gameObject.SetActive(true); }
            else { Winner1.transform.GetChild(1).gameObject.SetActive(true); }
            SignalEnd(EventCode , winningTeam);
        }
        else 
        {
            byte EventCode = 5;
            Winner2 = Instantiate(victoryScreenClient, canvas.transform);

            if (winningTeam == 0) { Winner2.transform.GetChild(0).gameObject.SetActive(true); }
            else { Winner2.transform.GetChild(1).gameObject.SetActive(true); }
            SignalEnd(EventCode, winningTeam);
        }
    }

    public void OnResetButton()
    {
        //UI
            byte EventCode = 3;
            Destroy(Winner1);
            SignalRestart(EventCode);
            ARRaycastPlace reset = FindObjectOfType<ARRaycastPlace>();
            reset.Restart();
            Destroy(gameObject);
            GameManager.IsRestarted = false;
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    //Special Moves
    private void ProcessSpecialMove()
    {
        if(specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if(myPawn.currentX == enemyPawn.currentX)
            {
                if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if(enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3((8* Mathf.Cos(yAngle) + 1 * Mathf.Sin(yAngle)) * tileSize, yOffset,(-1 * Mathf.Sin(yAngle) + 8 * Mathf.Cos(yAngle)) * tileSize) 
                            - bounds 
                            + new Vector3(place.x + (tileSize/2 * Mathf.Cos(yAngle) - tileSize/2 * Mathf.Sin(yAngle)) * tileSize,0, place.z + (tileSize/2 * Mathf.Sin(yAngle) + tileSize/2 * Mathf.Cos(yAngle)) * tileSize) 
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3((-1 * Mathf.Cos(yAngle) - 8 * Mathf.Sin(yAngle)) * tileSize, yOffset, (8 * Mathf.Sin(yAngle) - 1 * Mathf.Cos(yAngle)) * tileSize) 
                            - bounds 
                            + new Vector3(place.x + (tileSize / 2 * Mathf.Cos(yAngle) - tileSize / 2 * Mathf.Sin(yAngle)) * tileSize, 0, place.z + (tileSize / 2 * Mathf.Sin(yAngle) + tileSize / 2 * Mathf.Cos(yAngle)) * tileSize) 
                            + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }

        if(specialMove == SpecialMove.Castling)
        {
            var lastMove = moveList[moveList.Count - 1];

            // Left rook
            if(lastMove[1].x == 2)
            {
                if(lastMove[1].y == 0) //White side
                {
                    ChessPiece rook = chessPieces[0,0];
                    chessPieces[3,0] = rook;
                    PositionSinglePiece(3,0);
                    chessPieces[0,0] = null;
                }
                else if(lastMove[1].y == 7) //Black side
                {
                    ChessPiece rook = chessPieces[0,7];
                    chessPieces[3,7] = rook;
                    PositionSinglePiece(3,7);
                    chessPieces[0,7] = null;
                }
            }

            //Right Rook
            else if(lastMove[1].x == 6)
            {
                if(lastMove[1].y == 0) //White side
                {
                    ChessPiece rook = chessPieces[7,0];
                    chessPieces[5,0] = rook;
                    PositionSinglePiece(5,0);
                    chessPieces[7,0] = null;
                }
                else if(lastMove[1].y == 7) //Black side
                {
                    ChessPiece rook = chessPieces[7,7];
                    chessPieces[5,7] = rook;
                    PositionSinglePiece(5,7);
                    chessPieces[7,7] = null;
                }
            }
        }

        if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if(targetPawn.type == ChessPieceType.Pawn)
            {
                if(targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
                if(targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }
    }

    private void PreventCheck()
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x,y] != null)
                    if(chessPieces[x,y].type == ChessPieceType.King)  
                        if(chessPieces[x,y].team == currentlyDragging.team)
                            targetKing = chessPieces[x,y];  
        
        //Since we're sending ref availableMoves, we will be deleting moves that are puttin us in check
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        // Save the current values, to reset after the function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //Going through all the moves, simulate them and check if wr're in check
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            //DId we simulate the king's move
            if(cp.type == ChessPieceType.King)
                kingPositionThisSim = new Vector2Int(simX, simY);
            
            //Copy the [,] and not the reference
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackinPieces = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if(chessPieces[x,y] != null)
                    {
                        simulation[x,y] = chessPieces[x,y];
                        if(simulation[x,y].team != cp.team)
                            simAttackinPieces.Add(simulation[x,y]);
                    }
                }
            }

            //Simulate that move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            // Did one of the piece got taken down during simulation
            var deadPiece = simAttackinPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if(deadPiece != null)
                simAttackinPieces.Remove(deadPiece);

            // Get all the simulated attacking pieces moves
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackinPieces.Count; a++)
            {
                var pieceMoves = simAttackinPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    simMoves.Add(pieceMoves[b]);
                }
            }

            //Is the king in trouble? if so, remove the move
            if(ContainsValidMove(ref simMoves, kingPositionThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            //Restore the actual CP data
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        // Remove from the current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
            moves.Remove(movesToRemove[i]);

    }
    
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;


        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x,y] != null)
                {
                    if(chessPieces[x,y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x,y]);
                        if(chessPieces[x,y].type == ChessPieceType.King)
                            targetKing = chessPieces[x,y];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x,y]);
                    }
                }
        
        //Is the king attacked right now? 
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                    currentAvailableMoves.Add(pieceMoves[b]);
        }

        //Are we in check right now?
        if(ContainsValidMove(ref currentAvailableMoves, new  Vector2Int(targetKing.currentX, targetKing. currentY)))
        {
            //King is under attack, can we move something to help him?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                //Delete move that will put us in check        
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if(defendingMoves.Count != 0)
                    return false;
            }
            return true; //Checkmate exit
        }


        return false;
    }

    //Operation
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if(moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
        
    }

    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y))) {
            Debug.Log("Non è una mossa valida");
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        //Is there another piece on target position
        if(chessPieces[x,y] != null)
        {
            ChessPiece ocp = chessPieces[x,y];

            if(cp.team == ocp.team)
            {
                return false;
            }

            //If its the enemy team
            if(ocp.team == 0)
            {
                if(ocp.type == ChessPieceType.King)
                    CheckMate(1);
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3((8 * Mathf.Cos(yAngle) + 1 * Mathf.Sin(yAngle)) * tileSize, yOffset, (-1 * Mathf.Sin(yAngle) + 8 * Mathf.Cos(yAngle)) * tileSize)
                    - bounds 
                    + new Vector3(place.x + (tileSize / 2 * Mathf.Cos(yAngle) - tileSize / 2 * Mathf.Sin(yAngle)) * tileSize, 0, place.z + (tileSize / 2 * Mathf.Sin(yAngle) + tileSize / 2 * Mathf.Cos(yAngle)) * tileSize) 
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if(ocp.type == ChessPieceType.King)
                    CheckMate(0);
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3((-1 * Mathf.Cos(yAngle) - 8 * Mathf.Sin(yAngle)) * tileSize, yOffset, (8 * Mathf.Sin(yAngle) - 1 * Mathf.Cos(yAngle)) * tileSize) 
                - bounds 
                + new Vector3(place.x + (tileSize / 2 * Mathf.Cos(yAngle) - tileSize / 2 * Mathf.Sin(yAngle)) * tileSize, 0, place.z + (tileSize / 2 * Mathf.Sin(yAngle) + tileSize / 2 * Mathf.Cos(yAngle)) * tileSize) 
                + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x,y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x,y);

        isWhiteTurn = !isWhiteTurn;
        moveList.Add(new Vector2Int[] {previousPosition, new Vector2Int(x,y)});

        ProcessSpecialMove();

        if(CheckForCheckmate())
            CheckMate(cp.team);
        byte EventCode = 2;
        SignalEvent(x, y, EventCode);
//        photonView.RPC("MovePieceTO_RPC", RpcTarget.Others, x, y);
        return true;
    }

    private bool MoveToModified(ChessPiece cp, int x, int y)
    {
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        //Is there another piece on target position
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team)
            {
                return false;
            }

            //If its the enemy team
            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(1);

                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3((8 * Mathf.Cos(yAngle) + 1 * Mathf.Sin(yAngle)) * tileSize, yOffset, (-1 * Mathf.Sin(yAngle) + 8 * Mathf.Cos(yAngle)) * tileSize)
                    - bounds
                    + new Vector3(place.x + (tileSize / 2 * Mathf.Cos(yAngle) - tileSize / 2 * Mathf.Sin(yAngle)) * tileSize, 0, place.z + (tileSize / 2 * Mathf.Sin(yAngle) + tileSize / 2 * Mathf.Cos(yAngle)) * tileSize)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(0);
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3((-1 * Mathf.Cos(yAngle) - 8 * Mathf.Sin(yAngle)) * tileSize, yOffset, (8 * Mathf.Sin(yAngle) - 1 * Mathf.Cos(yAngle)) * tileSize)
                    - bounds
                    + new Vector3(place.x + (tileSize / 2 * Mathf.Cos(yAngle) - tileSize / 2 * Mathf.Sin(yAngle)) * tileSize, 0, place.z + (tileSize / 2 * Mathf.Sin(yAngle) + tileSize / 2 * Mathf.Cos(yAngle)) * tileSize)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        ProcessSpecialMove();

        if (CheckForCheckmate())
            CheckMate(cp.team);

        return true;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(tiles[x,y] == hitInfo)
                    return new Vector2Int(x,y);

        return -Vector2Int.one; // Invalid
    }

    public void SignalEvent(int targetX, int targetY , byte id) 
    {
        object[] data = new object[]
        {
            targetX,targetY
        };

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };

        SendOptions sendOptions = new SendOptions
        {
            Reliability = true
        };

        PhotonNetwork.RaiseEvent(id, data, raiseEventOptions, sendOptions);
    }

    public void SignalRestart(byte id)
    {
        object[] data = new object[]
        {
        };

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };

        SendOptions sendOptions = new SendOptions
        {
            Reliability = true
        };

        PhotonNetwork.RaiseEvent(id, data, raiseEventOptions, sendOptions);
    }

    public void SignalEnd(byte id, int team)
    {
        object data = team;

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };

        SendOptions sendOptions = new SendOptions
        {
            Reliability = true
        };

        PhotonNetwork.RaiseEvent(id, data, raiseEventOptions, sendOptions);
    }


    public void OnEvent(EventData incomingEvent)
    {
        object[] data = (object[]) incomingEvent.CustomData;
        
        if (incomingEvent.Code == 1) 
        {
            startX = 7 - (int) data[0];
            startY = 7 - (int) data[1];
            Debug.LogWarning("Event Received: MovePieceFROM_RPC(" + startX + ", " + startY + ")");
        };
        
        if (incomingEvent.Code == 2) 
        {
            endX = 7 - (int) data[0];
            endY = 7 - (int) data[1];
            updatable = true;
            Debug.LogWarning("Event Received: MovePieceTO_RPC(" + endX + ", " + endY + ")");
        };
        
        if (incomingEvent.Code == 3)
        {
            GameObject canvas = GameObject.FindGameObjectWithTag("Ending");
            Destroy(Winner2);
            GameObject Message = Instantiate( Warning, canvas.transform);
            Destroy(Message, 3f);
            ARRaycastPlace reset = FindObjectOfType<ARRaycastPlace>();
            reset.Restart();
            Destroy(gameObject);
        };
        
        if (incomingEvent.Code == 4)
        {
            GameObject canvas = GameObject.FindGameObjectWithTag("Ending");
            Winner2 = Instantiate(victoryScreenClient, canvas.transform);

            if ((int) data[0] == 0) { Winner2.transform.GetChild(0).gameObject.SetActive(true); }
            else { Winner2.transform.GetChild(1).gameObject.SetActive(true); }
        };

        if (incomingEvent.Code == 5)
        {
            GameObject canvas = GameObject.FindGameObjectWithTag("Ending");
            Winner1 = Instantiate(victoryScreen, canvas.transform);

            if ((int)data[0] == 0) { Winner1.transform.GetChild(0).gameObject.SetActive(true); }
            else { Winner1.transform.GetChild(1).gameObject.SetActive(true); }
        };

        if (incomingEvent.Code == 0) { Debug.LogWarning("Faulty event detected"); }
    }
    override public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    override public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
