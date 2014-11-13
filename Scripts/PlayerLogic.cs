using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public class PlayerLogic : Photon.MonoBehaviour
{
	private Vector3 correctPlayerPos;
	private Quaternion correctPlayerRot;
	private Vector3 correctPlayerVel;
	private Vector3 playerMov;

	public PowerUp[] powerUps;
	public int numPowerUps = 0;

	public List<FlagItem> flagItems;

	public float discSpeed = 0f;
	public int playerID;
	public int hasFlagOfPlayer = -1;
	public float initX = 0f;
	public float initZ = 0f;

	public double lastUpdate = 0d;

	public int selectedPowerUpIndex = 0;

	public string clipName;

	public GameObject guiTextureBomb;
	public GameObject guiTextureRocket;
	public GameObject guiTexturePowerUpSelector;
	public GameObject guiTexturePullIn;

	public GameObject playerMarker;
	public GameObject playerSpeech;

	public GameObject speech;

	void Start() {
		lastUpdate = PhotonNetwork.time;
		flagItems = new List<FlagItem>();
		powerUps = new PowerUp[8];
		numPowerUps = 0;

		UpdatePowerUpHUD();
	}
	
	void PlayerScored(int points) {
		GameObject.Find ("Scripts").GetComponent<GameMain> ().PlayerScored (playerID, points);
	}

	void PutFlagItemsInSlotsOnTopOfEachOther() {
		Vector3 pos = this.transform.position;
		
		float cnt = (float)PhotonNetwork.playerList.Length;//flagItems.Count;
		float delta = 2f*Mathf.PI/cnt;
		float cur = 0f;
		Dictionary<int, float> playerIDToIndex = new Dictionary<int, float>();
		Dictionary<int, int> itemsInSlots = new Dictionary<int, int>();
		for (int i = 0; i < PhotonNetwork.playerList.Length; i++) {
			PhotonPlayer player = PhotonNetwork.playerList[i];
			playerIDToIndex.Add (player.ID, (float)i);
			itemsInSlots.Add (player.ID, 0);
		}
		
		int itemsInCurSlot = 0;
		
		List<FlagItem> deleteThese = new List<FlagItem>();
		foreach (FlagItem flag in flagItems) {
			if (flag != null) {
				itemsInSlots.TryGetValue(flag.playerID, out itemsInCurSlot);
				itemsInSlots.Remove(flag.playerID);
				itemsInSlots.Add(flag.playerID, ++itemsInCurSlot);
				playerIDToIndex.TryGetValue(flag.playerID, out cur);
				flag.transform.position = new Vector3(pos.x + 2f * Mathf.Sin ((float)cur*delta), (float)itemsInCurSlot*2f, 
				                                      pos.z + 2f * Mathf.Cos ((float)cur*delta) );
			} else {
				deleteThese.Add (flag);
			}
			cur++;
		}
		
		foreach (FlagItem flag in deleteThese) {
			flagItems.Remove (flag);
		}
	}

	void PutFlagItemsInSlots() {
		Vector3 pos = this.transform.position;
		
		float cnt = (float)flagItems.Count;
		float delta = 2f*Mathf.PI/cnt;
		int cur = 0;
		Vector3 scale = new Vector3(0.15f, 0.15f, 0.15f);
		if (cnt > 5f) {
			scale = new Vector3(0.1f, 0.1f, 0.1f);
		}
		if (cnt > 7f) {
			scale = new Vector3(0.2f/3f, 0.2f/3f, 0.2f/3f);
		}
		List<FlagItem> deleteThese = new List<FlagItem>();
		foreach (FlagItem flag in flagItems) {
			if (flag != null) {
				flag.transform.position = new Vector3(pos.x + 2f * Mathf.Sin ((float)cur*delta), flag.transform.position.y, 
				                                      pos.z + 2f * Mathf.Cos ((float)cur*delta) );
				flag.transform.localScale = scale;
				flag.gameObject.GetComponent<MeshCollider>().enabled = false;
				flag.gameObject.GetComponent<SphereCollider>().enabled = false;

			} else {
				deleteThese.Add (flag);
			}
			cur++;
			
		}
		
		foreach (FlagItem flag in deleteThese) {
			flagItems.Remove (flag);
		}
	}

	[RPC]
	public void CapturedFlagItemRPC(int itemId) {
		GameObject flagItem = GameObject.Find ("PowerUp"+itemId);
		if (flagItem.GetComponent<FlagItem>().capturedByPlayer == -1) {
			flagItem.GetComponent<FlagItem>().capturedByPlayer = this.playerID;
			flagItem.transform.parent = transform;
			flagItems.Add (flagItem.GetComponent<FlagItem>());

			PutFlagItemsInSlots();
		}
	}

	public void CapturedFlagItem(int itemId) {
		photonView.RPC ("CapturedFlagItemRPC", PhotonTargets.All, itemId);
	}

	[RPC]
	public void LostFlagItemRPC(int itemId) {
		Debug.Log ("LostFlagItemRPC: " + itemId + ", playerID: " + playerID);
		GameObject flagItem = GameObject.Find ("PowerUp"+itemId);
		if (flagItem != null) {
			//if (flagItem.GetComponent<FlagItem>().capturedByPlayer == playerID) {
				flagItem.GetComponent<FlagItem>().capturedByPlayer = -1;
				flagItem.transform.parent = null;
				int idx = flagItems.FindIndex (item => item.GetId() == itemId);

				if (idx != -1) {
					flagItems.RemoveAt(idx);
				}
				Debug.Log ("Lost item, now: " + flagItems.Count);
				if (flagItem.GetComponent<FlagItem>() != null) {
					flagItem.GetComponent<FlagItem>().Reset();
				}

				PutFlagItemsInSlots();
			//}
		} else {
			Debug.Log ("flag-item was already destroyed...");
		}
	}

	public void LostFlagItem(FlagItem item) {
		photonView.RPC ("LostFlagItemRPC", PhotonTargets.All, item.id);
	}

	public void LoseRandomFlagItem(int playerId) {
		if (PhotonNetwork.player.ID == playerId) {
			int rand = UnityEngine.Random.Range (0, flagItems.Count - 1);
			LostFlagItem(flagItems.ToArray()[rand]);
		}
	}

	public void LoseAllFlagItems() {
		foreach (FlagItem item in flagItems) {
			LostFlagItem(item);
		}
	}

	[RPC]
	public void PowerUpDestroyedRPC(int itemId) {
		GameObject powerUp = GameObject.Find ("PowerUp"+itemId);
		if (powerUp != null) {
			GameObject.Destroy (powerUp);
		}
		UpdatePowerUpHUD();
	}

	[RPC]
	public void PowerUpCollectedRPC(string type, int itemId) {
		if (numPowerUps < 8) {
			GameObject powerUp = GameObject.Find ("PowerUp"+itemId);
			Type t = Type.GetType(type);
			PowerUp p = (PowerUp) powerUp.GetComponent(t);
			this.powerUps[numPowerUps++] =  p;
			UpdatePowerUpHUD();

			photonView.RPC ("PowerUpDestroyedRPC", PhotonTargets.All, itemId);
		}
	}
	
	public void PowerUpCollected(PowerUp powerUp) {
		photonView.RPC ("PowerUpCollectedRPC", PhotonPlayer.Find(this.playerID), powerUp.GetName(), powerUp.GetId ());
	}

	public void PowerUpDestroyed(int itemId) {
		photonView.RPC ("PowerUpDestroyedRPC", PhotonTargets.All, itemId);
	}

	public void SelectNextPowerUp() {
		int len = numPowerUps;

		if (len > 0) {
			selectedPowerUpIndex = (selectedPowerUpIndex + 1) % len;
		}
		Debug.Log (selectedPowerUpIndex);
		UpdatePowerUpHUD();
	}

	public PowerUp GetSelectedPowerUp() {
		if (numPowerUps > 0) {
			if (selectedPowerUpIndex < numPowerUps) {
				return powerUps[selectedPowerUpIndex];
			}
		}
		return null;
	}

	public PowerUp UseSelectedPowerUp() {
		PowerUp pUp = GetSelectedPowerUp();
		if (pUp != null) {
			for (int i = selectedPowerUpIndex; i < numPowerUps - 1; i++) {
				powerUps[i] = powerUps[i+1];
			}
			--numPowerUps;
			if (selectedPowerUpIndex > 0) {
				--selectedPowerUpIndex;
			}

		}
		UpdatePowerUpHUD();
		return pUp;
	}

	/*
	[RPC]
	public void PowerUpCollectedRPC(int itemId) {
		GameObject powerUp = GameObject.Find ("PowerUp"+itemId);
		if (powerUp != null) {
			GameObject.Destroy (powerUp);
		}
	}

	public void PowerUpCollected(PowerUp powerUp) {
		this.powerUps.Add (powerUp);
		UpdatePowerUpHUD();
		photonView.RPC ("PowerUpCollectedRPC", PhotonTargets.All, powerUp.GetId());
	}
*/
	public void UpdatePowerUpHUD() {
		GameObject[] objs = GameObject.FindGameObjectsWithTag("PowerUpGUITexture");

		for (int i = 0; i < objs.Length; i++) {
			GameObject.Destroy (objs[i]);
		}

		int w = Screen.width;
		int h = Screen.height;
		
		for (int cnt = 0; cnt < numPowerUps; cnt++) {
			PowerUp powerUp = powerUps[cnt];
			GUITexture texture = null;
			if (powerUp.GetName() == "PowerUpBomb") {
				GameObject obj = (GameObject) Instantiate(guiTextureBomb, new Vector3(0f, 0f, 0f), Quaternion.identity);
				texture = obj.GetComponent<GUITexture>();
			}

			if (powerUp.GetName() == "PowerUpRocket") {
				GameObject obj = (GameObject) Instantiate(guiTextureRocket, new Vector3(0f, 0f, 0f), Quaternion.identity);
				texture = obj.GetComponent<GUITexture>();
			}

			if (powerUp.GetName() == "PowerUpPullIn") {
				GameObject obj = (GameObject) Instantiate(guiTexturePullIn, new Vector3(0f, 0f, 0f), Quaternion.identity);
				texture = obj.GetComponent<GUITexture>();
			}

			if (texture != null) {
				Rect rect = new Rect(texture.pixelInset);
				
				rect.x = w - 35;
				rect.y = h - (37*cnt) - 80;
				
				texture.pixelInset = rect;

				if (cnt == selectedPowerUpIndex) {
					GameObject objSel = (GameObject) Instantiate(guiTexturePowerUpSelector, new Vector3(0f, 0f, 0f), Quaternion.identity);
					GUITexture selector = objSel.GetComponent<GUITexture>();

					Rect r = new Rect(selector.pixelInset);

					r.x = w - 62;
					r.y = h - (37*cnt) - 85;

					selector.pixelInset = r;
				}
			}
		}

		int ammo = gameObject.GetComponent<PlayerController>().gunAmmoAmount;
		
		string s = "x " + ammo + "\n";

		GameObject gameObj = GameObject.Find ("AmmoText");
		gameObj.GetComponent<GUIText> ().richText = true;
		gameObj.GetComponent<GUIText> ().text = s;

	}

	bool doPrediction = true;

	// Update is called once per frame
	// TODO: better prediction model... movement relative to last position
	void Update()
	{
		if (Input.GetKeyDown (KeyCode.KeypadEnter)) {
			doPrediction = !doPrediction;
			Debug.Log (doPrediction + " doPred");
		}

		if (photonView.isMine && rigidbody.transform.position.y < -33f) {
			Die ();
		}

		float pingInSeconds = (float)PhotonNetwork.GetPing () * 0.001f;
		float timeSinceLastUpdate = (float)(PhotonNetwork.time - lastUpdate);

		float totalTimePassed = pingInSeconds + timeSinceLastUpdate;

		if (!photonView.isMine)
		{
			if (this.clipName == "Walk") {
				animation["Walk"].speed = 1.5f;
			} 
			if (this.clipName != "") {
				gameObject.GetComponent<Animation>().Play (this.clipName);
			}

			Vector3 extPos;
			if (PhotonNetwork.isMasterClient) {
				extPos = this.correctPlayerPos + this.playerMov * pingInSeconds;
				//extPos = Quaternion.AngleAxis(discSpeed * pingInSeconds, Vector3.up) * this.correctPlayerPos + this.playerMov * pingInSeconds;
			} else {
				extPos = this.correctPlayerPos + this.playerMov * totalTimePassed;
				//extPos = Quaternion.AngleAxis(discSpeed * totalTimePassed, Vector3.up) * this.correctPlayerPos + this.playerMov * totalTimePassed;
			}

			float radius = Vector3.Distance(Vector3.zero, extPos);

			float speed = GetSpeedOfDiscAtRadius(radius);

			if (transform.position.y > 1.0f) {
				speed = 0f;
			}

			if (PhotonNetwork.isMasterClient) {
				//extPos = this.correctPlayerPos + this.playerMov * pingInSeconds;
				extPos = Quaternion.AngleAxis(speed * pingInSeconds, Vector3.up) * this.correctPlayerPos + this.playerMov * pingInSeconds;
			} else {
				//extPos = this.correctPlayerPos + this.playerMov * totalTimePassed;
				extPos = Quaternion.AngleAxis(speed * totalTimePassed, Vector3.up) * this.correctPlayerPos + this.playerMov * totalTimePassed;
			}

			if (doPrediction) {

				transform.position = Vector3.Lerp(transform.position, extPos, Time.deltaTime * 8);
				transform.rotation = Quaternion.Lerp(transform.rotation, correctPlayerRot, Time.deltaTime * 8);
			} else {
				transform.position = Vector3.Lerp(transform.position, this.correctPlayerPos, Time.deltaTime * 8);
				transform.rotation = Quaternion.Lerp(transform.rotation, correctPlayerRot, Time.deltaTime * 8);
			}

			if (Vector3.Distance(transform.position, extPos) > 3f) {
				transform.position = extPos;
			}
		}
	}

	public float GetSpeedOfDiscAtRadius(float radius) {
		GameObject[] objs = GameObject.FindGameObjectsWithTag ("DiscSlice");
		for (int i = 0; i < objs.Length; i++) {
			DiscSlice slice = objs[i].GetComponent<DiscSlice>();
			if (slice.innerRadius < radius && slice.outerRadius > radius) {
				return slice.speed;
			}
		}
		return 0f;
	}

	[RPC]
	public void DieRPC(int playerID) {
		rigidbody.transform.position = new Vector3 (initX, 1f, initZ);
		foreach (FlagItem item in flagItems) {
			item.Reset ();
		}

		flagItems = new List<FlagItem>();
		powerUps = new PowerUp[8];
		numPowerUps = 0;
		selectedPowerUpIndex = 0;

		gameObject.GetComponent<PlayerController>().gunAmmoAmount = 
			Math.Min (gameObject.GetComponent<PlayerController>().gunAmmoAmount, 20);

		if (PhotonNetwork.player.ID == playerID) {
			UpdatePowerUpHUD();
		}
	}

	public void Die() {
		rigidbody.transform.position = new Vector3(initX, 1f, initZ);
		transform.position = new Vector3(initX, 1f, initZ);

		photonView.RPC ("DieRPC", PhotonTargets.All, this.playerID);
	}

	[RPC]
	void SayRPC(string what) {
		speech.GetComponent<PlayerSpeech>().Say(what);
	}
	
	public void Say(string what) {
		photonView.RPC ("SayRPC", PhotonTargets.All, what);
	}

	void OnCollisionEnter(Collision other) {
		if (other.gameObject.tag == "Terrain") {
			this.Die ();
		}
	}

	bool isInited = false;

	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!isInited) {
			Vector3 c = (Vector3)photonView.instantiationData [0];
			playerID = (int)photonView.instantiationData [1];

			gameObject.name = "Player" + playerID;

			Color color = new Color (c.x, c.y, c.z);
			Vector3 p = rigidbody.transform.position;
			GameObject marker = (GameObject) Instantiate (playerMarker,  new Vector3(p.x, p.y + 1.5f * 6f, p.z), Quaternion.identity);
			marker.transform.parent = rigidbody.transform;
			marker.renderer.material.color = color;

			speech = (GameObject) Instantiate (playerSpeech,  new Vector3(p.x, p.y + 2f * 6f, p.z), Quaternion.identity);
			speech.transform.parent = rigidbody.transform;
			speech.GetComponent<TextMesh>().color = color;
			isInited = true;
		}
		PlayerController con = gameObject.GetComponent<PlayerController>();
		if (stream.isWriting)
		{
			// We own this player: send the others our data
			stream.SendNext(transform.position);
			stream.SendNext(transform.rotation);
			stream.SendNext(gameObject.GetComponent<PlayerController>().clipName);
			stream.SendNext(con.mov);
		}
		else
		{
			// Network player, receive data
			this.correctPlayerPos = (Vector3)stream.ReceiveNext();
			this.correctPlayerRot = (Quaternion)stream.ReceiveNext();
			this.clipName = (string)stream.ReceiveNext();
			this.playerMov = (Vector3)stream.ReceiveNext();
			lastUpdate = info.timestamp;
		}
	}
}