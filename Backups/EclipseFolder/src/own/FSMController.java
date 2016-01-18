package own;

import ch.idsia.agents.Agent;
import ch.idsia.agents.controllers.BasicMarioAIAgent;
import ch.idsia.benchmark.mario.engine.sprites.Mario;
import ch.idsia.benchmark.mario.environments.Environment;

public class FSMController extends BasicMarioAIAgent implements Agent {
	StringBuilder sb = new StringBuilder(); 
	
	public FSMController() {
		super("FSMController");
	}
	
	public void reset()
	{
	    action = new boolean[Environment.numberOfKeys];
	    action[Mario.KEY_RIGHT] = true;
	    action[Mario.KEY_SPEED] = true;
	}
	
	public boolean[] getAction()
	{
	    action[Mario.KEY_RIGHT] = true; 
	    printLevelScene();
	    //blocked();
	    return action;
	}
	
	public boolean blocked(){
		//isMarioOnGround
		for(int i : marioState){
			System.out.println("i: " + i); 
		}
		System.out.println(receptiveFieldHeight); 
		System.out.println(receptiveFieldWidth); 
		return false; 
	}
	
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
