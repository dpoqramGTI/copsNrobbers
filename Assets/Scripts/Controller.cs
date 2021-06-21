using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        //int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //TODO: Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        //
        onesBoard64x64();
    }

    private void onesBoard64x64()
    {
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                // Casos limites (faltan las esquinas)

                // Esquina arriba izquierda
                if (i == Constants.NumTiles - 8)
                {
                    if (i == j - 1 || i == j + 8)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Esquina arriba derecha
                else if (i == Constants.NumTiles - 1)
                {
                    if (i == j + 1 || i == j + 8)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Esquina abajo izquierda
                else if (i == 0)
                {
                    if (i == j - 1 || i == j - 8)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Esquina abajo derecha
                else if (i == Constants.TilesPerRow - 1)
                {
                    if (i == j + 1 || i == j - 8)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Limite por la izquierda % 8 == 0
                else if (i % 8 == 0)
                {
                    if (i == j - 1 || i == j - 8 || i == j + 8)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Limite por arriba
                else if (i < Constants.NumTiles && i >= Constants.NumTiles - 8)
                {
                    if (i == j + 1 || i == j - 1 || i == j + 8)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Limite por la derecha % 8 == 0
                else if (checkRightSide(i))
                {
                    if (i == j + 1 || i == j - 8 || i == j + 8)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Limite por bajo
                else if (i < 8 && i >= 0)
                {
                    if (i == j + 1 || i == j - 8 || i == j - 1)
                    {
                        matriu[i, j] = 1;
                    }
                }
                // Para poner 1 en las adyacentes arriba abajo izquierda derecha
                else if (i == j + 1 || i == j - 1 || i == j - 8 || i == j + 8)
                {
                    matriu[i, j] = 1;
                }

            }
        }
    }

    private bool checkRightSide(int i)
    {
        for (int e = 1; e <= Constants.TilesPerRow; e++)
        {
            if (i == (8 * e) - 1)
            {
                return true;
            }
        }
        return false;
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;
                ResetTiles();
                FindSelectableTiles(true);
                copCollisionHandle(cop_id);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        /*TODO: Cambia el código de abajo para hacer lo siguiente
        - Elegimos una casilla aleatoria entre las seleccionables que puede ir el caco
        - Movemos al caco a esa casilla
        - Actualizamos la variable currentTile del caco a la nueva casilla
        */

        //setSelectableTiles(clickedTile);
        int randomTile = getRandomSelectableTileNumber();
        robber.GetComponent<RobberMove>().MoveToTile(tiles[randomTile]);
        robber.GetComponent<RobberMove>().currentTile = randomTile;
    }

    private int getRandomSelectableTileNumber()
    {
        List<int> selectableTiles = new List<int>();
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (tiles[i].selectable)
            {
                selectableTiles.Add(i);
            }
        }
        System.Random rnd = new System.Random();
        int randomIndex = rnd.Next(0, selectableTiles.Count - 1);
        return selectableTiles[randomIndex];
    }
    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop == true)
        {
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        }
        else
        {
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;
        }

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        //TODO: Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        //Tendrás que cambiar este código por el BFS
        setSelectableTiles(indexcurrentTile);

        // Evito que pase por donde esta el otro policia
        int cop1 = cops[0].GetComponent<CopMove>().currentTile;
        int cop2 = cops[1].GetComponent<CopMove>().currentTile;
        tiles[cop1].selectable = false;
        tiles[cop2].selectable = false;
        //tiles[indexcurrentTile].selectable = false;
    }
    //Controlo que no pueda pasar con una ficha atraves de la otra
    private void copCollisionHandle(int cop_id)
    {
        int cop1 = cops[0].GetComponent<CopMove>().currentTile;
        int cop2 = cops[1].GetComponent<CopMove>().currentTile;

        if (cop_id == 0)
        {
            if (cop1 + 1 == cop2)
            {
                tiles[cop1 + 2].selectable = false;
            }
            else if (cop1 - 1 == cop2)
            {
                tiles[cop1 - 2].selectable = false;
            }
            else if (cop1 + 8 == cop2)
            {
                tiles[cop1 + 16].selectable = false;
            }
            else if (cop1 - 8 == cop2)
            {
                tiles[cop1 - 16].selectable = false;
            }
        }
        else if (cop_id == 1)
        {
            if (cop2 + 1 == cop1)
            {
                tiles[cop2 + 2].selectable = false;
            }
            else if (cop2 - 1 == cop1)
            {
                tiles[cop2 - 2].selectable = false;
            }
            else if (cop2 + 8 == cop1)
            {
                tiles[cop2 + 16].selectable = false;
            }
            else if (cop2 - 8 == cop1)
            {
                tiles[cop2 - 16].selectable = false;
            }
        }


    }

    public void cleanSelectableTiles()
    {
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].selectable = false;
        }
    }
    public void setSelectableTiles(int currentTile)
    {
        cleanSelectableTiles();
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (matriu[currentTile, i] == 1)
            {
                tiles[i].selectable = true;
                setAdyacentTilesSelected(i);
            }
        }
    }

    public void setAdyacentTilesSelected(int tile)
    {
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (matriu[tile, i] == 1)
            {
                tiles[i].selectable = true;
            }
        }
    }





}
