//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.34014
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using UnityEngine;

public class PlayerStats
{
	public int playerID;
	public int score = 0;
	public int matchWins = 0;
	public string name = "";
	public Color color;
	
	public PlayerStats (int playerID, int score, string name, Color color)
	{
		this.playerID = playerID;
		this.score = score;
		this.name = name;
		this.color = color;
	}
}

