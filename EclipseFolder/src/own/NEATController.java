package own;

import java.awt.event.KeyEvent;
import java.util.ArrayList;
import java.util.List;

import org.jgap.BulkFitnessFunction;
import org.jgap.Configuration;

import ch.idsia.agents.controllers.BasicMarioAIAgent;
import ch.idsia.benchmark.mario.engine.LevelScene;
import ch.idsia.benchmark.mario.engine.sprites.Mario;
import ch.idsia.benchmark.mario.environments.Environment;

public class NEATController extends BasicMarioAIAgent {

// ---- MARIO AI REPRESENTATION ---- // 
private Environment environment; 
private boolean[] actions; 

//http://www.marioai.org/gameplay-track/marioai-benchmark	for zlevels:
int zLevelScene = 1;
int zLevelEnemies = 0;

// ---- NEAT VARIABLES ---- // 
private Configuration configuration; 
private BulkFitnessFunction MarioAIFitnessFunction; 

private LevelScene inputs; 
private int[] outputs; 

// --- DEBUGGING VARIABLES ---- //
StringBuilder sb = new StringBuilder(); 

	public NEATController() {
		super("NEAT Controller");
		this.actions = new boolean[Environment.numberOfKeys]; 
	}
	
	public void reset(){
	    actions[Mario.KEY_LEFT] = false;
	    actions[Mario.KEY_RIGHT] = false;
	    actions[Mario.KEY_DOWN] = false;
	    actions[Mario.KEY_UP] = false;
	    actions[Mario.KEY_JUMP] = false;
	    actions[Mario.KEY_SPEED] = false;
	}
	
	public boolean[] getAction(){
		// Look at SimeMLPAgent to see a simple MLP agent representation
		actions[Mario.KEY_JUMP] = !actions[Mario.KEY_JUMP]; 
		return actions; 
	}
	
	public void integrateObservation(Environment environment){
		// Taken from SimpleMLPAgent.java
	    this.environment = environment;
	    levelScene = environment.getLevelSceneObservationZ(zLevelScene);
	    enemies = environment.getEnemiesObservationZ(zLevelEnemies);
	    mergedObservation = environment.getMergedObservationZZ(1, 0);

	    this.marioFloatPos = environment.getMarioFloatPos();
	    this.enemiesFloatPos = environment.getEnemiesFloatPos();
	    this.marioState = environment.getMarioState();

	    marioStatus = marioState[0];
	    marioMode = marioState[1];
	    isMarioOnGround = marioState[2] == 1;
	    isMarioAbleToJump = marioState[3] == 1;
	    isMarioAbleToShoot = marioState[4] == 1;
	    isMarioCarrying = marioState[5] == 1;
	    getKillsTotal = marioState[6];
	    getKillsByFire = marioState[7];
	    getKillsByStomp = marioState[8];
	    getKillsByShell = marioState[9];
	}
	

/* --- DEBUGGING FUNCTIONS ---- */	
	public void printLevelScene(){
		// Mario is always on square [9][9], therefore numbering it to M
		sb.setLength(0);
		System.out.println("Printing out scene, length: " + levelScene.length + ", " + levelScene[0].length);
		for(int i = 0; i<levelScene.length; i++){
			for(int j = 0; j<levelScene[i].length; j++){
				if(i == 9 && j == 9){
					sb.append("M"); 
				} else {
					sb.append(levelScene[i][j]);
				}
				if(levelScene[i][j] == 0){
					sb.append("  ");
				}
				
			}
			sb.append("\n");
		}
		System.out.println(sb.toString());
	}
}
