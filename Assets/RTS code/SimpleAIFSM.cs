﻿using UnityEngine;
using System.Collections;

/** This class handles state machine and state changes
 *  It calls methods defined in AIWithPathfinding to get the conditions
 *  and execute the actual behaviours.
 */

namespace Lab4 {
	public enum GameState {Idle, Approach, Attack, AttackApproach, Hold, Patrol, UserAttack};

	public class SimpleAIFSM : AIFSM {
		GameObject mouseTarget;
		// Use this for initialization
		void Start () {
			base.Start();
			currentState = GameState.Idle;
			aiController = GetComponent<AIWithPathfinding>();
			mouseTarget = GameObject.Find("Target");
		}
		
		// Update is called once per frame
		void Update () {
			//Get all the boolean variables for decision making.  See FSM diagram for reference.
			bool inAttackRange = aiController.CanAttack();
			bool hasLOS = aiController.HasLOS();
			bool inSightRange = aiController.EnemyClose();
			bool userCmd = aiController.ShouldApproach();
			bool pathComplete = aiController.PathComplete();
			bool stop = aiController.Stopped();

			//Combine the boolean variables to create more humanly readable conditions
			bool canAttack = inAttackRange && hasLOS && !userCmd;
			bool canAttackApproach = inSightRange && !inAttackRange && !userCmd;
			bool shouldIdle = !inSightRange || aiController.target == null;

//			print ("canAttack: "+canAttack+ " inAttackRange: "+ inAttackRange + " hasLOS: "+ hasLOS + " userCmd: "+ userCmd);
//			print ("canAttackApproach: "+canAttackApproach+ " inSightRange: "+ inSightRange + " inAttackRange: "+ inAttackRange + " userCmd: "+ userCmd);
			//print ("inSightRange: "+ inSightRange + " inAttackRange: "+ inAttackRange + " userCmd: "+ userCmd);
			//print ("canAttack: "+canAttack+" canAttackApproach: "+canAttackApproach+" shouldIdle: "+shouldIdle+" userCmd: "+userCmd);
			
			/*
			 * This transition function is only responsible for state changes
			 * If the conditions require a state change, we update currentState
			 * otherwise, if we need to remain in the current state, we do nothing
			 */ 
			switch(currentState)
			{
			case GameState.Idle:
				
				//if user says move
				if (userCmd){
					//print ("Switching to Approach");
					SwitchToApproach();
				//If we can attack
				}else if (canAttack){
					//print ("Switching to Attack");
					//print ("Switching to Attack");
					//print ("canAttack: "+canAttack+ " inAttackRange: "+ inAttackRange + " hasLOS: "+ hasLOS + " userCmd: "+ userCmd);
					aiController.GetClosest();
					currentState = GameState.Attack;
				//If we can approach to attack
				}else if (canAttackApproach){
					//print ("Switching to AttackMove");
					aiController.GetClosest();
					currentState = GameState.AttackApproach;
				}
				break;
			case GameState.Approach:
				//if user says stop
				if (pathComplete || stop){
					//print ("Switching to Idle");
					currentState = GameState.Idle;
				}
				break;
			case GameState.Attack:
				//if user says move
				if (userCmd){
					//print ("Switching to Approach");
					SwitchToApproach();
				//If we have nothing to do
				}else if (shouldIdle) {
					//print ("Switching to Idle");
					//On transition from Attack to Idle: set the mouse target, set target position to be that of the unit
					//aiController.target = mouseTarget.transform;
					//mouseTarget.transform.position = aiController.transform.position;
					currentState = GameState.Idle;
				//If we can approach to attack
				}else if (canAttackApproach){
					//print ("Switching to AttackMove");
					currentState = GameState.AttackApproach;
				}
				break;
			case GameState.AttackApproach:
				//if user says move
				if (userCmd){
					//print ("Switching to Approach");
					SwitchToApproach();
				//If we can attack
				}else if (canAttack) {
					//print ("Switching to Attack");
					currentState = GameState.Attack;
				//If we have nothing to do
				}else if (shouldIdle){
					//print ("Switching to Idle");
					currentState = GameState.Idle;
				}
				break;
			default:
				//If something goes wrong, default to idle
				currentState = GameState.Idle;
				break;
			}
			ExecuteCurrent();
		}

		//This gets called on transition form any state to approach
		public void SwitchToApproach(){
			aiController.target = mouseTarget.transform;
			currentState = GameState.Approach;
		}

		public void ExecuteCurrent()
		{
			//print ("Current state: "+currentState);
			switch(currentState)
			{
			case GameState.Idle:
				aiController.BeIdle();
				break;
			case GameState.Approach:
				aiController.Approach();
				break;
			case GameState.Attack:
				aiController.Attack ();
				break;
			case GameState.AttackApproach:
				aiController.AttackApproach();
				break;
			default:
				//If something goes wrong, default to idle
				aiController.BeIdle();
				break;
			}
		}
	}
}
