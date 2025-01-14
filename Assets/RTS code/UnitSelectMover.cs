﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Lab4;

/**
 * This class deals selecting units and unit groups
 * The selection code is clunky and has some redundant parts as it evolved organically from a different example.
 * I haven't had the time to rewrite it yet, but I'll clean it up eventiually.
 * We'll create more intelligent unit movement behaviours later
 */ 

namespace Lab4 {
	public class UnitSelectMover : MonoBehaviour {

		public LayerMask mask;						// Mask for the raycast placement
		public Transform target;					// Target destination
		private List<AIWithPathfinding> selectedAI; // List of selected units
		private Vector3 start_box;					//Start position for selection box
		private Vector3 end_box;					//End position for selection box
		public Texture selectionImage;				//Image for selection box
		private Rect boundbox;						//Bounding box for the units in 3D world coordinates
		private Rect screenBox;						//Bounding box for the units in screen coordinates
		private bool drawBox = false;				//Flag for drawing the box
		private bool readyToPatrol = false;
		private bool readyToAttack = false;

		// This variable will store the location of wherever we first click before dragging.
		
		/** Determines if the target position should be updated every frame or only on double-click */
		
		Camera cam;
		
		public void Start () {
			//Cache the Main Camera
			cam = Camera.main;
			selectedAI = new List<AIWithPathfinding>();
		}

		public bool HasSelected(){
			return selectedAI.Count > 0;
		}

		public Vector3? GetGroupPos(){
			UpdateSelection();
			if (selectedAI.Count > 0){
				Vector3 centrePos = Vector3.zero;

				foreach (AIWithPathfinding ai in selectedAI){
					centrePos += ai.transform.position;
				}
				centrePos /= selectedAI.Count;
				return centrePos;
			}else{
				return null;
			}
		}

		public void OnGUI () {

			if (start_box != -Vector3.one)
			{
				GUI.color = new Color(1, 1, 1, 0.25f);
				if (drawBox)
					GUI.DrawTexture(screenBox, selectionImage);
				//GUI.Box(screenBox, "Selection");
			}
	
			//If the user releases the right mouse button
			if (cam != null && Input.GetMouseButtonUp(1)) {
				//...remove dead units from the selection group
				UpdateSelection();
				//...and move the selected units to that position
				MoveUnit();
			//If left mound button up
			}else if (cam != null && Input.GetMouseButtonUp(0) && !readyToPatrol && !readyToAttack) {
				//Select the unit(s)
				SelectUnit();
				//and disable the selection box
				drawBox = false;
			}
		}


		//Select a unit or deselect all units
		public void SelectUnit() {
			RaycastHit hit;
			//Cast a ray
			if (Physics.Raycast	(cam.ScreenPointToRay (Input.mousePosition), out hit, Mathf.Infinity, mask) && hit.point != target.position) {
				string hitMask = LayerMask.LayerToName(hit.transform.gameObject.layer);
				//if ground is hit, deselect all units
				if (hitMask == "Ground"){
					SelectAll(false);
					selectedAI.Clear();
				//Otherwise select the unit that was clicked on
				}else if (hitMask == "Unit" ) {
					SelectAll(false);
					selectedAI.Clear();
					AIWithPathfinding ai = hit.transform.GetComponent<AIWithPathfinding>();
					//ai.target = target;
					ai.selected = true;
					selectedAI.Add(ai);
					SelectAll(true);
					print ("Object selected");
				}
			}

		}

		//Move selected units
		public void MoveUnit () {
			//Fire a ray through the scene at the mouse position and place the target where it hits
			RaycastHit hit;
			//Cast a ray
			if (Physics.Raycast	(cam.ScreenPointToRay (Input.mousePosition), out hit, Mathf.Infinity, mask) && hit.point != target.position) {
				string hitMask = LayerMask.LayerToName(hit.transform.gameObject.layer);
				print ("Hit "+hit.transform.name+" from layer "+hitMask);
				//If the ray hit the groud, move all selected units
				if (hitMask == "Ground" && selectedAI.Count != 0){
					target.position = hit.point;
					Transform[] targets = CalculateGroupPositions(target.position);
					for (int i = 0; i < selectedAI.Count; i++){
						////selectedAI[i].target = targets[i];
						selectedAI[i].SetTarget(targets[i]);
					//	selectedAI[i].target = target;
						selectedAI[i].FindPath();
						selectedAI[i].IssueMoveCommand();
					}
				}
			}
		}

		/**
		 * Move a group:
		 * - Calculate new positions
		 * - Set position for each unit
		 */

		/**
		 * Calculate positions:
		 * - Calculate centre point
		 * - For every unit, calculate position relative to centre: relP
		 * - For every unit, claculate position target+relP
		 */



		Transform[] CalculateGroupPositions(Vector3 pos){
			UpdateSelection();
			if (selectedAI.Count == 0)
				return null;

			//Calculate centre point
			Vector3 centrePos = Vector3.zero;
			Vector3[] offsets = new Vector3[selectedAI.Count];

			foreach (AIWithPathfinding ai in selectedAI){
				centrePos += ai.transform.position;
			}
			centrePos /= selectedAI.Count;
			Debug.DrawRay(centrePos,10*Vector3.up,Color.cyan,10);

			//For every unit, calculate position relative to centre: relP
			Vector3[] relPos = new Vector3[selectedAI.Count];
			for (int i = 0; i < selectedAI.Count; i++){
				relPos[i] = selectedAI[i].transform.position - centrePos;

			}

			//For every unit, claculate position target+relP
			Transform[] newTargets = new Transform[selectedAI.Count];
			for (int i = 0; i < selectedAI.Count; i++){
				GameObject marker = (GameObject)Instantiate(target.gameObject, pos + relPos[i], transform.rotation);
				newTargets[i] = marker.transform;
				newTargets[i].gameObject.name = "TempTarget";
				//Destroy(marker, 5);
			}

			return newTargets;
		}

		/** 
		 * Remove all null references from list of select units
		 * Null referecnes would be from player units that were destroyed after they were selected
		 */
		void UpdateSelection(){
			//Pass a lambda expression to the RemoveAll method
//			print ("Calling from "+transform.name);
			selectedAI.RemoveAll(a => a == null);
		}

		/**
		 * Regular update
		 */ 
		void Update() {
//			print (readyToPatrol);
			//Start drawing selection box on left mouse down
			if(Input.GetMouseButtonDown(0)) {
				start_box = Input.mousePosition;
				drawBox = true;
			}

			//If user presses "s", stop the current movement
			if (Input.GetKey (KeyCode.S)){
				StopAll();
			}

			//If user presses "p", issue patrol command
			if (Input.GetKeyUp (KeyCode.P)){
				readyToPatrol = true;
				readyToAttack = false;
				//print (selectedAI.Count);
			}

			//If user presses "a", issue attack/attackmove command
			if (Input.GetKeyUp (KeyCode.A)){
				readyToAttack = true;
				readyToPatrol = false;
				//print (selectedAI.Count);
			}

			if (Input.GetKey (KeyCode.H)){
				HoldAll();
			}

			//Keep drawing selection box on left mouse down
			if(Input.GetMouseButton(0)){
				end_box = Input.mousePosition;
				makeBox();
				//printy(GUIUtility.ScreenToGUIPoint(Input.mousePosition));
				//print(boundbox);
			}

			//On mouse button up, select all unts within the box
			if(Input.GetMouseButtonUp(0)) {
				//Select all 
				if (readyToPatrol){
					readyToPatrol = false;
					//Issue patrol command
					RaycastHit hit;
					//Cast a ray
					if (Physics.Raycast	(cam.ScreenPointToRay (Input.mousePosition), out hit, Mathf.Infinity, mask) && hit.point != target.position) {
						string hitMask = LayerMask.LayerToName(hit.transform.gameObject.layer);
						print ("Hit "+hit.transform.name+" from layer "+hitMask);
						//If the ray hit the groud, move all selected units
						print ("selectedAI.Count= "+selectedAI.Count);
						if (hitMask == "Ground" && selectedAI.Count != 0){
							target.position = hit.point;
							PatrolAll(target.position);
						}
					}
				}else if(readyToAttack){
					readyToAttack = false;
					//Issue patrol command
					RaycastHit hit;
					//Cast a ray
					if (Physics.Raycast	(cam.ScreenPointToRay (Input.mousePosition), out hit, Mathf.Infinity, mask) && hit.point != target.position) {
						string hitMask = LayerMask.LayerToName(hit.transform.gameObject.layer);
						print ("Hit "+hit.transform.name+" from layer "+hitMask);
						//If the ray hit the groud, move all selected units
						print ("selectedAI.Count= "+selectedAI.Count);
						target.position = hit.point;
						if ( selectedAI.Count != 0){
							if (hitMask == "Unit" ){
								AttackAll(hit.transform);
							}else{
								target.position = hit.point;
								Transform[] targets = CalculateGroupPositions(target.position);
								for (int i = 0; i < selectedAI.Count; i++){
									//selectedAI[i].target = targets[i];
									selectedAI[i].SetTarget(targets[i]);
									//selectedAI[i].target = target;
									//selectedAI[i].FindPath();
									//selectedAI[i].IssueMoveCommand();
								}
								//AttackAll(target);
								AttackAll(targets);
							}


							
						}
					}
				}else{
					//Disable box
					drawBox = false;
					//Get end box corner
					end_box = Input.mousePosition;
					//Create box
					makeBox();

					//Iterate through all selectable objects and check which ones are in the box
					GameObject[] csel = GameObject.FindGameObjectsWithTag("PlayerUnit");
					for (int i = 0; i < csel.Length; i++) {
						//Convert object position to screen coordinated
						Vector3 objectlocation = Camera.main.WorldToScreenPoint(new Vector3(csel[i].transform.position.x,csel[i].transform.position.y,csel[i].transform.position.z));
						
						//If the object falls inside the screen box set its state to selected so we can use it later
						if(boundbox != null && boundbox.Contains(objectlocation)) {
							//csel[i].SendMessage("setisSelected", true);    
							AIWithPathfinding ai = csel[i].GetComponent<AIWithPathfinding>();
							//ai.target = target;
							selectedAI.Add(ai);
						}
						SelectAll(true);
					}
				}
			}
		}

		//Create a selection box
		private void makeBox() {
			//Ensures the bottom left and top right values are correct
			//regardless of how the user boxes units
			float xmin = Mathf.Min(start_box.x, end_box.x);
			float ymin = Mathf.Min(start_box.y, end_box.y);

			float width = Mathf.Max(start_box.x, end_box.x) - xmin;
			float height = Mathf.Max(start_box.y, end_box.y) - ymin;
			boundbox = new Rect(xmin, ymin, width, height);

			//Create box in sreen coordinated
			screenBox = new Rect(start_box.x, Screen.height-start_box.y, end_box.x-start_box.x, start_box.y-end_box.y);
		}

		//Stop all selected units
		private void StopAll() {
			UpdateSelection();
			foreach (AIWithPathfinding ai in selectedAI){
				ai.Stop();
			}
		}

		//Issue Hold() to all selected units
		private void HoldAll() {
			UpdateSelection();
			foreach (AIWithPathfinding ai in selectedAI){
				ai.HoldOn();
			}
		}

		//Issue Patrol() to all selected units
		private void PatrolAll(Vector3 patPoint) {
			UpdateSelection();
			foreach (AIWithPathfinding ai in selectedAI){
				ai.PatrolOn(patPoint);
			}
		}

		//Issue Patrol() to all selected units
		private void AttackAll(Transform target) {
			UpdateSelection();
			foreach (AIWithPathfinding ai in selectedAI){
				ai.AttackOn(target);
			}
		}

		//Issue Patrol() to all selected units
		private void AttackAll(Transform[] targets) {
			UpdateSelection();
			for (int i = 0; i < selectedAI.Count; i++){
				selectedAI[i].AttackOn(targets[i]);
			}
		}

		//Set all units in selectd group as selected
		private void SelectAll(bool status) {
			UpdateSelection();
			foreach (AIWithPathfinding ai in selectedAI){
				ai.highlight.SetActive(status);
				ai.selected = status;
			}
		}
	}
}
					               
