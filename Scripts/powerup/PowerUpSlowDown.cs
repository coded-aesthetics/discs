﻿using UnityEngine;
using System.Collections;

public class PowerUpSlowDown : PowerUpBase {

	public GameObject spellCollider;

	public override string GetName() {
		return "PowerUpSlowDown";
	}
	
	public override void Activate(GameObject player, Vector3 pos) {
		GameObject obj = (GameObject) Instantiate (spellCollider, pos, Quaternion.identity);
		obj.GetComponent<SpellCollider>().castingPlayerID = PhotonNetwork.player.ID;
		obj.GetComponent<SpellCollider>().spellName = "SpellSlowDown";
		SpellSlowDown spell = new SpellSlowDown();
		spell.rate = 0.5f;
		obj.GetComponent<SpellCollider>().spell = spell;
	}
}