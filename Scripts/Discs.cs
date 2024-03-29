﻿using UnityEngine;
using System.Collections;

public class Discs : MonoBehaviour {
	
	public GameObject disc;
	public GameObject bay;

	public int numDiscs = 8;

	public float radius = 35f;

	public Disc[] discs = null;

	public bool isNetworked = true;
	public bool isDemo = false;

	public float avgHoleSize = 0.4f;
	public int avgNumSlices = 5;
	public float maxDiscSpeed = 50f;

	// Use this for initialization
	void Start () {
		createDiscs ();
	}
	
	void createDiscs() {
		float delta = radius / (float) numDiscs;
		discs = new Disc[numDiscs];

		for (int i = 0; i < numDiscs; i++) {
			float curRadiusInner = Mathf.Max (delta * (float) i + 0.1f, 0f);
			float curRadiusOuter = curRadiusInner + delta - 0.1f;
			GameObject discClone = (GameObject) Instantiate(disc, new Vector3(0, 0, 0), Quaternion.identity);
			
			discClone.GetComponent<Disc>().numSlices = Random.Range(Mathf.Max(avgNumSlices - 2, 0), avgNumSlices + 2);

			if (i == 0) {
				discClone.GetComponent<Disc>().numSlices = 1;
			}

			discClone.GetComponent<Disc>().speed = Random.Range(-maxDiscSpeed, maxDiscSpeed);

			discClone.GetComponent<Disc>().innerRadius = curRadiusInner;
			discClone.GetComponent<Disc>().outerRadius = curRadiusOuter;

			discClone.GetComponent<Disc>().avgHoleSize = avgHoleSize;

			discClone.GetComponent<Disc>().discs = this;
			discClone.GetComponent<Disc>().index = i;

			discClone.GetComponent<Disc>().isNetworked = isNetworked;
			discClone.GetComponent<Disc>().isDemo = isDemo;

			discs[i] = (Disc)discClone.GetComponent<Disc>();
		}
	}

	// Update is called once per frame
	void Update () {


	}
}
