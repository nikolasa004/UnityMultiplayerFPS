using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public MatchSettings matchSettings;
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Ejo više game menadzera ejo!");
        }
        else
        {
            instance = this;
        }
    }
    #region Player tracking
    private static Dictionary<string, PlayerManager> players = new Dictionary<string, PlayerManager>();
    public static void RegisterPlayer(string net_ID, PlayerManager player) //Registruje igraca
    {
        string player_ID = "Player " + net_ID;  //Sastavi se ime od rednog broja
        players.Add(player_ID, player);  //Ubaci se u dictionary
        player.transform.name = player_ID;  //Da mu se takvo ime
    }
    public static void UnRegisterPlayer(string playerID)
    {
        players.Remove(playerID);  //Izbaci se iz dictionary
    }
    public static PlayerManager GetPlayer(string playerID)
    {
        return players[playerID];
    }

    public static IEnumerable<PlayerManager> GetPlayers()
    {
        return players.Values;
    }
    #endregion
}
