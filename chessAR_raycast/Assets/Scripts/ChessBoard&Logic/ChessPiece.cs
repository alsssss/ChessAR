using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ChessPieceType
{
    None=0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}

public class ChessPiece : MonoBehaviour{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    void Awake()
    {
        Vector3 truePos = GameObject.Find("ARRaycastPlace").GetComponent<ARRaycastPlace>().p;
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 direction = cameraPosition - truePos;
        Vector3 targetRotationEuler = Quaternion.LookRotation(direction).eulerAngles;
        Vector3 scaledEuler = Vector3.Scale(targetRotationEuler, Vector3.up);
        float rot = scaledEuler.y;
        transform.rotation = Quaternion.Euler((team == 0) ? new Vector3(0,rot,0)  : new Vector3(0,rot+180,0));
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(3,3));
        r.Add(new Vector2Int(3,4));
        r.Add(new Vector2Int(4,3));
        r.Add(new Vector2Int(4,4));

        return r;
    }

    public virtual SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        return SpecialMove.None;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if(force)
        {
            transform.position = desiredPosition;
        }
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if(force)
        {
            transform.localScale = desiredScale;
        }
    }

    public virtual void ChangePieceAttributes(ChessPieceType types, int t)
    {
        team = t;
        type = types;
    }

    public void ResetScale(bool force)
    {
        if (force) { desiredScale = Vector3.one; }
        
    }
}
